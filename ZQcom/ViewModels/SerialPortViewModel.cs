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


        public event EventHandler<string>? DataReceived;            // 数据接收事件
        public ObservableCollection<string>? AvailablePorts { get; set; } // 可用的串口列表

        // 事件
        private readonly IEventAggregator _eventAggregator;




        // ------------------------初始化------------------------------
        public SerialPortViewModel(IEventAggregator eventAggregator)
        {
            _serialPortService = new SerialPortService();
            SerialPortNames = [];
            BaudRateOptions = [9600, 19200, 38400, 57600, 115200];
            ParityOptions = [Parity.None, Parity.Odd, Parity.Even, Parity.Mark, Parity.Space];
            StopBitOptions = [StopBits.None, StopBits.One, StopBits.Two, StopBits.OnePointFive];
            DataBitOptions = [5, 6, 7, 8];

            //_cacheSizeLimit = 10 * 1024 * 1024; // 默认10 MB
            //InitializeLogFile();

            // 刷新串口列表
            PopulateSerialPortNames();

            // 绑定接收数据
            _serialPortService.DataReceived += OnDataReceived;

            //发布事件
            _eventAggregator = eventAggregator;


            // 用于滚动数据，可能还有用
            //_serialService.DataReceived += (s, e) =>
            //{
            //    LogText += e + Environment.NewLine;
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        // Scroll to bottom
            //        var scrollViewer = FindVisualChild<ScrollViewer>(Application.Current.MainWindow);
            //        scrollViewer.ScrollToBottom();
            //    });
            //};
        }


        // ------------------------数据绑定------------------------------
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
                    _serialPort = _serialPortService.OpenPort(SelectedSerialPort, baudRate, SelectedParity, SelectedStopBits, SelectedDataBits);
                    OpenCloseButtonText = "关闭串口";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开串口失败: {ex.Message}");
                }
            }
            else
            {
                _serialPortService.ClosePort(_serialPort);
                _serialPort = null;
                OpenCloseButtonText = "打开串口";
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
                        _serialPortService.SendData(_serialPort, Convert.FromHexString(data));
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
                    _serialPortService.SendData(_serialPort, data + (AddNewline ? "\r\n" : ""));
                }
                // 发送到日志框内
                LogMessage($"<< {data}");
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

        // 接收数据
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var sp = (SerialPort)sender;
            string data = sp.ReadExisting();
            // 格式化数据
            data = FormatData(data);
            // 输出到对应框中
            LogMessage($">> {data}");
            ProcessData(data);// 处理数据
        }



        // 判断是否为有效的十六进制字符串
        public bool IsHexString(string input)
        {
            // 去除字符串中的所有空格
            input = input.Replace(" ", "");

            // 正则表达式匹配16进制字符
            return Regex.IsMatch(input, "^[0-9A-Fa-f]+$");
        }
        // 格式化数据（由于判断是否为16进制字符串）
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
            // 去除所有空格
            hexString = hexString.Replace(" ", "");

            // 将字符串按每两个字符分割，并用空格连接
            return string.Join(" ", Enumerable.Range(0, hexString.Length / 2).Select(i => hexString.Substring(i * 2, 2)));
        }



        // 发送日志消息
        private void LogMessage(string message)
        {
            LogText += $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
        }
        // 发送截取数据
        private void ExtractedDataMessage(string data)
        {

            ExtractedText += $"{data}{Environment.NewLine}";
        }
        // 发送处理过的数据
        private void ConvertedDataMessage(string data)
        {
            ConvertedText += $"{data}{Environment.NewLine}";

        }

        // 发送到处理数据框
        // 只有当IsProcessData 为 true 时才进行处理
        // 开启16进制显示时会以16进制数据处理，不开启则以普通字符串处理
        private void ProcessData(string data)
        {
            if (IsProcessData)
            {
                try
                {
                    // 获取数据起始位置和长度
                    int startIndex = StartPosition - 1; // 起始位置从1开始
                    int length = Length;

                    // 检查起始位置
                    if (StartPosition <= 0)
                    {
                        MessageBox.Show("起始位置不能小于等于0，请重新输入！");
                        IsProcessData = false; // 关闭处理数据
                        return;
                    }

                    // ----根据是否发送16进制数据进行不同处理----
                    string processedData;
                    // 移除空格是因为当开启16进制显示时，字符串中会包含空格、换行
                    string hexDataWithoutSpaces = data.Replace(" ", "").Replace("\n", "").Replace("\r", "");
                    // 最终转换的浮点数据
                    float floatValue;


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





                    // 由于前面已经经过是否显示16进制处理过了，这个判断倒显得怪异
                    if (IsHexDisplay)
                    {
                        // 将16进制字符串转换为字节数组
                        byte[] bytes = Convert.FromHexString(processedData);

                        // 非常重要，因为小端模式下，数组中的数据需要反转才能正确转换
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(bytes);
                        }

                        // 将字节数组转换为32位浮点数
                        floatValue = BitConverter.ToSingle(bytes, 0);


                        ConvertedDataMessage(floatValue.ToString());
                    }
                    else
                    {
                        // 尝试直接将字符串转换为32位浮点数
                        if (float.TryParse(processedData, out float result))
                        {
                            floatValue = result;
                            ConvertedDataMessage(result.ToString());
                        }
                        else
                        {
                            MessageBox.Show("无法将数据转换为浮点数！");
                            IsProcessData = false; // 关闭处理数据
                            return;
                        }
                    }
                    // 发布事件
                    _eventAggregator.GetEvent<DataReceivedEvent>().Publish(floatValue);
                }
                catch (FormatException ex)
                {
                    // 其他异常
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"处理数据发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        IsProcessData = false;// 关闭处理数据
                    });
                }
            }
        }


        // 初始化日志文件
        //private void InitializeLogFile()
        //{
        //    _logFilePath = GenerateLogFileName();
        //    _fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        //    _writer = new StreamWriter(_fileStream) { AutoFlush = true };
        //}
        // 生成日志文件名
        private string GenerateLogFileName()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"Log/log_{timestamp}.txt";
        }
        // 确保目录存在
        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        // 保存日志
        private void SaveLog()
        {
            try
            {
                string logFilePath = GenerateLogFileName();
                EnsureDirectoryExists(Path.GetDirectoryName(logFilePath));
                // 保存日志框
                File.WriteAllText(logFilePath, LogText);
                // 保存处理数据框
                if(IsProcessData)
                {
                    File.WriteAllText(logFilePath.Replace(".txt", "_extracted.txt"), ExtractedText);
                    File.WriteAllText(logFilePath.Replace(".txt", "_converted.txt"), ConvertedText);
                }
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
            Process.Start(new ProcessStartInfo("explorer.exe", logDirectory));
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
        // 忘记这是干什么的了，先注释保留
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