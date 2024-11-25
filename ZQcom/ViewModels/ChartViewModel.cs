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
using Microsoft.Win32;
using System.IO;
using ZQcom.Views;

namespace ZQcom.ViewModels
{
    public class ChartViewModel : ViewModelBase
    {
        private ChartModel _chartModel;                                     // 图表数据
        private bool _isEnableChart = false;                                // 启用图表,默认不可视
        private int _maxChartPoints = 100;                                  // 图表最大数据点数
        private bool _isDisableAnimation = false;                           // 禁用动画
        private ChartValues<double> _dataDisplayChartValues = new ChartValues<double>();// 数据显示图表数据


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
            get => _maxChartPoints-1;
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


        // 显示在图表中的数据
        // 【隐患】没有合理用上ChartModel，如果这般做下去，后面会很乱
        public ChartValues<double> DataDisplayChartValues
        {
            get => _dataDisplayChartValues;
            set
            {
                _dataDisplayChartValues = value;
                RaisePropertyChanged(nameof(DataDisplayChartValues));
            }
        }


        // ------------------------私有方法------------------------------
        // 异步添加数据点,先放着
        //private async Task AddDataPointsAsync(double value)
        //{
        //    // 异步处理事件
        //    await Task.Run(() =>
        //    {
        //        AddDataPoint(value);
        //    });
        //}

        // 添加数据点
        public void AddDataPoint(double value)
        {
            if (IsEnableChart)
            {
                // 由于起始索引为0，索引没有用">="
                if (ChartModel.GetDataPointCount() > AxisXMaxValue)
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
        public ICommand FFTCommand => new RelayCommand(FFTChart);

        public ICommand DataDisplayChartCommand => new RelayCommand(ExecuteDataDisplayCommand);

        // 清除图表
        public void ClearChart()
        {
            ChartModel.Clear();
        }

        // FFT处理
        public void FFTChart()
        {
            ChartModel.FFT();
        }

        // 进行数据显示
        private void ExecuteDataDisplayCommand()
        {
            // 获取当前目录
            string currentDirectory = System.IO.Directory.GetCurrentDirectory();
            string logDirectory = System.IO.Path.Combine(currentDirectory, "Log");

            // 检查并创建 Log 目录
            if (!System.IO.Directory.Exists(logDirectory))
            {
                System.IO.Directory.CreateDirectory(logDirectory);
            }

            // 执行命令时调用的方法
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                RestoreDirectory = true,
                InitialDirectory = logDirectory // 设置初始目录为 Log 目录
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                LoadAndProcessData(filePath);

                // 数据处理完成后打开图表窗口
                var chartWindow = new DataDisplayChartWindow();
                chartWindow.DataContext = this; // 将当前视图模型设置为数据上下文
                chartWindow.Show();
            }
        }


        private void LoadAndProcessData(string filePath)
        {
            // 从文件路径加载数据并处理
            // 只保留可以转换为浮点数的有效数据
            List<double> dataPoints = File.ReadAllLines(filePath)
                                          .Where(line => double.TryParse(line, out _))
                                          .Select(line => double.Parse(line))
                                          .ToList();

            // 使用图表模型的AddDataPoint方法逐个添加数据点
            foreach (var point in dataPoints)
            {
                _dataDisplayChartValues.Add(point);
            }
        }

        // // 用于调试
        //public ICommand DebugCommand => new RelayCommand(DebugAddPoints);

        //public void DebugAddPoints()
        //{
        //    AddDataPoint(DateTime.Now.Millisecond % 100);
        //}
    }
}