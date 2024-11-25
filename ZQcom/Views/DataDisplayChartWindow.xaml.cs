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
                var data = viewModel.DataDisplayChartValues.ToList();

                // 生成X轴数据
                List<double> xData = Enumerable.Range(0, data.Count).Select(i => (double)i).ToList();

                // 检查数据是否为空
                if (data.Count == 0)
                {
                    MessageBox.Show("No data to display.");
                    return;
                }

                // 调试输出
                System.Diagnostics.Debug.WriteLine("X Data: " + string.Join(", ", xData));
                System.Diagnostics.Debug.WriteLine("Y Data: " + string.Join(", ", data));

                // 添加数据到现有的 WpfPlot 控件
                plot.Plot.Add.Scatter(xData, data);
                plot.Plot.XLabel("Index");
                plot.Plot.YLabel("Value");


                // 刷新 WpfPlot 控件以显示更新后的图表
                plot.Refresh();
            }
        }
    }
}