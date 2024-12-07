using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO.Ports;
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
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
/// 编程守则：




namespace ZQcom.ViewModels
{
    public partial class SerialPortViewModel : ViewModelBase
    {
        // 内部普通变量
        private readonly SerialPortService _serialPortService;      // 串口服务对象
        private SerialPort? _serialPort;                            // 当前打开的串口实例
        private readonly StringBuilder _signBuffer = new();         // 存储标记数据

        // 数据绑定属性
        private string _openCloseButtonText = "打开串口";           // 打开/关闭串口按钮的文本
        private string ?_sendDataText;                              // 发送的数据
        private bool _isHexSend;                                    // 是否以十六进制格式发送数据
        private bool _isHexDisplay;                                 // 是否以十六进制格式显示数据
        private bool _addNewline;                                   // 是否在每行数据末尾添加换行符
        private string _selectedSerialPort = string.Empty;          // 选中的串口号
        private int _selectedBaudRate;                              // 选中的波特率
        private Parity _selectedParity;                             // 选中的校验位
        private StopBits _selectedStopBits;                         // 选中的停止位
        private int _selectedDataBits;                              // 选中的数据位
        private bool _isTimedSendEnabled;                           // 是否启用定时发送
        private int _timedSendInterval;                             // 定时发送的时间间隔（毫秒）
        private bool _isExtractedData;                              // 是否处理数据
        private bool _isConvertedData;                              // 是否转换数据
        private int _startPosition;                                 // 数据处理的起始位置
        private int _length ;                                       // 数据处理的长度
        private bool _isHighFrequencyReceiving;                     // 是否高频接收
        private bool _oldIsHighFrequencyReceiving;                  // 上一次是否高频接收
        private bool _isDisableTimestamp;                           // 是否禁用时间戳
        private int _receiveBytes = 0;                              // 接收到的字节数
        private int _sendBytes = 0;                                 // 发送的字节数
        private int _receiveNum = 0;                                // 接收到的数据包数量
        private int _sendNum = 0;                                   // 发送的数据包数量
        private int _pendingQueueSize = 0;                          // 待处理的队列大小
        private bool _isEnableChart;                                // 启用图表,默认不可视

        // 定时器相关
        private readonly DispatcherTimer _uiUpdateTimer;            // UI更新定时器
        private Timer? _timedSendTimer;                             // 定时发送定时器
        private readonly object _timedSendTimerLock = new();

        // 线程相关
        private readonly ConcurrentQueue<string> _receiveQueue = new();// 【生产者-消费者模式】
        private readonly ConcurrentQueue<string> _logQueue = new();


        // 线程相关
        private CancellationTokenSource? _highFrequencyReceivingCancellationTokenSource;
        private Task? _highFrequencyReceivingTask;


        private readonly BlockingCollection<string> _smallBatchDataQueue = []; // 用于存储小批量数据的队列
        private readonly BlockingCollection<string> _dataToProcessQueue = []; // 用于存储需要处理的数据
        private CancellationTokenSource? _smallBatchCancellationTokenSource; // 用于取消小批量数据处理任务
        private CancellationTokenSource? _dataProcessingCancellationTokenSource; // 用于取消数据处理任务
        private Task? _smallBatchReceivingTask; // 小批量数据处理任务
        private Task? _dataProcessingTask; // 数据处理任务

        // 事件
        private readonly IEventAggregator _eventAggregator;         // 事件发布者




        // ------------------------初始化------------------------------
        public SerialPortViewModel(IEventAggregator eventAggregator,SerialPortSettings serialPortSettings)
        {
            // --------------变量初始化--------------
            _serialPortService = new SerialPortService();
            SerialPortNames = [];
            BaudRateOptions = [9600, 19200, 38400, 57600, 115200];
            ParityOptions = [Parity.None, Parity.Odd, Parity.Even, Parity.Mark, Parity.Space];
            StopBitOptions = [StopBits.None, StopBits.One, StopBits.Two, StopBits.OnePointFive];
            DataBitOptions = [5, 6, 7, 8];

            // --------------加载配置属性--------------
            SetSettings(serialPortSettings);

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

            _serialPortService.Start(_serialPort, OnDataReceivedHighFrequency);

            _highFrequencyReceivingCancellationTokenSource = new CancellationTokenSource();
            _highFrequencyReceivingTask = Task.Run(() => ReadTaskHighFrequency(_highFrequencyReceivingCancellationTokenSource.Token));
        }

