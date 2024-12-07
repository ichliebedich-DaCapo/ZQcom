using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ScottPlot;
using ZQcom.ViewModels;
using ScottPlot.WPF;
using MathNet.Numerics.IntegralTransforms;
using System.Windows.Media.Imaging;
using System.IO;
using Newtonsoft.Json;
using ZQcom.Models;

namespace ZQcom.Views
{
    public partial class DataDisplayChartWindow : Window
    {
        private List<double> _dataValues;
        private readonly List<double> _signIndex;

        private DataDisplayChartViewModel _dataDisplayChartViewModel;

        // 数据打印配置文件路径
        private readonly string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "DataDisplayChartSettings.json");

        public DataDisplayChartWindow(List<double> dataValues, List<double> signIndex)
        {
            _dataValues = dataValues;
            _signIndex = signIndex;

            InitializeComponent();
           

            // 确保目录存在
            string? directoryPath = Path.GetDirectoryName(settingsFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 读取配置文件
            DataDisplayChartSettings settings;
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    var json = File.ReadAllText(settingsFilePath);
                    settings = JsonConvert.DeserializeObject<DataDisplayChartSettings>(json) ?? new DataDisplayChartSettings();
                }
                else
                {
                    settings = new DataDisplayChartSettings(); // 使用默认配置
                }
            }
            catch (Exception ex)
            {
                // 记录异常或适当处理
                MessageBox.Show($"加载设置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                settings = new DataDisplayChartSettings(); // 使用默认配置
            }

            _dataDisplayChartViewModel = new DataDisplayChartViewModel(dataValues,signIndex,settings);

            // 创建并绑定视图模型
            DataContext = _dataDisplayChartViewModel;// 将 DataContext 设置为当前窗口实例

            // --------传入组件-------
            _dataDisplayChartViewModel._dataChartPlot = DataChartPlot;

            Loaded += Window_Loaded;
            
        }


        // ----------------------------窗口相关方法----------------------------
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 生成X轴数据
            List<double> xData = Enumerable.Range(0, _dataValues.Count).Select(i => (double)i).ToList();

            // 检查数据是否为空
            if (_dataValues.Count == 0)
            {
                MessageBox.Show("没有任何数据。");
                return;
            }

            // 添加数据到现有的 WpfPlot 控件
            DataChartPlot.Plot.Add.Scatter(xData, _dataValues);
            DataChartPlot.Plot.XLabel("Index");
            DataChartPlot.Plot.YLabel("Value");

            // 添加标记
            foreach (double index in _signIndex)
            {
                DataChartPlot.Plot.Add.VerticalLine(index);
            }

            DataChartPlot.Plot.Axes.AutoScale();
            // 刷新 WpfPlot 控件以显示更新后的图表
            DataChartPlot.Refresh();
        }


    }
}



