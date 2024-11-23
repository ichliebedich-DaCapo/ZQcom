using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LiveCharts;
using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Input;
using Prism.Events;
using ZQcom.Models;
using System.Net;
using ZQcom.Events;

namespace ZQcom.ViewModels
{
    public class ChartViewModel : ViewModelBase
    {
        private ChartModel _chartModel;                             // 图表数据
        private bool _isEnableChart = false;                          // 启用图表,默认不可视
        private int _maxChartPoints = 100;                            // 图表最大数据点数
        private bool _isDisableAnimation = false;                           // 禁用动画

        // 用于订阅事件
        private readonly IEventAggregator _eventAggregator;

        // 初始化
        public ChartViewModel(IEventAggregator eventAggregator)
        {
            _chartModel = new ChartModel();
            // 订阅事件
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<DataReceivedEvent>().Subscribe(AddDataPoint);
        }

        // ------------------------数据绑定------------------------------
        // 图表数据
        public ChartModel ChartModel
        {
            get => _chartModel;
            set
            {
                _chartModel = value;
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

        // 图表最大数据点数属性
        public int AxisXMaxValue
        {
            get => _maxChartPoints;
        }
        // 图表最大数据点数
        public int MaxDisplayPoints
        {
            get => _maxChartPoints;
            set
            {
                _maxChartPoints = value;
                RaisePropertyChanged(nameof(MaxDisplayPoints));
                RaisePropertyChanged(nameof(AxisXMaxValue));
            }
        }


        // 禁用动画属性
        public bool DisableAnimation
        {
            get => _isDisableAnimation;
        }
        // 是否禁用动画
        public bool IsDisableAnimation
        {
            get => _isDisableAnimation;
            set
            {
                _isDisableAnimation = value;
                RaisePropertyChanged(nameof(IsDisableAnimation));
                RaisePropertyChanged(nameof(DisableAnimation));
            }
        }
        // ------------------------私有方法------------------------------
        private async Task AddDataPointsAsync(double value)
        {
            // 异步处理事件
            await Task.Run(() =>
            {
                AddDataPoint(value);
            });
        }

        // 添加数据点
        public void AddDataPoint(double value)
        {
            if (IsEnableChart)
            {
                // 由于起始索引为0，索引没有用">="
                if (ChartModel.GetDataPointCount() > MaxDisplayPoints)
                    RemoveLastDataPoint();// 删除最后一条数据
                ChartModel.AddDataPoint(value);
            }
        }
        // 删除最后一条数据
        public void RemoveLastDataPoint()
        {
            if(IsEnableChart)
            {
                ChartModel.RemoveDataPoint(0);
            }
        }


        // ------------------------绑定事件------------------------------
        public ICommand ClearChartCommand => new RelayCommand(ClearChart);




        // 清除图表
        public void ClearChart()
        {
            ChartModel.Clear();
        }



        // // 用于调试
        //public ICommand DebugCommand => new RelayCommand(DebugAddPoints);

        //public void DebugAddPoints()
        //{
        //    AddDataPoint(DateTime.Now.Millisecond % 100);
        //}
    }
}