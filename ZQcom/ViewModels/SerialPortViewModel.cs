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
    public class SerialPortViewModel : BaseViewModel
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
            //_serialService = new SerialService();
            AvailablePorts = new ObservableCollection<string>(SerialPort.GetPortNames());
            //OpenCommand = new RelayCommand(OpenSerialPort);

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
                //RaisePropertyChanged(nameof(SelectedBaudRate));
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
                //RaisePropertyChanged(nameof(SelectedParity));
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
                //RaisePropertyChanged(nameof(SelectedStopBits));
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
                //RaisePropertyChanged(nameof(SelectedDataBits));
            }
        }

        // 启闭串口按钮_文本
        public string OpenCloseButtonText
        {
            get => _openCloseButtonText;
            set
            {
                _openCloseButtonText = value;
                //RaisePropertyChanged(nameof(OpenCloseButtonText));
            }
        }

        // 发送数据
        public string SendDataText
        {
            get => _sendDataText;
            set
            {
                _sendDataText = value;
                //RaisePropertyChanged(nameof(SendDataText));
            }
        }

        // 日志框
        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                //RaisePropertyChanged(nameof(LogText));
            }
        }


        // 接收数据框
        public string ReceiveText
        {
            get => _receiveText;
            set
            {
                _receiveText = value;
                //RaisePropertyChanged(nameof(ReceiveText));
            }
        }

        // 截取数据框
        public string ExtractedText
        {
            get => _extractedText;
            set
            {
                _extractedText = value;
                //RaisePropertyChanged(nameof(ExtractedText));
            }
        }


        // 转换数据框
        public string ConvertedText
        {
            get => _convertedText;
            set
            {
                _convertedText = value;
                //RaisePropertyChanged(nameof(ConvertedText));
            }
        }

        // 十六进制
        public bool IsHexSend
        {
            get => _isHexSend;
            set
            {
                _isHexSend = value;
                //RaisePropertyChanged(nameof(IsHexSend));
            }
        }

        // 十六进制显示
        public bool IsHexDisplay
        {
            get => _isHexDisplay;
            set
            {
                _isHexDisplay = value;
                //RaisePropertyChanged(nameof(IsHexDisplay));
            }
        }

        // 添加回车
        public bool AddNewline
        {
            get => _addNewline;
            set
            {
                _addNewline = value;
                //RaisePropertyChanged(nameof(AddNewline));
            }
        }


        // 定时发送
        public bool IsTimedSendEnabled
        {
            get => _isTimedSendEnabled;
            set
            {
                _isTimedSendEnabled = value;
                //RaisePropertyChanged(nameof(IsTimedSendEnabled));
            }
        }

        // 定时发送间隔
        public int TimedSendInterval
        {
            get => _timedSendInterval;
            set
            {
                _timedSendInterval = value;
                //RaisePropertyChanged(nameof(TimedSendInterval));
            }
        }


        // 数据处理
        public bool IsProcessData
        {
            get => _isProcessData;
            set
            {
                _isProcessData = value;
                //RaisePropertyChanged(nameof(IsProcessData));
            }
        }

        // 截取数据的起始位置
        public int StartPosition
        {
            get => _startPosition;
            set
            {
                _startPosition = value;
                //RaisePropertyChanged(nameof(StartPosition));
            }
        }

        // 截取数据的长度
        public int Length
        {
            get => _length;
            set
            {
                _length = value;
                //RaisePropertyChanged(nameof(Length));
            }
        }



        //public ICommand ToggleSerialPortCommand => new RelayCommand(ToggleSerialPort);
        //public ICommand SendDataCommand => new RelayCommand(SendData);
        //public ICommand RefreshSerialPortsCommand => new RelayCommand(PopulateSerialPortNames);
        //public ICommand ToggleTimedSendCommand => new RelayCommand(ToggleTimedSend);

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