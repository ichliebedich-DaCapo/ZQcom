using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using ScottPlot;

namespace ZQcom.Views
{
    public partial class ImageWindow : Window
    {

        /// <summary>
        ///  柱状图
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="title"></param>
        public ImageWindow(float[] inputData,string title)
        {
            InitializeComponent();

            List<double> xData = Enumerable.Range(0, inputData.Length).Select(i => (double)i).ToList();

            ImagePlot.Plot.Add.Bars(xData, inputData);

            ImagePlot.Plot.Title(title);
            ImagePlot.Plot.XLabel("Frequency Index");
            ImagePlot.Plot.YLabel("Magnitude");

        }
        /// <summary>
        /// 折线图
        /// </summary>
        /// <param name="inputData"></param>
        public ImageWindow(List<double> inputData, string title)
        {
            InitializeComponent();

            List<double> xData = Enumerable.Range(0, inputData.Count).Select(i => (double)i).ToList();

            ImagePlot.Plot.Add.Scatter(xData, inputData);

            ImagePlot.Plot.Title(title);
            ImagePlot.Plot.XLabel("Frequency Index");
            ImagePlot.Plot.YLabel("Magnitude");
           
        }
    }
}



