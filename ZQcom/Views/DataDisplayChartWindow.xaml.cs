using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ScottPlot;
using ZQcom.ViewModels;
using ScottPlot.WPF;
using MathNet.Numerics.IntegralTransforms;
using System.Windows.Media.Imaging;

namespace ZQcom.Views
{
    public partial class DataDisplayChartWindow : Window
    {
        private List<double> _dataValues;
        private List<double> _signIndex;
        private bool _isFFTDisplayed = false;
        private ScottPlot.Plottables.BarPlot _fftSeries;

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

        private void FFTButton_Click(object sender, RoutedEventArgs e)
        {
            int startIndex = int.Parse(StartIndexInput.Text);
            int length = int.Parse(LengthInput.Text);

            if (startIndex == -1 && length == -1)
            {
                startIndex = 0;
                length = _dataValues.Count;
            }
            else if (length > _dataValues.Count - startIndex || startIndex >= _dataValues.Count)
            {
                MessageBox.Show("无效的起始位置或长度。");
                return;
            }

            var fftData = _dataValues.Skip(startIndex).Take(length).ToArray();
            var fftResult = ComputeFFT(fftData);

            if (OpenNewWindowCheckbox.IsChecked.Value)
            {
                DisplayFFTInNewWindow(fftResult);
            }
            else
            {
                // Toggle display of FFT result
                if (_isFFTDisplayed)
                {
                    RemoveFFTPlot();
                }
                else
                {
                    DisplayFFT(fftResult);
                }
                _isFFTDisplayed = !_isFFTDisplayed;
            }
        }

        private float[] ComputeFFT(double[] data)
        {
            var complexData = new MathNet.Numerics.Complex32[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                complexData[i] = new MathNet.Numerics.Complex32((float)data[i], 0);
            }

            Fourier.Forward(complexData, FourierOptions.Matlab);

            var magnitudes = complexData.Select(c => c.Magnitude).ToArray();

            return magnitudes;
        }

        private void DisplayFFT(float[] fftResult)
        {
            List<double> xData = Enumerable.Range(0, fftResult.Length).Select(i => (double)i).ToList();
            _fftSeries = plot.Plot.Add.Bars(xData, fftResult);
            plot.Refresh();
        }

        private void RemoveFFTPlot()
        {
            if (_fftSeries != null)
            {
                plot.Plot.Remove(_fftSeries);
                _fftSeries = null;
            }
            plot.Refresh();
        }

        private void SmoothButton_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for smooth button functionality
            MessageBox.Show("Smoothing not yet implemented.");
        }

        private void RemoveOutliersButton_Click(object sender, RoutedEventArgs e)
        {
            double threshold = double.Parse(ThresholdInput.Text);
            _dataValues = _dataValues.Where(value => Math.Abs(value) <= threshold).ToList();

            // Regenerate X-axis data
            List<double> xData = Enumerable.Range(0, _dataValues.Count).Select(i => (double)i).ToList();

            // Clear existing plots
            plot.Plot.Clear();

            // Add updated data to the plot
            plot.Plot.Add.Scatter(xData, _dataValues);
            plot.Plot.XLabel("Index");
            plot.Plot.YLabel("Value");

            // Add markers
            foreach (double index in _signIndex)
            {
                if (index < _dataValues.Count)
                {
                    plot.Plot.Add.VerticalLine(index);
                }
            }

            plot.Plot.Axes.AutoScale();
            plot.Refresh();
        }

        private void DisplayFFTInNewWindow(float[] fftResult)
        {
            var fftWindow = new FFTWindow(fftResult);
            fftWindow.Show();
        }
    }
}



