using System.Linq;
using System.Windows;
using ScottPlot;
using ZQcom.ViewModels;
using ScottPlot.WPF;

namespace ZQcom.Views
{
    public partial class DataDisplayChartWindow : Window
    {
        public DataDisplayChartWindow()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ChartViewModel;
            if (viewModel != null)
            {
                // 获取数据
                var data = viewModel.DataDisplayChartValues.ToArray();

                // 生成X轴数据
                double[] xData = Enumerable.Range(0, data.Length).Select(i => (double)i).ToArray();

                // 添加数据到现有的 WpfPlot 控件
                plot.Plot.Add.Scatter(xData, data);// 真他宝贝沙雕，找半天API才找到，API名称就他宝贝这么喜欢变吗
                plot.Plot.XLabel("Index");
                plot.Plot.YLabel("Value");
                // 刷新 WpfPlot 控件以显示更新后的图表
                plot.Refresh();
            }
        }
    }
}