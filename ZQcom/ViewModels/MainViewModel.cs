using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace ZQcom.ViewModels
{
    public class MainViewModel
    {
        private SerialPortViewModel _serialPortViewModel;
        private ChartViewModel _chartViewModel;

        public MainViewModel()
        {
            _serialPortViewModel = new SerialPortViewModel();
            _chartViewModel = new ChartViewModel();

            _serialPortViewModel.DataReceived += OnDataReceived;
        }

        public SerialPortViewModel SerialPortViewModel => _serialPortViewModel;
        public ChartViewModel ChartViewModel => _chartViewModel;

        private void OnDataReceived(object sender, string data)
        {
            //_chartViewModel.AddDataPoint(double.Parse(data));
        }
    }
}