        public void StopHighFrequencyReceiving()
        {

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

            // 绑定 DataReceived 事件处理程序并启动
            _serialPortService.Start(_serialPort, OnDataReceivedSmallBatch);

            // 启动小批量数据处理任务
            _smallBatchCancellationTokenSource = new CancellationTokenSource();
            _smallBatchReceivingTask = Task.Run(() => ProcessSmallBatchData(_smallBatchCancellationTokenSource.Token));

            // 启动数据处理任务
            _dataProcessingCancellationTokenSource = new CancellationTokenSource();
            _dataProcessingTask = Task.Run(() => ProcessData(_dataProcessingCancellationTokenSource.Token));
        }

        public void StopSmallBatchReceiving()
        {
            // 取消小批量数据处理任务
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

        // 获取配置
        public SerialPortSettings GetSettings()
        {
            return new SerialPortSettings
            {
                SelectedSerialPort = SelectedSerialPort,
                SelectedBaudRate = SelectedBaudRate,
                SelectedParity = SelectedParity,
                SelectedStopBits = SelectedStopBits,
                SelectedDataBits = SelectedDataBits,
                SendDataText = SendDataText,
                IsHexSend = IsHexSend,
                IsHexDisplay = IsHexDisplay,
                AddNewline = AddNewline,
                TimedSendInterval = TimedSendInterval,
                IsExtractedData = IsExtractedData,
                IsConvertedData = IsConvertedData,
                StartPosition = StartPosition,
                Length = Length,
                IsDisableTimestamp = IsDisableTimestamp,
                IsEnableChart = IsEnableChart,
                IsHighFrequencyReceiving = IsHighFrequencyReceiving
            };
        }
        // 设置串口参数
        public void SetSettings(SerialPortSettings settings)
        {
            SelectedSerialPort = settings.SelectedSerialPort;
            SelectedBaudRate = settings.SelectedBaudRate;
            SelectedParity = settings.SelectedParity;
            SelectedStopBits = settings.SelectedStopBits;
            SelectedDataBits = settings.SelectedDataBits;
            SendDataText = settings.SendDataText;
            IsHexSend = settings.IsHexSend;
            IsHexDisplay = settings.IsHexDisplay;
            AddNewline = settings.AddNewline;
            TimedSendInterval = settings.TimedSendInterval;
            IsExtractedData = settings.IsExtractedData;
            IsConvertedData = settings.IsConvertedData;
            StartPosition = settings.StartPosition;
            Length = settings.Length;
            IsDisableTimestamp = settings.IsDisableTimestamp;
            IsEnableChart = settings.IsEnableChart;
            IsHighFrequencyReceiving = settings.IsHighFrequencyReceiving;
        }

        // ---------------------------------绑定事件----------------------------------------
        public ICommand RefreshSerialPortsCommand => new RelayCommand(PopulateSerialPortNames);
        public ICommand ToggleSerialPortCommand => new RelayCommand(ToggleSerialPort);
        public ICommand SendDataCommand => new RelayCommand(SendData);
        public ICommand ToggleTimedSendCommand => new RelayCommand(ToggleTimedSend);

        public ICommand SaveLogCommand => new RelayCommand(SaveLog);

        public ICommand OpenLogDirectoryCommand => new RelayCommand(OpenLogDirectory);
        public ICommand ClearTextCommand => new RelayCommand(ClearText);
        public ICommand SignCommand => new RelayCommand(SignIndex);

        // 刷新串口列表
        private void PopulateSerialPortNames()
        {
            SerialPortNames.Clear();
            foreach (var port in SerialPortService.GetAvailablePorts())
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
                    _oldIsHighFrequencyReceiving = IsHighFrequencyReceiving;
                    if (IsHighFrequencyReceiving)
                    {
                        StartHighFrequencyReceiving();
                    }
                    else
                    {
                        StartSmallBatchReceiving();
                    }
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
                OpenCloseButtonText = "打开串口";

                // 关闭接收
                if (_oldIsHighFrequencyReceiving)
                    StopHighFrequencyReceiving();
                else
                    StopSmallBatchReceiving();

                // ---执行清理工作---
                _serialPort.Dispose();
                _serialPort = null;

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

            // 发送框中的数据
            SendDataBase(SendDataText);
        }

        private void SendDataBase(string data)
        {
            // 获取要发送的数据,确保不改变原数据
            if (IsHexSend)
            {
                // 检验是否为十六进制字符串
                if (IsHexString(data))
                {
                    // 发送转为16进制字节数组的数据
                    byte[] hexData = Convert.FromHexString(data);
                    _serialPortService.SendData(_serialPort, hexData);

                    // 【UI更新】进行字节统计
                    Interlocked.Add(ref _backgroundReceiveBytes, hexData.Length);

                }
                else
                {
                    MessageBox.Show("请不要输入非法字符！");

                    // 停止定时发送
                    if (IsTimedSendEnabled)
                    {
                        // 停止并释放定时器资源
                        _timedSendTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                        _timedSendTimer?.Dispose();
                        _timedSendTimer = null;
                        IsTimedSendEnabled = false;
                    }
                    return;
                }

                // 格式化数据，用于显示
                data = FormatHexString(data);
            }
            else
            {
                // 16进制的换行我还没做，因为暂时没有遇到发送16进制还需要加上换行的需求
                data = data + (AddNewline ? "\r\n" : "");
                _serialPortService.SendData(_serialPort, data);

                // 【UI更新】进行字节统计
                // 将字符串转换为字节数据（假设使用ASCII编码，后续可能会添加多种编码方式）
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
                // byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
                Interlocked.Add(ref _backgroundSendBytes, buffer.Length);
            }

            // 【UI更新】进行数量统计
            Interlocked.Increment(ref _backgroundSendCount);



            // 发送到日志框内
            // 不能加入到UI异步更新线程，否则容易卡死
            SendLogMessage(data);
        }

        /// <summary>
        /// 启用/禁用定时发送
        /// </summary>
        private void ToggleTimedSend()
        {
            lock (_timedSendTimerLock)
            {
                IsTimedSendEnabled = !IsTimedSendEnabled;

                if (IsTimedSendEnabled)
                {
                    // 如果已经存在一个计时器，先停止它
                    _timedSendTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                    // 创建或重新配置计时器
                    _timedSendTimer = new Timer(TimedSendCallback, null, _timedSendInterval, _timedSendInterval);
                }
                else
                {
                    // 停止并释放定时器资源
                    _timedSendTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    _timedSendTimer?.Dispose();
                    _timedSendTimer = null;
                }
            }
        }

        private void TimedSendCallback(object state)
        {
            try
            {
                // 发送数据
                SendDataBase(SendDataText);
            }
            catch (Exception ex)
            {
                // 处理异常
                MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                // 如果发生异常，可能需要禁用定时发送以防止无限次尝试导致更多错误
                lock (_timedSendTimerLock)
                {
                    IsTimedSendEnabled = false;
                    _timedSendTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    _timedSendTimer?.Dispose();
                    _timedSendTimer = null;
                }
            }
        }


        // ---------接收打印数据--------
        /// -------------高频接收模式--------------
        /// 高频接收模式下不提供处理数据、发送数据等操作
        /// 
        private readonly SemaphoreSlim _dataAvailableSignal = new(0); // 信号量
        private void OnDataReceivedHighFrequency(object? sender, SerialDataReceivedEventArgs e)
        {
            Interlocked.Increment(ref _backgroundReceiveCount); // 增加后台计数器
            _dataAvailableSignal.Release(); // 通知读取任务开始处理数据
        }
        public void ReadTaskHighFrequency(CancellationToken cancellationToken)
        {
            const int BatchSize = 1024; // 批量读取大小

            while (!cancellationToken.IsCancellationRequested)
            {
                _dataAvailableSignal.Wait(cancellationToken); // 等待数据可用信号

                try
                {
                    while (_serialPort?.BytesToRead > 0)
                    {
                        var buffer = new byte[Math.Min(_serialPort.BytesToRead, BatchSize)];
                        int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);

                        if (bytesRead > 0)
                        {
                            // 统计字节数
                            Interlocked.Add(ref _backgroundReceiveBytes, bytesRead);

                            // 实时更新UI
                            string data = _serialPort.Encoding.GetString(buffer, 0, bytesRead);
                            ReceiveLogMessageHighFrequency(data, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        }

                        // 防止CPU占用过高，使用异步延迟
                        Task.Delay(10, cancellationToken);
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

        // 尝试使用手动去替换换行，结果发现会卡死，还不如正则表达式。真是奇怪
        private readonly StringBuilder _receiveLogMessageBuffer = new();

        // 使用 GeneratedRegexAttribute 定义正则表达式
        [GeneratedRegex(@"(\r\n|\r|\n)")]
        private static partial Regex LineBreakRegex();
        
        private void ReceiveLogMessageHighFrequency(string inputData, string timestamp)
        {
            // 替换 \0 为 \\0，并使用生成的正则表达式替换换行符
            var logEntry = LineBreakRegex().Replace(inputData.Replace("\0", "\\0"), $"\r\n[{timestamp}]>> ");

            // 将日志条目追加到缓冲区
            _receiveLogMessageBuffer.Append(logEntry);

            // 异步更新UI
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.LogText?.AppendText(_receiveLogMessageBuffer.ToString());
                _receiveLogMessageBuffer.Clear(); // 清空缓冲区
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
        // 缓存接收日志数据
        private void ReceiveLogMessageSmallBatch(ref string inputData)
        {
            // 缓存当前时间的格式化字符串
            string timestamp = IsDisableTimestamp ? "" : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]";

            // 手动替换 \r 或 \n 为 \r\n，比正则表达式更为高效
            char[] inputChars = inputData.ToCharArray();
            List<char> outputChars = [];
            for (int i = 0; i < inputChars.Length; i++)
            {
                if (inputChars[i] == '\n')
                {
                    if (i > 0 && inputChars[i - 1] == '\r')
                    {
                        continue; // 跳过已经存在的 \r\n
                    }
                    outputChars.Add('\r');
                    outputChars.Add('\n');
                }
                else if (inputChars[i] == '\r')
                {
                    if (i + 1 < inputChars.Length && inputChars[i + 1] == '\n')
                    {
                        outputChars.Add('\r');
                        outputChars.Add('\n');
                        i++; // 跳过下一个字符 \n
                    }
                    else
                    {
                        outputChars.Add('\r');
                        outputChars.Add('\n');
                    }
                }
                else
                {
                    outputChars.Add(inputChars[i]);
                }
            }

            // 将字符列表转换回字符串
            inputData = new string(outputChars.ToArray());

            // 使用 AppendFormat 减少方法调用次数
            lock (_logBuffer)
            {
                if (inputData.EndsWith("\r\n"))
                {
                    _logBuffer.AppendFormat("{0}>> {1}", timestamp, inputData);
                }
                else
                {
                    _logBuffer.AppendFormat("{0}>> {1}\r\n", timestamp, inputData);
                }
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
                    // 格式化数据，用于16进制显示
                    data = FormatData(data);

                    // 打印日志数据
                    ReceiveLogMessageSmallBatch(ref data);

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
                Debug.WriteLine("处理小批量数据的任务已被取消。");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("collection was completed"))
            {
                // 如果集合被标记为完成且队列为空，则正常退出
                Debug.WriteLine("处理小批量数据的任务已完成。");
            }
            catch (Exception ex)
            {
                // 处理其他异常
                Debug.WriteLine($"处理小批量数据时发生错误: {ex.Message}");
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
            const int batchSize = 50; // 每批处理的数据数量
            var convertedBatch = new List<string>(batchSize);
            var extractedBatch = new List<string>(batchSize);
            int lastExtractedTickCount = Environment.TickCount;
            int lastConvertedTickCount = Environment.TickCount;
            int maxProcessingInterval = 300;
            var cleanedData = new StringBuilder();
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

                    // 观测有多少数据
                    Interlocked.Exchange(ref _backgroundPendingNum, _dataToProcessQueue.Count);

                    // 使用手动遍历来移除所有空白字符（包括空格、换行符等）
                    // 优化：避免创建新的字符串实例，直接在现有字符串上操作
                    cleanedData.Clear();
                    foreach (char c in data)
                    {
                        if (!char.IsWhiteSpace(c))
                        {
                            cleanedData.Append(c);
                        }
                    }
                    string extractedData = cleanedData.ToString();


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
                            extractedData = cleanedData.ToString().Substring(StartPosition - 1, Length);
                        }

                        // 将截取的数据添加到待处理的批次中
                        extractedBatch.Add(extractedData);

                        // 动态批次处理逻辑
                        if (extractedBatch.Count >= batchSize || Environment.TickCount - lastExtractedTickCount >= maxProcessingInterval)
                        {
                            AppendBatchToExtractedBuffer(extractedBatch);
                            extractedBatch.Clear();
                            lastExtractedTickCount = Environment.TickCount;
                        }
                    }

                    // -----转换数据-----
                    if (IsConvertedData)
                    {
                        if (IsHexDisplay)
                        {
                            // 将16进制字符串转换为字节数组
                            byte[] bytes = Convert.FromHexString(extractedData);

                            // 检查字节数组长度是否为4
                            if (bytes.Length != 4)
                            {
                                // 错误情况下的处理
                                convertedBatch.Add("长度不足");
                            }
                            else
                            {
                                // 非常重要，因为小端模式下，数组中的数据需要反转才能正确转换
                                if (BitConverter.IsLittleEndian)
                                {
                                    Array.Reverse(bytes);
                                }

                                // 将字节数组转换为32位浮点数
                                convertedBatch.Add(BitConverter.ToSingle(bytes, 0).ToString());
                            }
                        }
                        else
                        {
                            // 不开启十六进制转换
                            try
                            {
                                // 【隐患】这一个函数有大问题，如果是不符合的函数，会直接导致程序崩溃，根本不处理。这个轮子个人感觉造得很差劲
                                // 转换为浮点数
                                if (float.TryParse(extractedData, out float result))
                                {
                                    convertedBatch.Add(result.ToString()); // 添加到批次列表

                                    /// ----发布事件----
                                    if (IsEnableChart)
                                        _eventAggregator.GetEvent<DataReceivedEvent>().Publish(result);
                                }
                                else
                                {

                                    convertedBatch.Add("无法解析为浮点数");

                                    /// ----发布事件----
                                    if (IsEnableChart)
                                        _eventAggregator.GetEvent<DataReceivedEvent>().Publish(0);
                                }
                            }
                            catch (Exception ex)
                            {
                                // 记录异常，不阻塞主线程
                                Debug.WriteLine($"数据转换失败：{ex.Message}");

                                /// ----发布事件----
                                if (IsEnableChart)
                                    _eventAggregator.GetEvent<DataReceivedEvent>().Publish(0);
                            }
                        }

                        // 动态批次处理逻辑
                        if (convertedBatch.Count >= batchSize || Environment.TickCount - lastConvertedTickCount >= maxProcessingInterval)
                        {
                            AppendBatchToConvertedBuffer(convertedBatch);
                            convertedBatch.Clear();
                            lastConvertedTickCount = Environment.TickCount;
                        }
                    }
                }
            }
            finally
            {
                // 最终更新UI上的处理结果（如果有剩余未处理的数据）
                if (convertedBatch.Count > 0)
                {
                    AppendBatchToConvertedBuffer(convertedBatch);
                }
            }
        }


        // -------刷新UI-------
        // 后台计数器
        private int _backgroundReceiveCount = 0;  // 接收数量
        private int _backgroundReceiveBytes = 0;  // 接收字节数
        private int _backgroundPendingNum = 0;    // 待处理数量
        private int _backgroundSendCount = 0;     // 发送数量
        private int _backgroundSendBytes = 0;     // 发送字节数

        private void UpdateUI()
        {
            // 更新UI上的ReceiveNum
            ReceiveNum += Interlocked.Exchange(ref _backgroundReceiveCount, 0); // 假设ReceiveNum是您的数据绑定属性
            ReceiveBytes += Interlocked.Exchange(ref _backgroundReceiveBytes, 0);
            PendingNum = Interlocked.Exchange(ref _backgroundPendingNum, 0);// 想要使用 read方法，但得是 long类型
            SendNum += Interlocked.Exchange(ref _backgroundSendCount, 0);
            SendBytes += Interlocked.Exchange(ref _backgroundSendBytes, 0);

            // 更新日志
            if (_logBuffer.Length > 0)
            {
                lock (_logBuffer)
                {
                    LogText?.AppendText(_logBuffer.ToString());
                    _logBuffer.Clear();
                }
            }

            // 更新截取的数据
            if (_extractedDataBuffer.Length > 0)
            {
                lock (_extractedDataBuffer)
                {
                    ExtractedText?.AppendText(_extractedDataBuffer.ToString());
                    _extractedDataBuffer.Clear();
                }
            }

            // 更新转换后的数据
            if (_convertedDataBuffer.Length > 0)
            {
                lock (_convertedDataBuffer)
                {
                    ConvertedText?.AppendText(_convertedDataBuffer.ToString());
                    _convertedDataBuffer.Clear();
                }
            }

        }

        // 截取数据
        private void AppendBatchToExtractedBuffer(List<string> batch)
        {
            lock (_extractedDataBuffer)
            {
                foreach (var item in batch)
                {
                    _extractedDataBuffer.AppendLine(item);
                }
            }
        }
        private void AppendBatchToConvertedBuffer(List<string> batch)
        {
            lock (_convertedDataBuffer)
            {
                foreach (var item in batch)
                {
                    _convertedDataBuffer.AppendLine(item);
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
        private void SendLogMessage(string inputData)
        {
            // 不知道为什么无法加入异步UI线程，加入后会很卡，可能与异步线程"数据处理任务”的调用有关
            if (IsDisableTimestamp)
            {
                //LogText += $" {message}{Environment.NewLine}";
                lock (_logBuffer)
                {
                    _logBuffer.AppendFormat("<< {0}{1}", inputData, Environment.NewLine);
                }
            }
            else
            {
                // 缓存当前时间的格式化字符串
                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                // 使用 AppendFormat 减少方法调用次数
                lock (_logBuffer)
                {
                    _logBuffer.AppendFormat("[{0}]<< {1}{2}", now, inputData, Environment.NewLine);
                }
            }

        }


        private void SignIndex()
        {
            _signBuffer.Append(ReceiveNum);
            _signBuffer.AppendLine();
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
                if (LogText?.Text != "")
                {
                    File.WriteAllText(logFilePath, LogText?.Text);
                    MessageBox.Show($"日志已成功保存到: {logFilePath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                // 保存处理数据框
                if (IsExtractedData)
                {
                    if (ExtractedText?.Text != "")
                        File.WriteAllText(logFilePath.Replace(".txt", "_extracted.txt"), ExtractedText?.Text);
                    if (ConvertedText?.Text != "")
                        File.WriteAllText(logFilePath.Replace(".txt", "_converted.txt"), ConvertedText?.Text);
                }

                // 保存标记框
                if (_signBuffer.Length != 0)
                {
                    File.WriteAllText(logFilePath.Replace(".txt", "_sign.txt"), _signBuffer.ToString());
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
                _receiveQueue.Clear();//清空接收队列
                _logQueue.Clear();

                // 清除日志框、处理数据框
                LogText?.Clear();
                ExtractedText?.Clear();
                ConvertedText?.Clear();

                // 清除标记框
                _signBuffer.Clear();

                // 清除接收发送数据统计
                ReceiveBytes = 0;
                SendBytes = 0;
                ReceiveNum = 0;
                SendNum = 0;
                PendingNum = 0;
                //MessageBox.Show("已成功清屏", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }



        // ------------------------组件映射------------------------------
        public TextEditor? LogText { get; set; }// 日志框
        public TextEditor? ExtractedText { get; set; }// 提取数据框
        public TextEditor? ConvertedText { get; set; }// 转换数据框



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
                    MessageBox.Show("起始位置不能小于等于0，请重新输入！",
                            "提示",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
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
                    MessageBox.Show("-1即表示全长。长度不能小于-1，请重新输入！",
                            "提示",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
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


        // 是否启用高频接收模式
        public bool IsHighFrequencyReceiving
        {
            get => _isHighFrequencyReceiving;
            set
            {
                _isHighFrequencyReceiving = value;
                if (value)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "注意！高频接收模式下适合接收2KHz以上发送速率的数据，且不提供发送数据、处理数据等操作。" +
                            "如果此时串口为打开状态，那么需要关闭后再重新打开才能使用。",
                            "提示",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    });
                }
                RaisePropertyChanged(nameof(IsHighFrequencyReceiving));
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