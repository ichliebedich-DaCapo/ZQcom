using System.Linq;
using System.Windows;
using ScottPlot;
using ZQcom.ViewModels;
using ScottPlot.WPF;

namespace ZQcom.Views
{
    public partial class DataDisplayChartWindow : Window
    {
        private List<double> _dataValues;
        private List<double> _signIndex;
        public DataDisplayChartWindow(List<double> dataValues, List<double> signIndex)
        {
            _dataValues = dataValues;
            _signIndex = signIndex;

            InitializeComponent();
            Loaded += Window_Loaded;

        }

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
            plot.Plot.Add.Scatter(xData, _dataValues);
            plot.Plot.XLabel("Index");
            plot.Plot.YLabel("Value");

            // 添加标记
            foreach (double index in _signIndex)
            {
                plot.Plot.Add.VerticalLine(index);
            }

            plot.Plot.Axes.AutoScale();
            // 刷新 WpfPlot 控件以显示更新后的图表
            plot.Refresh();
        }
    }
}