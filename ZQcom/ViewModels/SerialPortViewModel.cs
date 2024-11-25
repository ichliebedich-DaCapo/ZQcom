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

/// 编程守则：
/// ①UI更新：一定使用UI异步更新，不然会严重阻塞主线程



namespace ZQcom.ViewModels
{
    public class SerialPortViewModel : ViewModelBase
    {
        // 内部普通变量
        private readonly SerialPortService _serialPortService;      // 串口服务对象
        private SerialPort? _serialPort;                            // 当前打开的串口实例
        //private List<string> _logCache;                             // 日志缓存
        //private FileStream ?_fileStream;                             // 日志文件流
        //private StreamWriter _writer;                               // 日志文件写入对象
        //private long _cacheSizeLimit;                               // 日志缓存大小
        //private string ?_logFilePath;                                // 日志文件路径
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
        private int _selectedBaudRate = 9600;                       // 选中的波特率
        private Parity _selectedParity = Parity.None;               // 选中的校验位
        private StopBits _selectedStopBits = StopBits.One;          // 选中的停止位
        private int _selectedDataBits = 8;                          // 选中的数据位
        private bool _isTimedSendEnabled;                           // 是否启用定时发送
        private int _timedSendInterval = 100;                       // 定时发送的时间间隔（毫秒）
        private CancellationTokenSource? _cancellationTokenSource;  // 用于取消定时发送任务的CancellationTokenSource
        private bool _isProcessData;                                // 是否处理数据
        private int _startPosition = 7;                             // 数据处理的起始位置
        private int _length = 8;                                    // 数据处理的长度
        //private bool _isLogSave=false;                              // 是否保存日志
        private bool _isDisableTimestamp=false;                     // 是否禁用时间戳
        private bool _isForceProcess = false;                       // 是否强制处理数据
        private int _receiveBytes = 0;                              // 接收到的字节数
        private int _sendBytes = 0;                                 // 发送的字节数
        private int _receiveNum = 0;                                // 接收到的数据包数量
        private int _sendNum = 0;                                   // 发送的数据包数量
        private int _pendingQueueSize = 0;                          // 待处理的队列大小

        // 定时器相关
        //private readonly DispatcherTimer _queueSizeUpdateTimer;

        // 线程相关
        private readonly ConcurrentQueue<string> _receiveQueue = new ConcurrentQueue<string>();// 【生产者-消费者模式】
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private CancellationTokenSource? _processingCancellationTokenSource;
        private Task? _processingTask;
        private Task? _logProcessingTask;



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

            //_cacheSizeLimit = 10 * 1024 * 1024; // 默认10 MB
            //InitializeLogFile();


            //--------------前置准备--------------
            // 刷新串口列表
            PopulateSerialPortNames();

            // 绑定接收数据
            _serialPortService.DataReceived += OnDataReceived;

            //发布事件
            _eventAggregator = eventAggregator;


            // --------------线程相关--------------

            // --------------定时器相关-------------- 
            // 初始化定时器，每500毫秒更新一次队列大小
            //_queueSizeUpdateTimer = new DispatcherTimer(DispatcherPriority.Background)
            //{
            //    Interval = TimeSpan.FromMilliseconds(400)
            //};
            //_queueSizeUpdateTimer.Tick += OnQueueSizeUpdateTimerTick;

        }


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
        public bool IsProcessData
        {
            get => _isProcessData;
            set
            {
                _isProcessData = value;
                RaisePropertyChanged(nameof(IsProcessData));
            }
        }

        // 截取数据的起始位置
        public int StartPosition
        {
            get => _startPosition;
            set
            {
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
                _length = value;
                RaisePropertyChanged(nameof(Length));
            }
        }


        //// 是否保存日志
        //public bool IsLogSave
        //{
        //    get => _isLogSave;
        //    set
        //    {
        //        _isLogSave = value;
        //        RaisePropertyChanged(nameof(IsLogSave));
        //    }
        //}


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


