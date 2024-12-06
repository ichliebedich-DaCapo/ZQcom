using Prism.Events;
using System;
using ZQcom.Services;
using ZQcom.ViewModels;

namespace ZQcom.ViewModels
{
    public class MainViewModel 
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly SerialPortViewModel _serialPortViewModel;
        private readonly ChartViewModel _chartViewModel;

        public MainViewModel()
        {
            _eventAggregator = new EventAggregator();
            _serialPortViewModel = new SerialPortViewModel(_eventAggregator);
            _chartViewModel = new ChartViewModel(_eventAggregator);

            // 调试信息
            System.Diagnostics.Debug.WriteLine("MainViewModel initialized");
        }

        public SerialPortViewModel SerialPortViewModel => _serialPortViewModel;
        public ChartViewModel ChartViewModel => _chartViewModel;
    }
}