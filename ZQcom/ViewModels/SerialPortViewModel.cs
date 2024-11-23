using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Controls;
using System.IO.Ports;
using System.Windows.Media;
using System.Windows;
using ZQcom.Models;
using ZQcom.Services;

namespace ZQcom.ViewModels
{
    public class SerialPortViewModel : BaseViewModel
    {
        private readonly SerialService _serialService;
        private string _selectedPort;
        private string _logText;

        public event EventHandler<string> DataReceived;

        public ObservableCollection<string> AvailablePorts { get; set; }
        public string SelectedPort
        {
            get => _selectedPort;
            set
            {
                _selectedPort = value;
                OnPropertyChanged(nameof(SelectedPort));
            }
        }

        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                OnPropertyChanged(nameof(LogText));
            }
        }

        public ICommand OpenCommand { get; set; }

        public SerialPortViewModel()
        {
            _serialService = new SerialService();
            AvailablePorts = new ObservableCollection<string>(SerialPort.GetPortNames());
            OpenCommand = new RelayCommand(OpenSerialPort);

            _serialService.DataReceived += (s, e) =>
            {
                LogText += e + Environment.NewLine;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Scroll to bottom
                    var scrollViewer = FindVisualChild<ScrollViewer>(Application.Current.MainWindow);
                    scrollViewer.ScrollToBottom();
                });
            };
        }

        private void OpenSerialPort(object parameter)
        {
            var model = new SerialPortModel
            {
                PortName = SelectedPort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One
            };

            _serialService.OpenAsync(model).Wait();
        }

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