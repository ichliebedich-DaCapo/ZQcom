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
        private int _maxChartPoints;                                  // 图表最大数据点数
        private bool _isDisableAnimation;                           // 禁用动画
        private List<double> _dataDisplayChartValues = [];                  // 数据显示图表数据
        private List<double> _signIndexValues = [];                         // 标记索引

        // 用于订阅事件
        private readonly IEventAggregator _eventAggregator;

        // 初始化
        public ChartViewModel(IEventAggregator eventAggregator,ChartSettings chartSettings)
        {
            // 初始化配置
            _maxChartPoints=chartSettings.MaxChartPoints;
            _isDisableAnimation = chartSettings.IsDisableAnimation;

            _chartModel = new ChartModel();
            // 订阅事件
            _eventAggregator = eventAggregator;
            // 加上ThreadOption.BackgroundThread，防止阻塞主线程，导致死锁
            _eventAggregator.GetEvent<DataReceivedEvent>().Subscribe(AddDataPoint, ThreadOption.BackgroundThread);
        }




        // ------------------------私有方法------------------------------
        public ChartSettings GetSettings()
        {
            return new ChartSettings
            {
                MaxChartPoints = _maxChartPoints,
                IsDisableAnimation = _isDisableAnimation
            };
        }

        public void SetSettings(ChartSettings settings)
        {
            _maxChartPoints = settings.MaxChartPoints;
            _isDisableAnimation = settings.IsDisableAnimation;
        }


        // 添加数据点
        public void AddDataPoint(double value)
        {

            // 由于起始索引为0，索引没有用">="
            if (ChartModel.GetDataPointCount() > AxisXMaxValue)
                RemoveLastDataPoint();// 删除最后一条数据
            ChartModel.AddDataPoint(value);

        }
        // 删除最后一条数据
        public void RemoveLastDataPoint()
        {
            ChartModel.RemoveDataPoint(0);
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

        /// <summary>
        /// 进行数据显示
        /// </summary>
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
                // 加载并预处理数据
                LoadAndProcessData(filePath);

                string signFilePath = Path.Combine(Path.GetDirectoryName(filePath), $"{string.Join("_", Path.GetFileNameWithoutExtension(filePath).Split('_').Take(3))}_sign.txt");

                // 加载标记数据
                if (File.Exists(signFilePath))
                    LoadSignIndexData(signFilePath);

                // ----数据处理完成后打开图表窗口----
                var chartWindow = new DataDisplayChartWindow(_dataDisplayChartValues, _signIndexValues);


                //chartWindow.DataContext = this; // 将当前视图模型设置为数据上下文
                chartWindow.Show();
            }
        }


        // 从文件中加载数据并预处理
        private void LoadAndProcessData(string filePath)
        {
            _dataDisplayChartValues = File.ReadAllLines(filePath)
                                           .SelectMany(ExtractNumbersFromLine)
                                           .Where(number => !double.IsNaN(number))
                                           .ToList();
        }

        private IEnumerable<double> ExtractNumbersFromLine(string line)
        {
            // 去除前后空白字符
            line = line.Trim();

            // 尝试从 ">>" 后面提取数字
            if (line.Contains(">>"))
            {
                var parts = line.Split(new[] { ">>" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (double.TryParse(part.Trim(), out double number))
                    {
                        yield return number;
                    }
                }
            }
            // 尝试从时间戳后面的数字提取
            else if (line.Contains(':'))
            {
                var parts = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (double.TryParse(part.Trim(), out double number))
                    {
                        yield return number;
                    }
                }
            }
            // 尝试直接解析整行
            else
            {
                if (double.TryParse(line, out double number))
                {
                    yield return number;
                }
            }
        }

        // 从文件中加载标记数据
        private void LoadSignIndexData(string filePath)
        {
            _signIndexValues = File.ReadAllLines(filePath)
                                      .SelectMany(ExtractNumbersFromLine)
                                      .Where(number => !double.IsNaN(number))
                                      .ToList();
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




        // 图表最大数据点数属性
        public int AxisXMaxValue
        {
            get => _maxChartPoints - 1;
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

    }
}