using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MathNet.Numerics.IntegralTransforms;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ZQcom.Models;
using ZQcom.Views;

namespace ZQcom.ViewModels
{
    public class DataDisplayChartViewModel :ViewModelBase
    {
        private bool _isFFTDisplayed = false;
        private ScottPlot.Plottables.BarPlot? _fftSeries;
        private List<double> _dataValues;
        private readonly List<double> _signIndex;

        public  ScottPlot.WPF.WpfPlot? _dataChartPlot;

        public DataDisplayChartViewModel(List<double> dataValues, List<double> signIndex,DataDisplayChartSettings settings) 
        { 
        _dataValues = dataValues;
            _signIndex = signIndex;
        
        }


        // ----------------------------内部方法----------------------------
        // 获取配置
        public DataDisplayChartSettings GetSettings()
        {
            return new DataDisplayChartSettings
            {

            };
        }
        // 设置串口参数
        public void SetSettings(DataDisplayChartSettings settings)
        {


        }


        // ----------------------------事件绑定----------------------------
        public ICommand FFTCommand => new RelayCommand(FFT);
        private void FFT()
        {
            int startIndex =FFTStartIndexInput;
            int length = FFTLengthInput;

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

            if (IsEnableNewFFTWindow)
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
            _fftSeries = _dataChartPlot?.Plot.Add.Bars(xData, fftResult);
            _dataChartPlot?.Refresh();
        }

        private void RemoveFFTPlot()
        {
            if (_fftSeries != null)
            {
                _dataChartPlot?.Plot.Remove(_fftSeries);
                _fftSeries = null;
            }
            _dataChartPlot?.Refresh();
        }

        private void SmoothButton_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for smooth button functionality
            MessageBox.Show("Smoothing not yet implemented.");
        }

        private void RemoveOutliersButton_Click(object sender, RoutedEventArgs e)
        {
            double threshold = ThresholdInput;
            _dataValues = _dataValues.Where(value => Math.Abs(value) <= threshold).ToList();

            // Regenerate X-axis data
            List<double> xData = Enumerable.Range(0, _dataValues.Count).Select(i => (double)i).ToList();

            // Clear existing plots
            _dataChartPlot?.Plot.Clear();

            // Add updated data to the _dataChartPlot?
            _dataChartPlot?.Plot.Add.Scatter(xData, _dataValues);
            _dataChartPlot?.Plot.XLabel("Index");
            _dataChartPlot?.Plot.YLabel("Value");

            // Add markers
            foreach (double index in _signIndex)
            {
                if (index < _dataValues.Count)
                {
                    _dataChartPlot?.Plot.Add.VerticalLine(index);
                }
            }

            _dataChartPlot?.Plot.Axes.AutoScale();
            _dataChartPlot?.Refresh();
        }

        private void DisplayFFTInNewWindow(float[] fftResult)
        {
            var fftWindow = new FFTWindow(fftResult);
            fftWindow.Show();
        }

        // ---------------------------数据绑定--------------------------------
        // 是否启用新窗口
        private bool _isEnableNewFFTWindow;
        public bool IsEnableNewFFTWindow
        {
            get => _isEnableNewFFTWindow;
            set
            {
                _isEnableNewFFTWindow = value;
                RaisePropertyChanged(nameof(IsEnableNewFFTWindow)); // 触发属性更改通知
            }
        }

        // FFT起始位置
        private int _FFTStartIndexInput=-1;
        public int FFTStartIndexInput
        {
            get => _FFTStartIndexInput;
            set
            {
                _FFTStartIndexInput = value;
                RaisePropertyChanged(nameof(FFTStartIndexInput)); // 触发属性更改通知
            }
        }

        // FFT长度
        private int _FFTLengthInput=-1;
        public int FFTLengthInput
        {
            get => _FFTLengthInput;
            set
            {
                _FFTLengthInput = value;
                RaisePropertyChanged(nameof(FFTLengthInput)); // 触发属性更改通知
            }
        }

        // 阈值
        private int _thresholdInput=100;
        public int ThresholdInput
        {
            get => _thresholdInput;
            set
            {
                _thresholdInput = value;
                RaisePropertyChanged(nameof(ThresholdInput)); // 触发属性更改通知
            }
        }



    }
}