        // 是否强制处理数据
        public bool IsForceProcess
        {
            get => _isForceProcess;
            set
            {
                _isForceProcess = value;
                RaisePropertyChanged(nameof(IsForceProcess));
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
        private async void ToggleSerialPort()
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

                    // 启动定时器
                    //_queueSizeUpdateTimer.Start();

                    // 【生产者-消费者模式】启动数据处理任务
                    _processingCancellationTokenSource = new CancellationTokenSource();
                    _processingTask = Task.Run(async () => await ProcessDataQueueAsync(_processingCancellationTokenSource.Token), _processingCancellationTokenSource.Token);

                    // 启动日志打印任务
                    _logProcessingTask = Task.Run(async () => await ProcessLogQueueAsync(_processingCancellationTokenSource.Token), _processingCancellationTokenSource.Token);
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

                // 【生产者-消费者模式】停止数据处理任务
                if (_processingCancellationTokenSource != null && (_processingTask != null || _logProcessingTask != null))
                {
                    _processingCancellationTokenSource.Cancel();

                    try
                    {
                        // 使用异步等待，防止阻塞主线程
                        if (_processingTask != null)
                        {
                            await _processingTask.WithTimeout(TimeSpan.FromSeconds(10)); // 设置超时时间为10秒
                        }

                        if (_logProcessingTask != null)
                        {
                            await _logProcessingTask.WithTimeout(TimeSpan.FromSeconds(10)); // 设置超时时间为10秒
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 任务被取消
                    }
                    catch (TimeoutException)
                    {
                        // 超时处理
                        MessageBox.Show("数据处理任务已超时。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"数据处理任务发生异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"数据处理任务发生异常，丢失引用", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                    _serialPortService.SendData(_serialPort, data );

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

        // 接收打印数据
        private void OnDataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            if (sender is SerialPort sp)
            {
                ReceiveBytes += sp.BytesToRead;
                ++ReceiveNum;

                string data = sp.ReadExisting();
                _receiveQueue.Enqueue(data); // 将接收到的数据放入接收队列
            }
        }


        private async Task ProcessLogQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_receiveQueue.TryDequeue(out var data))
                {
                    data = FormatData(data);
                    LogMessage($">> {data}");
                    _logQueue.Enqueue(data); // 将格式化后的数据放入日志队列
                }
                else
                {
                    await Task.Delay(100, cancellationToken); // 队列为空时等待
                }
            }
        }
        private async Task ProcessDataQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!_logQueue.IsEmpty || !cancellationToken.IsCancellationRequested)
                {
                    if (_logQueue.TryDequeue(out var data))
                    {
                        ProcessData(data);
                        PendingNum = _logQueue.Count; // 更新队列大小
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) { /* 任务被取消 */ }
            catch (Exception ex)
            {
                MessageBox.Show($"数据处理任务发生异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 启用/禁用定时发送
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
        // 发送截取数据
        private void ExtractedDataMessage(string data)
        {
            // 不知道为什么无法加入异步UI线程，加入后会很卡，可能与异步线程"数据处理任务”的调用有关
            ExtractedText += $"{data}{Environment.NewLine}";
        }
        // 发送处理过的数据
        private void ConvertedDataMessage(string data)
        {
            // 不知道为什么无法加入异步UI线程，加入后会很卡，可能与异步线程"数据处理任务”的调用有关
            ConvertedText += $"{data}{Environment.NewLine}";
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
                    if (StartPosition <= 0 && length!=-1)
                    {
                        MessageBox.Show("起始位置不能小于等于0，请重新输入！");
                        IsProcessData = false; // 关闭处理数据
                        return;
                    }

                    // ----根据是否发送16进制数据进行不同处理----
                    string processedData;
                    // 移除空格是因为当开启16进制显示时，字符串中会包含空格、换行
                    string hexDataWithoutSpaces = data.Replace(" ", "").Replace("\n", "").Replace("\r", "");
                    // 最终转换的浮点数据,默认为0
                    float floatValue=0.0f;


                    // 检查数据长度
                    // 增加了判断条件，当长度为-1时，表示从起始位置到末尾
                    if(length==-1)
                    {
                        processedData = hexDataWithoutSpaces; 
                    }
                    else
                    {
                        if (startIndex + length > hexDataWithoutSpaces.Length)
                        {
                            MessageBox.Show("数据长度不足，无法处理！");
                            IsProcessData = false; // 关闭处理数据
                            return;
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
                            if (!IsForceProcess)
                            {
                                MessageBox.Show("十六进制字符串长度不正确，无法转换为32位浮点数！");
                                IsProcessData = false; // 关闭处理数据
                                return;
                            }
                            else
                            {
                                ConvertedDataMessage("长度不足");
                            }
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
                        if (!IsForceProcess)
                        {
                            MessageBox.Show("无法将数据转换为浮点数！");
                            IsProcessData = false; // 关闭处理数据
                            return;
                        }
                        else
                        {
                            ConvertedDataMessage("无法转换");
                        }
                    }
                }

                    // 发布事件
                    _eventAggregator.GetEvent<DataReceivedEvent>().Publish(floatValue);
                }
                catch (FormatException ex)
                {
                    // 其他异常
                        MessageBox.Show($"处理数据发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        IsProcessData = false;// 关闭处理数据
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
                if (LogText != "")
                    File.WriteAllText(logFilePath, LogText);
                // 保存处理数据框
                if (IsProcessData)
                {
                    if (ExtractedText != "")
                        File.WriteAllText(logFilePath.Replace(".txt", "_extracted.txt"), ExtractedText);
                    if (ConvertedText != "")
                        File.WriteAllText(logFilePath.Replace(".txt", "_converted.txt"), ConvertedText);
                }
                if (ReceiveText != "" || ExtractedText != "" || ConvertedText != "")
                    MessageBox.Show($"日志已成功保存到: {logFilePath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
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

                // 清除接收发送数据统计
                ReceiveBytes = 0;
                SendBytes = 0;
                ReceiveNum = 0;
                SendNum = 0;
                //MessageBox.Show("已成功清屏", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
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