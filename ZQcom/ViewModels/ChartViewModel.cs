using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LiveCharts;
using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows.Input;
using ZQcom.Models;

namespace ZQcom.ViewModels
{
    public class ChartViewModel : ViewModelBase
    {
        private ChartModel _chartModel;                             // 图表数据
        private bool _isEnableChart=false;                          // 启用图表
        private int _maxChartPoints=100;                            // 图表最大数据点数


        public ChartModel ChartModel
        {
            get => _chartModel;
            set
            {
                _chartModel = value;
            }
        }

        // 初始化
        public ChartViewModel()
        {
            _chartModel = new ChartModel();




        }

        // ------------------------数据绑定------------------------------
        // 启用图表
        public bool IsEnableChart
        {
            get => _isEnableChart;
            set
            {
                _isEnableChart = value;
                RaisePropertyChanged(nameof(IsEnableChart));
            }
        }

        // 图表最大数据点数
        public int MaxChartPoints
        {
            get => _maxChartPoints;
            set
            {
                _maxChartPoints = value;
                RaisePropertyChanged(nameof(MaxChartPoints));
            }
        }





        // ------------------------绑定事件------------------------------
        public ICommand DebugCommand => new RelayCommand(DebugAddPoints);

        public void DebugAddPoints()
        {
            ChartModel.AddDataPoint(DateTime.Now.Millisecond%100);
        }
    }
}