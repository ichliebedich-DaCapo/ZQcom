using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Controls;
using System.IO.Ports;
using System.Windows.Media;
using System.Windows;
using ZQcom.Models;
using ZQcom.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Text.RegularExpressions;
using System.Text;
using ZQcom.Events;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using ZQcom.Helpers;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;

/// 编程守则：




namespace ZQcom.ViewModels
{
    public class SerialPortViewModel : ViewModelBase
    {
        // 内部普通变量
        private readonly SerialPortService _serialPortService;      // 串口服务对象
        private SerialPort? _serialPort;                            // 当前打开的串口实例

        // 数据绑定属性
        private string _openCloseButtonText = "打开串口";           // 打开/关闭串口按钮的文本
        private string _sendDataText = "01040000000271CB";          // 发送的数据
        private string _logText = "";                               // 日志文本
        private string _receiveText = "";                           // 接收到的数据文本
        private string _extractedText = "";                         // 提取的数据文本
        private string _convertedText = "";                         // 转换后的数据文本
        private bool _isHexSend;                                    // 是否以十六进制格式发送数据
        private bool _isHexDisplay;                                 // 是否以十六进制格式显示数据
        private bool _addNewline;                                   // 是否在每行数据末尾添加换行符
        private string _selectedSerialPort = string.Empty;          // 选中的串口号
        private int _selectedBaudRate = 115200;                     // 选中的波特率
        private Parity _selectedParity = Parity.None;               // 选中的校验位
        private StopBits _selectedStopBits = StopBits.One;          // 选中的停止位
        private int _selectedDataBits = 8;                          // 选中的数据位
        private bool _isTimedSendEnabled;                           // 是否启用定时发送
        private int _timedSendInterval = 100;                       // 定时发送的时间间隔（毫秒）
        private bool _isExtractedData = false;                      // 是否处理数据
        private bool _isConvertedData = false;                      // 是否转换数据
        private int _startPosition = 7;                             // 数据处理的起始位置
        private int _length = -1;                                    // 数据处理的长度
        private bool _isDisableTimestamp = false;                   // 是否禁用时间戳
        private int _receiveBytes = 0;                              // 接收到的字节数
        private int _sendBytes = 0;                                 // 发送的字节数
        private int _receiveNum = 0;                                // 接收到的数据包数量
        private int _sendNum = 0;                                   // 发送的数据包数量
        private int _pendingQueueSize = 0;                          // 待处理的队列大小
        private bool _isEnableChart = false;                        // 启用图表,默认不可视

        // 定时器相关
        private readonly DispatcherTimer _uiUpdateTimer; // 

        // 线程相关
        private CancellationTokenSource? _cancellationTokenSource;  // 用于取消定时发送任务的CancellationTokenSource

        private readonly ConcurrentQueue<string> _receiveQueue = new();// 【生产者-消费者模式】
        private readonly ConcurrentQueue<string> _logQueue = new();


        // 新
        private CancellationTokenSource? _highFrequencyReceivingCancellationTokenSource;
        private Task? _highFrequencyReceivingTask;


        private readonly BlockingCollection<string> _smallBatchDataQueue = []; // 用于存储小批量数据的队列
        private readonly BlockingCollection<string> _dataToProcessQueue = []; // 用于存储需要处理的数据
        private CancellationTokenSource? _smallBatchCancellationTokenSource; // 用于取消小批量数据处理任务
        private CancellationTokenSource? _dataProcessingCancellationTokenSource; // 用于取消数据处理任务
        private Task? _smallBatchReceivingTask; // 小批量数据处理任务
        private Task? _dataProcessingTask; // 数据处理任务

        // 事件
        public event EventHandler<string>? DataReceived;            // 数据接收事件
        private readonly IEventAggregator _eventAggregator;         // 事件发布者




