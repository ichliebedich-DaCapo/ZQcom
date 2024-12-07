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
        private DataDisplayChartViewModel _dataDisplayChartViewModel;

        // 数据打印配置文件路径
        private readonly string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "DataDisplayChartSettings.json");

        public DataDisplayChartWindow(List<double> dataValues, List<double> signIndex)
        {
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



            // 创建并绑定视图模型
            _dataDisplayChartViewModel = new DataDisplayChartViewModel(dataValues, signIndex, settings);
            DataContext = _dataDisplayChartViewModel;// 将 DataContext 设置为当前窗口实例


            // --------传入组件-------
            _dataDisplayChartViewModel.DataChartPlot = DataChartPlot;


            // 初始化图表
            _dataDisplayChartViewModel.InitChart();
        }


        // ----------------------------窗口相关方法----------------------------
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 保存设置
            var settings = _dataDisplayChartViewModel.GetSettings();

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(settingsFilePath, json);
        }



    }
}



