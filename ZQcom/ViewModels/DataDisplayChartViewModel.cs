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
        public List<double> _dataValues;
        public List<double> _signIndex;

        // 
        public  ScottPlot.WPF.WpfPlot? DataChartPlot;

        public DataDisplayChartViewModel(List<double> dataValues, List<double> signIndex, DataDisplayChartSettings settings)
        {
            _dataValues = dataValues;
            _signIndex = signIndex;

            SetSettings(settings);
        }


        // ----------------------------内部方法----------------------------
        // 绘制最初的图形
        public void InitChart()
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
            DataChartPlot?.Plot.Add.Scatter(xData, _dataValues);
            DataChartPlot?.Plot.XLabel("Index");
            DataChartPlot?.Plot.YLabel("Value");

            // 添加标记
            foreach (double index in _signIndex)
            {
                DataChartPlot?.Plot.Add.VerticalLine(index);
            }

            DataChartPlot?.Plot.Axes.AutoScale();
            // 刷新 WpfPlot 控件以显示更新后的图表
            DataChartPlot?.Refresh();
        }

        // 获取配置
        public DataDisplayChartSettings GetSettings()
        {
            return new DataDisplayChartSettings
            {
                IsEnableNewFFTWindow = IsEnableNewFFTWindow,
                FFTStartIndexInput = FFTStartIndexInput,
                FFTLengthInput = FFTLengthInput,
                ThresholdInput = ThresholdInput
            };
        }
        // 设置串口参数
        public void SetSettings(DataDisplayChartSettings settings)
        {
            IsEnableNewFFTWindow = settings.IsEnableNewFFTWindow;
            FFTStartIndexInput = settings.FFTStartIndexInput;
            FFTLengthInput = settings.FFTLengthInput;
            ThresholdInput = settings.ThresholdInput;
        }


        // ----------------------------事件绑定----------------------------
        public ICommand FFTCommand => new RelayCommand(FFT);
        private void FFT()
        {
            int startIndex =FFTStartIndexInput;
            int length = FFTLengthInput;

            if (length == -1)
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
            _fftSeries = DataChartPlot?.Plot.Add.Bars(xData, fftResult);
            DataChartPlot?.Refresh();
        }

        private void RemoveFFTPlot()
        {
            if (_fftSeries != null)
            {
                DataChartPlot?.Plot.Remove(_fftSeries);
                _fftSeries = null;
            }
            DataChartPlot?.Refresh();
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
            DataChartPlot?.Plot.Clear();

            // Add updated data to the _dataChartPlot?
            DataChartPlot?.Plot.Add.Scatter(xData, _dataValues);
            DataChartPlot?.Plot.XLabel("Index");
            DataChartPlot?.Plot.YLabel("Value");

            // Add markers
            foreach (double index in _signIndex)
            {
                if (index < _dataValues.Count)
                {
                    DataChartPlot?.Plot.Add.VerticalLine(index);
                }
            }

            DataChartPlot?.Plot.Axes.AutoScale();
            DataChartPlot?.Refresh();
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
        private int _FFTStartIndexInput;
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
        private int _FFTLengthInput;
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
        private int _thresholdInput;
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
