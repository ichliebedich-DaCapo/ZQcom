using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ScottPlot;

namespace ZQcom.Views
{
    public partial class FFTWindow : Window
    {
        public FFTWindow(float[] fftResult)
        {
            InitializeComponent();

            List<double> xData = Enumerable.Range(0, fftResult.Length).Select(i => (double)i).ToList();
            FFTPlot.Plot.Add.Bars(xData, fftResult);
            FFTPlot.Plot.Title("FFT Result");
            FFTPlot.Plot.XLabel("Frequency Index");
            FFTPlot.Plot.YLabel("Magnitude");

        }
    }
}