        // ------------------------初始化------------------------------
        public SerialPortViewModel(IEventAggregator eventAggregator)
        {
            // --------------变量初始化--------------
            _serialPortService = new SerialPortService();
            SerialPortNames = [];
            BaudRateOptions = [9600, 19200, 38400, 57600, 115200];
            ParityOptions = [Parity.None, Parity.Odd, Parity.Even, Parity.Mark, Parity.Space];
            StopBitOptions = [StopBits.None, StopBits.One, StopBits.Two, StopBits.OnePointFive];
            DataBitOptions = [5, 6, 7, 8];


            //--------------前置准备--------------
            // 刷新串口列表
            PopulateSerialPortNames();


            //发布事件
            _eventAggregator = eventAggregator;


            // --------------线程相关--------------


            // --------------定时器相关-------------- 
            _uiUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.05) };
            _uiUpdateTimer.Tick += (sender, args) => UpdateUI();
            _uiUpdateTimer.Start();
        }



        // ---------------------------------私有方法----------------------------------------
        // 开启高频接收。适合大批量数据，高频次，100KHz接收仍不会卡顿，以数据中的分隔符为1次数据接收
        private void StartHighFrequencyReceiving()
        {
            if (_highFrequencyReceivingTask != null && !_highFrequencyReceivingTask.IsCompleted)
            {
                return; // 如果任务已经在运行，则不启动新的任务
            }

            _serialPort.DataReceived += OnDataReceivedHighFrequency;
            _highFrequencyReceivingCancellationTokenSource = new CancellationTokenSource();
            _highFrequencyReceivingTask = ReadTaskHighFrequency(_highFrequencyReceivingCancellationTokenSource.Token);
        }

        public void StopHighFrequencyReceiving()
        {
            _serialPort.DataReceived -= OnDataReceivedHighFrequency;
            if (_highFrequencyReceivingCancellationTokenSource != null)
            {
                _highFrequencyReceivingCancellationTokenSource.Cancel();
                _highFrequencyReceivingCancellationTokenSource.Dispose();
                _highFrequencyReceivingCancellationTokenSource = null;
            }

            if (_highFrequencyReceivingTask != null)
            {
                _highFrequencyReceivingTask.Wait(); // 等待任务完成
                _highFrequencyReceivingTask = null;
            }
        }

        /// <summary>
        /// 小批量接收
        /// </summary>
        // 适合少量数据，低频次，10KHz没有问题。且不需要通过数据中的分隔符来判断是几次接收（自动添加换行），
        // 同时为了性能考虑，日志采用定时刷新，解决了1KHz~10KHz出现时间戳丢失的情况（不过初次接收时，由于StringBuilder还在扩建，可能会导致时间戳丢失）
        // 原本10KHZ以上会出现卡顿漏收问题，经过修改后，10KHz以上不适合使用，会出现大面积的数据丢失
        // 1KHz处理数据时容易出现换行缺失，500Hz很少出现换行缺失，200Hz几乎没有换行缺失
        public void StartSmallBatchReceiving()
        {
            if (_smallBatchReceivingTask != null && !_smallBatchReceivingTask.IsCompleted)
            {
                return; // 如果任务已经在运行，则不启动新的任务
            }

            // 绑定 DataReceived 事件处理程序
            _serialPort.DataReceived += OnDataReceivedSmallBatch;

            // 启动小批量数据处理任务
            _smallBatchCancellationTokenSource = new CancellationTokenSource();
            _smallBatchReceivingTask = Task.Run(() => ProcessSmallBatchData(_smallBatchCancellationTokenSource.Token));

            // 启动数据处理任务
            _dataProcessingCancellationTokenSource = new CancellationTokenSource();
            _dataProcessingTask = Task.Run(() => ProcessData(_dataProcessingCancellationTokenSource.Token));
        }

        public void StopSmallBatchReceiving()
        {
            // 解绑 DataReceived 事件处理程序
            _serialPort.DataReceived -= OnDataReceivedSmallBatch;

            // 取消并等待小批量数据处理任务完成
            if (_smallBatchCancellationTokenSource != null)
            {
                _smallBatchCancellationTokenSource.Cancel();
                _smallBatchCancellationTokenSource.Dispose();
                _smallBatchCancellationTokenSource = null;
            }

            if (_smallBatchReceivingTask != null)
            {
                _smallBatchReceivingTask.Wait(); // 等待任务完成
                _smallBatchReceivingTask = null;
            }

            // 取消数据处理任务
            if (_dataProcessingCancellationTokenSource != null)
            {
                _dataProcessingCancellationTokenSource.Cancel();
                _dataProcessingCancellationTokenSource.Dispose();
                _dataProcessingCancellationTokenSource = null;
            }

            if (_dataProcessingTask != null)
            {
                _dataProcessingTask.Wait(); // 等待任务完成
                _dataProcessingTask = null;
            }
        }

        // ---------------------------------绑定事件----------------------------------------
        public ICommand RefreshSerialPortsCommand => new RelayCommand(PopulateSerialPortNames);
        public ICommand ToggleSerialPortCommand => new RelayCommand(ToggleSerialPort);
        public ICommand SendDataCommand => new RelayCommand(SendData);
        public ICommand ToggleTimedSendCommand => new RelayCommand(ToggleTimedSend);

        public ICommand SaveLogCommand => new RelayCommand(SaveLog);

        public ICommand OpenLogDirectoryCommand => new RelayCommand(OpenLogDirectory);
        public ICommand ClearTextCommand => new RelayCommand(ClearText);


        // 刷新串口列表
        private void PopulateSerialPortNames()
        {
            SerialPortNames.Clear();
            foreach (var port in _serialPortService.GetAvailablePorts())
            {
                SerialPortNames.Add(port);
            }
        }

        // 启用/禁用串口
        // 关闭串口时，也会同步关闭处理数据任务，
        private void ToggleSerialPort()
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                if (string.IsNullOrEmpty(SelectedSerialPort) || !int.TryParse(SelectedBaudRate.ToString(), out int baudRate))
                {
                    MessageBox.Show("请选择串口和波特率。");
                    return;
                }

                try
                {
                    // -------------打开串口逻辑-------------
                    _serialPort = _serialPortService.OpenPort(SelectedSerialPort, baudRate, SelectedParity, SelectedStopBits, SelectedDataBits);
                    OpenCloseButtonText = "关闭串口";

                    // 启用接收
                    StartSmallBatchReceiving();
                    //StartHighFrequencyReceiving();

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开串口失败: {ex.Message}");
                }
            }
            else
            {
                // -------------关闭串口逻辑-------------
                _serialPortService.ClosePort(_serialPort);
                _serialPort = null;
                OpenCloseButtonText = "打开串口";

                // 关闭接收
                StopSmallBatchReceiving();
                //StopHighFrequencyReceiving();

            }
        }



        // 发送数据
        private void SendData()
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                MessageBox.Show("请先打开串口。");
                return;
            }

            // 获取要发送的数据,确保不改变原数据
            var data = SendDataText;
            if (!string.IsNullOrEmpty(data))
            {
                if (IsHexSend)
                {
                    // 检验是否为十六进制字符串
                    if (IsHexString(data))
                    {
                        // 发送转为16进制字节数组的数据
                        byte[] hexData = Convert.FromHexString(data);
                        _serialPortService.SendData(_serialPort, hexData);

                        // 【UI更新】进行字节统计
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SendBytes += hexData.Length;
                        });

                    }
                    else
                    {
                        MessageBox.Show("请不要输入非法字符！");
                        return;
                    }
                    // 格式化数据，确保以16进制发送时不会出错
                    data = FormatHexString(data);
                }
                else
                {
                    // 16进制的换行我还没做，原因很简单，我暂时没有遇到发送16进制还需要加上换行的需求
                    data = data + (AddNewline ? "\r\n" : "");
                    _serialPortService.SendData(_serialPort, data);

                    // 【UI更新】进行字节统计
                    // 将字符串转换为字节数据（假设使用ASCII编码，后续可能会添加多种编码方式）
                    byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
                    //byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SendBytes += buffer.Length;
                    });


                }

                // 【UI更新】进行数量统计
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ++SendNum;
                });


                // 发送到日志框内
                // 不能加入到UI异步更新线程，否则容易卡死
                LogMessage($"<< {data}");
            }
        }



        // ---------接收打印数据--------


        /// -------------高频接收模式--------------
        private SemaphoreSlim _dataAvailableSignal = new SemaphoreSlim(0); // 信号量
        private void OnDataReceivedHighFrequency(object? sender, SerialDataReceivedEventArgs e)
        {
            Interlocked.Increment(ref _backgroundReceiveCount); // 增加后台计数器
            _dataAvailableSignal.Release(); // 通知读取任务开始处理数据
        }
        public async Task ReadTaskHighFrequency(CancellationToken cancellationToken)
        {
            const int BatchSize = 1024; // 批量读取大小

            while (!cancellationToken.IsCancellationRequested)
            {
                // 使用异步等待来避免阻塞
                await _dataAvailableSignal.WaitAsync(cancellationToken); // 等待数据可用信号
                try
                {
                    while (_serialPort.BytesToRead > 0)
                    {
                        var buffer = new byte[Math.Min(_serialPort.BytesToRead, BatchSize)];
                        int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);

                        if (bytesRead > 0)
                        {
                            // 统计字节数
                            Interlocked.Add(ref _backgroundReceiveBytes, bytesRead);

                            // 实时更新UI
                            string data = _serialPort.Encoding.GetString(buffer, 0, bytesRead);
                            await LogMessageHighFrequencyAsync(data, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        }

                        // 防止CPU占用过高，使用异步延迟
                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 任务被取消，正常退出
                    break;
                }
                catch
                {
                    // 处理异常
                    continue;
                }
            }
        }

        private async Task LogMessageHighFrequencyAsync(string inputData, string timestamp)
        {
            var sb = new StringBuilder();
            var value = inputData.Replace("\0", "\\0");

            // 使用正则表达式替换换行符，并添加时间戳
            var logEntry = Regex.Replace(value, @"(\r\n|\r|\n)", $"\r\n[{timestamp}]>> ");
            sb.Append(logEntry);

            // 异步更新UI
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TextEditor?.AppendText(sb.ToString());
            });
        }


        /// ---------------小批量数据接收-------------
       

        private void OnDataReceivedSmallBatch(object? sender, SerialDataReceivedEventArgs e)
        {
            Interlocked.Add(ref _backgroundReceiveBytes, _serialPort.BytesToRead);
            Interlocked.Increment(ref _backgroundReceiveCount); // 增加后台计数器

            // 直接读取所有可用数据并添加到队列中

                _smallBatchDataQueue.Add(_serialPort.ReadExisting());

        }

        // 用于累积日志数据
        private readonly StringBuilder _logBuffer = new(); 
        private void LogMessageSmallBatch(ref string inputData)
        {
            // 缓存当前时间的格式化字符串
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            // 使用 AppendFormat 减少方法调用次数
            lock (_logBuffer)
            {
                _logBuffer.AppendFormat("[{0}]>> {1}{2}", now, inputData, Environment.NewLine);
            }
        }

        private void ProcessSmallBatchData(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Take 将阻塞当前线程，直到队列中有数据可用或取消标记被触发
                    string data = _smallBatchDataQueue.Take(cancellationToken);

                    // 打印数据
                    LogMessageSmallBatch(ref data);

                    // 将数据添加到处理队列中
                    if (IsExtractedData || IsConvertedData)
                    {
                        _dataToProcessQueue.Add(data, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 如果取消请求被触发，则退出循环
                Console.WriteLine("处理小批量数据的任务已被取消。");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("collection was completed"))
            {
                // 如果集合被标记为完成且队列为空，则正常退出
                Console.WriteLine("处理小批量数据的任务已完成。");
            }
            catch (Exception ex)
            {
                // 处理其他异常
                Console.WriteLine($"处理小批量数据时发生错误: {ex.Message}");
            }
            finally
            {
                // 完成后确保清理资源
                _smallBatchDataQueue.CompleteAdding();
            }
        }


        /// <summary>
        ///  处理数据
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// 目前只适用小批量数据接收模式
        private readonly StringBuilder _extractedDataBuffer = new(); // 用于累积截取的数据
        private readonly StringBuilder _convertedDataBuffer = new(); // 用于累积转换后的数据
        private void ProcessData(CancellationToken cancellationToken)
        {
            const int batchSize = 10; // 每批处理的数据数量
            var batch = new List<string>(batchSize);

            try
            {
                while (!cancellationToken.IsCancellationRequested || !_dataToProcessQueue.IsCompleted)
                {
                    string data;
                    try
                    {
                        // Take 将阻塞当前线程，直到队列中有数据可用或取消标记被触发
                        data = _dataToProcessQueue.Take(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // 如果取消请求被触发，则退出循环
                        break;
                    }

                    // 使用手动遍历来移除所有空白字符（包括空格、换行符等）
                    string cleanedData = new string(data.Where(c => !char.IsWhiteSpace(c)).ToArray());

                    string extractedData = cleanedData;

                    // 截取数据
                    if (IsExtractedData)
                    {
                        if (Length > 0)
                        {
                            if (StartPosition - 1 + Length > cleanedData.Length)
                            {
                                lock (_convertedDataBuffer) // 错误信息也累积到 _convertedDataBuffer
                                {
                                    _convertedDataBuffer.AppendLine("长度不足");
                                }
                                continue;
                            }

                            // 截取数据
                            extractedData = cleanedData.Substring(StartPosition - 1, Length);
                        }

                        // 显示截取的数据
                        ExtractedDataMessage(extractedData);
                    }

                    // -----转换数据-----
                    if (IsConvertedData)
                    {
                        try
                        {
                            // 转换为浮点数
                            if (float.TryParse(extractedData, out float result))
                            {
                                lock (_convertedDataBuffer)
                                {
                                    _convertedDataBuffer.AppendLine(result.ToString());
                                }
                                batch.Add(result.ToString()); // 添加到批次列表

                                // 发布事件
                                if (IsEnableChart)
                                    _eventAggregator.GetEvent<DataReceivedEvent>().Publish(result);
                            }
                            else
                            {
                                lock (_convertedDataBuffer) // 错误信息也累积到 _convertedDataBuffer
                                {
                                    _convertedDataBuffer.AppendLine("数据转换失败: 无法解析为浮点数");
                                }

                                // 发布事件
                                if (IsEnableChart)
                                    _eventAggregator.GetEvent<DataReceivedEvent>().Publish(0);
                            }
                        }
                        catch (Exception ex)
                        {
                            // 记录异常，不阻塞主线程
                            Console.WriteLine($"数据转换失败：{ex.Message}");

                            // 发布事件
                            if (IsEnableChart)
                                _eventAggregator.GetEvent<DataReceivedEvent>().Publish(0);
                        }

                        // 如果批次达到阈值，则累积到缓冲区
                        if (batch.Count >= batchSize)
                        {
                            AppendBatchToBuffer(batch);
                            batch.Clear();
                        }
                    }

                    // 更新UI上的处理结果（每处理一批数据后更新一次）
                    if (_convertedDataBuffer.Length > 0 && batch.Count == 0)
                    {
                        // 不再直接更新UI，而是累积到缓冲区
                    }
                }
            }
            finally
            {
                // 最终更新UI上的处理结果（如果有剩余未处理的数据）
                if (batch.Count > 0)
                {
                    AppendBatchToBuffer(batch);
                }

                // 确保最后的数据累积到缓冲区
                if (_convertedDataBuffer.Length > 0)
                {
                    // 这里不做任何事情，因为最终刷新由定时器完成
                }
            }
        }

        // 辅助方法：将批次添加到缓冲区
        private void AppendBatchToBuffer(List<string> batch)
        {
            lock (_convertedDataBuffer)
            {
                foreach (var item in batch)
                {
                    _convertedDataBuffer.AppendLine(item);
                }
            }
        }



        // -------刷新UI-------
        private int _backgroundReceiveCount; // 后台计数器
        private int _backgroundReceiveBytes; // 后台计数器
        private void UpdateUI()
        {
            int count = Interlocked.Exchange(ref _backgroundReceiveCount, 0); // 获取并重置后台计数器
            int bytes = Interlocked.Exchange(ref _backgroundReceiveBytes, 0);

            // 更新UI上的ReceiveNum
            ReceiveNum += count; // 假设ReceiveNum是您的数据绑定属性
            ReceiveBytes += bytes;

            // 更新日志
            if(_logBuffer.Length>0)
            {
                lock (_logBuffer)
                {
                    TextEditor?.AppendText(_logBuffer.ToString());
                    _logBuffer.Clear();
                }
            }

            // 更新截取的数据
            if (_extractedDataBuffer.Length > 0)
            {
                lock (_extractedDataBuffer)
                {
                    ExtractedText += _extractedDataBuffer.ToString();
                    _extractedDataBuffer.Clear();
                }
            }

            // 更新转换后的数据
            if (_convertedDataBuffer.Length > 0)
            {
                lock (_convertedDataBuffer)
                {
                    ConvertedText += _convertedDataBuffer.ToString();
                    _convertedDataBuffer.Clear();
                }
            }
        }

        // 发送截取数据
        private void ExtractedDataMessage(string data)
        {
            lock (_extractedDataBuffer)
            {
                _extractedDataBuffer.AppendLine(data);
            }
        }

        // 发送处理过的数据
        private void ConvertedDataMessage(string data)
        {
            lock (_convertedDataBuffer)
            {
                _convertedDataBuffer.AppendLine(data);
            }
        }




        /// <summary>
        /// 启用/禁用定时发送
        /// </summary>
        private async void ToggleTimedSend()
        {
            if (_isTimedSendEnabled)
            {
                _cancellationTokenSource?.Cancel();
                _isTimedSendEnabled = false;
                RaisePropertyChanged(nameof(IsTimedSendEnabled));
            }
            else
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _isTimedSendEnabled = true;
                RaisePropertyChanged(nameof(IsTimedSendEnabled));

                try
                {
                    while (true)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        await Task.Delay(TimedSendInterval, _cancellationTokenSource.Token);
                        SendData();
                    }
                }
                catch (OperationCanceledException)
                {
                    // 当任务被取消时抛出的异常
                    // 注释掉，防止频繁弹出消息框
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    MessageBox.Show("定时发送已取消", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                    //});
                }
                catch (Exception ex)
                {
                    // 其他异常
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                finally
                {
                    _isTimedSendEnabled = false;
                    RaisePropertyChanged(nameof(IsTimedSendEnabled));
                }
            }
        }



        // 判断是否为有效的十六进制字符串
        public bool IsHexString(string input)
        {
            // 去除字符串中的所有空格
            input = input.Replace(" ", "");

            // 正则表达式匹配16进制字符
            return Regex.IsMatch(input, "^[0-9A-Fa-f]+$");
        }
        // 格式化数据，当为16进制显示时，将数据转为16进制字符串，否则原样返回
        private string FormatData(string data)
        {
            if (IsHexDisplay)
            {
                return BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(data)).Replace("-", " ");
            }
            return data;
        }
        // 格式化16进制字符串
        public string FormatHexString(string hexString)
        {
            // 去除所有空格，以16进制显示是不会有换行的
            hexString = hexString.Replace(" ", ""); ;

            // 将字符串按每两个字符分割，并用空格连接
            return string.Join(" ", Enumerable.Range(0, hexString.Length / 2).Select(i => hexString.Substring(i * 2, 2)));
        }



        // 发送日志消息
        private void LogMessage(string message)
        {
            // 不知道为什么无法加入异步UI线程，加入后会很卡，可能与异步线程"数据处理任务”的调用有关
            if (IsDisableTimestamp)
            {
                LogText += $" {message}{Environment.NewLine}";
            }
            else
            {
                LogText += $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
            }
        }


        // 发送到处理数据框
        // 只有当IsProcessData 为 true 时才进行处理
        // 开启16进制显示时会以16进制数据处理，不开启则以普通字符串处理
        private void ProcessData(string data)
        {
            try
            {
                // 获取数据起始位置和长度
                int startIndex = StartPosition - 1; // 起始位置从1开始
                int length = Length;

                // 检查起始位置
                if (StartPosition <= 0 && length != -1)
                {
                    MessageBox.Show("起始位置不能小于等于0，请重新输入！");
                    IsExtractedData = false; // 关闭处理数据
                    return;
                }

                // ----根据是否发送16进制数据进行不同处理----
                string processedData;
                // 移除空格是因为当开启16进制显示时，字符串中会包含空格、换行
                string hexDataWithoutSpaces = data.Replace(" ", "").Replace("\n", "").Replace("\r", "");
                // 最终转换的浮点数据,默认为0
                float floatValue = 0.0f;


                // 检查数据长度
                // 增加了判断条件，当长度为-1时，表示从起始位置到末尾
                if (length == -1)
                {
                    processedData = hexDataWithoutSpaces;
                }
                else
                {
                    if (startIndex + length <= hexDataWithoutSpaces.Length)
                    {
                        ConvertedDataMessage("长度不足");
                    }

                    // 截取数据,并发送
                    processedData = hexDataWithoutSpaces.Substring(startIndex, length);
                }
                ExtractedDataMessage(processedData);



                // 由于前面已经经过是否显示16进制处理过了，所以此时一定是16进制字符串       
                if (IsHexDisplay)
                {
                    // 将16进制字符串转换为字节数组
                    byte[] bytes = Convert.FromHexString(processedData);

                    // 检查字节数组长度是否为4
                    if (bytes.Length != 4)
                    {
                        // 错误情况下的处理

                        ConvertedDataMessage("长度不足");

                    }
                    else
                    {


                        // 非常重要，因为小端模式下，数组中的数据需要反转才能正确转换
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(bytes);
                        }

                        // 将字节数组转换为32位浮点数
                        floatValue = BitConverter.ToSingle(bytes, 0);
                        ConvertedDataMessage(floatValue.ToString());
                    }

                }
                else
                {
                    // 尝试直接将字符串转换为32位浮点数
                    //【隐患】没有进行异常处理，前面字符串确实会黏连，这就非常奇怪。想要解决也很简单，来个图表直连
                    if (float.TryParse(processedData, out float result))
                    {
                        floatValue = result;
                        ConvertedDataMessage(result.ToString());
                    }
                    else
                    {
                        ConvertedDataMessage("无法转换");
                    }
                }

                // 发布事件

                if (IsEnableChart)
                    _eventAggregator.GetEvent<DataReceivedEvent>().Publish(floatValue);
            }
            catch (FormatException ex)
            {
                // 其他异常
                MessageBox.Show($"处理数据发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                IsExtractedData = false;// 关闭处理数据
            }
        }



        // 生成日志文件名
        private string GenerateLogFileName()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"Log/log_{timestamp}.txt";
        }
        // 确保目录存在
        private void EnsureDirectoryExists(string? directoryPath)
        {
            if (directoryPath != null)
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
        }
        // 保存日志
        public void SaveLog()
        {
            try
            {
                string logFilePath = GenerateLogFileName();
                EnsureDirectoryExists(Path.GetDirectoryName(logFilePath));
                // 保存日志框

                if (TextEditor?.Text != "")
                {
                    File.WriteAllText(logFilePath, TextEditor?.Text);
                    MessageBox.Show($"日志已成功保存到: {logFilePath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                // 保存处理数据框
                if (IsExtractedData)
                {
                    if (ExtractedText != "")
                        File.WriteAllText(logFilePath.Replace(".txt", "_extracted.txt"), ExtractedText);
                    if (ConvertedText != "")
                        File.WriteAllText(logFilePath.Replace(".txt", "_converted.txt"), ConvertedText);
                }

            }
            catch (Exception ex)
            {
                // 处理异常，例如显示错误消息
                MessageBox.Show($"保存日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 打开日志文件夹
        public void OpenLogDirectory()
        {
            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Log");
            EnsureDirectoryExists(logDirectory); // 直接传递 logDirectory
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", logDirectory));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开日志文件夹时发生错误: {ex.Message}");
            }
        }


        public void ClearText()
        {
            // 弹出确认对话框
            var result = MessageBox.Show("确定要清屏吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // 清除文本框内容
                LogText = string.Empty;
                ExtractedText = string.Empty;
                ConvertedText = string.Empty;
                _receiveQueue.Clear();//清空接收队列
                _logQueue.Clear();

                // 清除日志框
                TextEditor?.Clear();

                // 清除接收发送数据统计
                ReceiveBytes = 0;
                SendBytes = 0;
                ReceiveNum = 0;
                SendNum = 0;
                //MessageBox.Show("已成功清屏", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }



        // ------------------------组件映射------------------------------
        public TextEditor? TextEditor { get; set; }




        // ------------------------数据绑定------------------------------
        // 可用的串口列表
        public ObservableCollection<string>? AvailablePorts { get; set; }
        public ObservableCollection<string> SerialPortNames { get; set; }
        public string SelectedSerialPort
        {
            get => _selectedSerialPort;
            set
            {
                _selectedSerialPort = value;
                RaisePropertyChanged(nameof(SelectedSerialPort));
            }
        }


        // 设置波特率
        public ObservableCollection<int> BaudRateOptions { get; set; }
        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set
            {
                _selectedBaudRate = value;
                RaisePropertyChanged(nameof(SelectedBaudRate));
            }
        }

        // 奇偶校验
        public ObservableCollection<Parity> ParityOptions { get; set; }
        public Parity SelectedParity
        {
            get => _selectedParity;
            set
            {
                _selectedParity = value;
                RaisePropertyChanged(nameof(SelectedParity));
            }
        }

        // 停止位
        public ObservableCollection<StopBits> StopBitOptions { get; set; }
        public StopBits SelectedStopBits
        {
            get => _selectedStopBits;
            set
            {
                _selectedStopBits = value;
                RaisePropertyChanged(nameof(SelectedStopBits));
            }
        }

        // 数据位
        public ObservableCollection<int> DataBitOptions { get; set; }
        public int SelectedDataBits
        {
            get => _selectedDataBits;
            set
            {
                _selectedDataBits = value;
                RaisePropertyChanged(nameof(SelectedDataBits));
            }
        }

        // 启闭串口按钮_文本
        public string OpenCloseButtonText
        {
            get => _openCloseButtonText;
            set
            {
                _openCloseButtonText = value;
                RaisePropertyChanged(nameof(OpenCloseButtonText));
            }
        }

        // 发送数据
        public string SendDataText
        {
            get => _sendDataText;
            set
            {
                _sendDataText = value;
                RaisePropertyChanged(nameof(SendDataText));
            }
        }

        // 日志框
        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                RaisePropertyChanged(nameof(LogText));
            }
        }


        // 接收数据框
        public string ReceiveText
        {
            get => _receiveText;
            set
            {
                _receiveText = value;
                RaisePropertyChanged(nameof(ReceiveText));
            }
        }

        // 截取数据框
        public string ExtractedText
        {
            get => _extractedText;
            set
            {
                _extractedText = value;
                RaisePropertyChanged(nameof(ExtractedText));
            }
        }


        // 转换数据框
        public string ConvertedText
        {
            get => _convertedText;
            set
            {
                _convertedText = value;
                RaisePropertyChanged(nameof(ConvertedText));
            }
        }

        // 十六进制
        public bool IsHexSend
        {
            get => _isHexSend;
            set
            {
                _isHexSend = value;
                RaisePropertyChanged(nameof(IsHexSend));
            }
        }

        // 十六进制显示
        public bool IsHexDisplay
        {
            get => _isHexDisplay;
            set
            {
                _isHexDisplay = value;
                RaisePropertyChanged(nameof(IsHexDisplay));
            }
        }

        // 添加回车
        public bool AddNewline
        {
            get => _addNewline;
            set
            {
                _addNewline = value;
                RaisePropertyChanged(nameof(AddNewline));
            }
        }


        // 定时发送
        public bool IsTimedSendEnabled
        {
            get => _isTimedSendEnabled;
            set
            {
                _isTimedSendEnabled = value;
                RaisePropertyChanged(nameof(IsTimedSendEnabled));
            }
        }

        // 定时发送间隔
        public int TimedSendInterval
        {
            get => _timedSendInterval;
            set
            {
                _timedSendInterval = value;
                RaisePropertyChanged(nameof(TimedSendInterval));
            }
        }


        // 是否处理数据
        public bool IsExtractedData
        {
            get => _isExtractedData;
            set
            {
                _isExtractedData = value;
                RaisePropertyChanged(nameof(IsExtractedData));
            }
        }

        // 是否转换数据
        public bool IsConvertedData
        {
            get => _isConvertedData;
            set
            {
                _isConvertedData = value;
                RaisePropertyChanged(nameof(IsConvertedData));
            }
        }


        // 截取数据的起始位置
        public int StartPosition
        {
            get => _startPosition;
            set
            {
                // 检查起始位置
                if (Length != -1 && value < 1)
                {
                    MessageBox.Show("起始位置不能小于等于0，请重新输入！");
                    IsExtractedData = false; // 关闭截取数据
                    _startPosition = 1;
                    return;
                }
                _startPosition = value;
                RaisePropertyChanged(nameof(StartPosition));
            }
        }

        // 截取数据的长度
        public int Length
        {
            get => _length;
            set
            {
                if (value < -1)
                {
                    MessageBox.Show("-1即表示全长。长度不能小于-1，请重新输入！");
                    IsExtractedData = false;
                    _length = -1;
                    return;
                }

                _length = value;
                RaisePropertyChanged(nameof(Length));
            }
        }


        // 是否禁用时间戳
        public bool IsDisableTimestamp
        {
            get => _isDisableTimestamp;
            set
            {
                _isDisableTimestamp = value;
                RaisePropertyChanged(nameof(IsDisableTimestamp));
            }
        }


        // 接收的字节数、数量，发送的字节数、数量
        public int ReceiveBytes
        {
            get => _receiveBytes;
            set
            {
                _receiveBytes = value;
                RaisePropertyChanged(nameof(ReceiveBytes));
            }
        }
        public int SendBytes
        {
            get => _sendBytes;
            set
            {
                _sendBytes = value;
                RaisePropertyChanged(nameof(SendBytes));
            }
        }
        public int ReceiveNum
        {
            get => _receiveNum;
            set
            {
                _receiveNum = value;
                RaisePropertyChanged(nameof(ReceiveNum));
            }
        }
        public int SendNum
        {
            get => _sendNum;
            set
            {
                _sendNum = value;
                RaisePropertyChanged(nameof(SendNum));
            }
        }

        // 待处理数量
        public int PendingNum
        {
            get => _pendingQueueSize;
            set
            {
                _pendingQueueSize = value;
                RaisePropertyChanged(nameof(PendingNum));
            }
        }

        // 图表可视属性
        public System.Windows.Visibility ChartVisibility
        {
            get
            {
                if (IsEnableChart)
                {
                    return System.Windows.Visibility.Visible;
                }
                else
                {
                    return System.Windows.Visibility.Hidden;
                }
            }
        }


        // 启用图表
        public bool IsEnableChart
        {
            get => _isEnableChart;
            set
            {
                _isEnableChart = value;
                RaisePropertyChanged(nameof(IsEnableChart));
                RaisePropertyChanged(nameof(ChartVisibility));
            }
        }





        //// 用于测试图表性能
        //public ICommand DebugCommand => new RelayCommand(Debug);

        //public async void Debug()
        //{
        //        try
        //        {
        //        float floatValue = 0;
        //        Random random = new Random();
        //        _cancellationTokenSource = new CancellationTokenSource();
        //        while (true)
        //            {
        //                if (_cancellationTokenSource.Token.IsCancellationRequested)
        //                {
        //                    break;
        //                }
        //                // 每10ms向图表传递一个数据
        //                await Task.Delay(10, _cancellationTokenSource.Token);
        //            _eventAggregator.GetEvent<DataReceivedEvent>().Publish(random.Next(100));
        //        }
        //        }
        //        catch (OperationCanceledException)
        //        {
        //        }
        //        catch (Exception ex)
        //        {
        //            // 其他异常
        //            Application.Current.Dispatcher.Invoke(() =>
        //            {
        //                MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        //            });
        //        }
        //        finally
        //        {
        //        }
        //}


        // ---------------------------------------------------------------------
        // ---------------------------------------------------------------------
        // 忘记这是干啥的了，先注释保留
        //private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        //{
        //    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        //    {
        //        var child = VisualTreeHelper.GetChild(parent, i);
        //        if (child is T t)
        //        {
        //            return t;
        //        }

        //        var childOfChild = FindVisualChild<T>(child);
        //        if (childOfChild != null)
        //        {
        //            return childOfChild;
        //        }
        //    }

        //    return null;
        //}

        //private void OnDataReceived(string data)
        //{
        //    DataReceived?.Invoke(this, data);
        //}
    }
}