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

namespace ZQcom.ViewModels
{
    public class SerialPortViewModel : ViewModelBase
    {
        private readonly SerialPortService _serialPortService;
        private SerialPort _serialPort;
        private string _openCloseButtonText = "打开串口";
        private string _sendDataText = "01040000000271CB";
        private string _logText = "";
        private string _receiveText = "";
        private string _extractedText = "";
        private string _convertedText = "";
        private bool _isHexSend;
        private bool _isHexDisplay;
        private bool _addNewline;
        private string _selectedSerialPort = string.Empty;
        private int _selectedBaudRate = 9600;
        private Parity _selectedParity = Parity.None;
        private StopBits _selectedStopBits = StopBits.One;
        private int _selectedDataBits = 8;
        private bool _isTimedSendEnabled;
        private int _timedSendInterval = 1000;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isProcessData;
        private int _startPosition = 0;
        private int _length = 0;

        public event EventHandler<string> DataReceived;

        public ObservableCollection<string> AvailablePorts { get; set; }


        // ------------------------初始化------------------------------
        public SerialPortViewModel()
        {
            _serialPortService = new SerialPortService();
            SerialPortNames = new ObservableCollection<string>();
            BaudRateOptions = new ObservableCollection<int> { 9600, 19200, 38400, 57600, 115200 };
            ParityOptions = new ObservableCollection<Parity> { Parity.None, Parity.Odd, Parity.Even, Parity.Mark, Parity.Space };
            StopBitOptions = new ObservableCollection<StopBits> { StopBits.None, StopBits.One, StopBits.Two, StopBits.OnePointFive };
            DataBitOptions = new ObservableCollection<int> { 5, 6, 7, 8 };

            PopulateSerialPortNames();

            _serialPortService.DataReceived += OnDataReceived;


            // 先留着
            //_serialService = new SerialPortService();
            //AvailablePorts = new ObservableCollection<string>(SerialPort.GetPortNames());
            //OpenCommand = new RelayCommand(OpenSerialPort);


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


        // 数据处理
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


        // ---------------------------------绑定事件----------------------------------------
        public ICommand RefreshSerialPortsCommand => new RelayCommand(PopulateSerialPortNames);
        public ICommand ToggleSerialPortCommand => new RelayCommand(ToggleSerialPort);
        public ICommand SendDataCommand => new RelayCommand(SendData);
        public ICommand ToggleTimedSendCommand => new RelayCommand(ToggleTimedSend);


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
        private async void SendData()
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                MessageBox.Show("请先打开串口。");
                return;
            }

            var data = SendDataText;
            if (!string.IsNullOrEmpty(data))
            {
                if (IsHexSend)
                {
                    byte[] bytes = HexStringToByteArray(data);
                    _serialPortService.SendData(_serialPort, bytes);
                }
                else
                {
                    _serialPortService.SendData(_serialPort, data + (AddNewline ? "\r\n" : ""));
                }
                LogMessage($"发送: >> {data}");
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

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimedSendInterval, _cancellationTokenSource.Token);
                    SendData();
                }
            }
        }

        // 接收数据
        private void OnDataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            var sp = (SerialPort)sender;
            string data = sp.ReadExisting();
            LogMessage($"接收: << {data}");
            ReceiveText += FormatData(data);

            if (IsProcessData && StartPosition >= 0 && Length > 0)
            {
                ExtractedText = data.Substring(StartPosition, Math.Min(Length, data.Length - StartPosition));
            }

            // 滚动到最底部
            ScrollToBottom();
        }




        // 十六进制字符串转字节数组
        private byte[] HexStringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        // 格式化数据
        private string FormatData(string data)
        {
            if (IsHexDisplay)
            {
                return BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(data)).Replace("-", " ");
            }
            return data;
        }



        // 发送日志消息
        private void LogMessage(string message)
        {
            LogText += $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
        }

        private void ScrollToBottom()
        {
            // 这里假设 TbxLog 和 TbxReceiveData 是在 View 中定义的控件
            // 实际滚动操作应该在 View 中实现
        }






        // ---------------------------------------------------------------------
        // ---------------------------------------------------------------------




        //public string LogText
        //{
        //    get => _logText;
        //    set
        //    {
        //        _logText = value;
        //        OnPropertyChanged(nameof(LogText));
        //    }
        //}

        public ICommand OpenCommand { get; set; }



        //private void OpenSerialPort(object parameter)
        //{
        //    var model = new SerialPortModel
        //    {
        //        PortName = SelectedPort,
        //        BaudRate = 9600,
        //        Parity = Parity.None,
        //        DataBits = 8,
        //        StopBits = StopBits.One
        //    };

        //    _serialService.OpenAsync(model).Wait();
        //}

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    return t;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }

        private void OnDataReceived(string data)
        {
            DataReceived?.Invoke(this, data);
        }
    }
}