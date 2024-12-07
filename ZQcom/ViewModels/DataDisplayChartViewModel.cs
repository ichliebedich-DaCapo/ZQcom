using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MathNet.Numerics.IntegralTransforms;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
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
        private bool _isSmoothDataDisplayed = false;
        private ScottPlot.Plottables.BarPlot? _fftSeries;
        private ScottPlot.Plottables.Scatter? _smoothSeries;
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
                IsEnableNewWindow = IsEnableNewWindow,
                FFTStartIndexInput = FFTStartIndexInput,
                FFTLengthInput = FFTLengthInput,
                ThresholdInput = ThresholdInput,
                StepSize = StepSize,
                WindowWidth = WindowWidth
            };
        }
        // 设置串口参数
        public void SetSettings(DataDisplayChartSettings settings)
        {
            IsEnableNewWindow = settings.IsEnableNewWindow;
            FFTStartIndexInput = settings.FFTStartIndexInput;
            FFTLengthInput = settings.FFTLengthInput;
            ThresholdInput = settings.ThresholdInput;
            StepSize = settings.StepSize;
            WindowWidth = settings.WindowWidth;
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

        private static void DisplayFFTInNewWindow(float[] fftResult)
        {
            var fftWindow = new ImageWindow(fftResult, "FFT Result");
            fftWindow.Show();
        }


        // 平滑数据处理
        private void DisplaySmoothData(List<double> smoothedData)
        {
            List<double> xData = Enumerable.Range(0, smoothedData.Count).Select(i => (double)i).ToList();
            _smoothSeries = DataChartPlot?.Plot.Add.Scatter(xData, smoothedData);
            DataChartPlot?.Refresh();
        }

        private void RemoveSmoothDataPlot()
        {
            if (_smoothSeries != null)
            {
                DataChartPlot?.Plot.Remove(_smoothSeries);
                _smoothSeries = null;
            }
            DataChartPlot?.Refresh();
        }

        private static void DisplaySmoothDataInNewWindow(List<double> smoothedData)
        {
            // 创建一个新窗口，并传递转换后的 float数组
            var smoothDataWindow = new ImageWindow(smoothedData, "SmoothData Result");
            smoothDataWindow.Show();
        }

        // ----------------------------事件绑定----------------------------
        public ICommand FFTCommand => new RelayCommand(FFT);
        public ICommand SmoothDataCommand => new RelayCommand(SmoothData);
        public ICommand DeleteAbnormalDataCommand => new RelayCommand(DeleteAbnormalData);
        public ICommand SaveImageCommand => new RelayCommand(SaveImage);

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

            if (IsEnableNewWindow)
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



        public void SmoothData()
        {
            if (_dataValues == null || _dataValues.Count == 0 || WindowWidth <= 0 || StepSize <= 0)
            {
                MessageBox.Show("没有有效的数据");
                return; // 确保在这种情况下函数提前返回
            }

            List<double> smoothedData = []; // 使用List<double>而非数组

            for (int i = 0; i <= _dataValues.Count - WindowWidth; i += StepSize)
            {
                double sum = 0;
                for (int j = 0; j < WindowWidth; j++)
                {
                    sum += _dataValues[i + j];
                }
                double average = sum / WindowWidth;
                smoothedData.Add(average); // 正确地使用.Add()方法
            }


            if (IsEnableNewWindow)
            {
                DisplaySmoothDataInNewWindow(smoothedData);
            }
            else
            {
                // Toggle display of FFT result
                if (_isSmoothDataDisplayed)
                {
                    RemoveSmoothDataPlot();
                }
                else
                {
                    DisplaySmoothData(smoothedData);
                }
                _isSmoothDataDisplayed = !_isSmoothDataDisplayed;
            }

        }

        private void DeleteAbnormalData()
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

        private void SaveImage()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // 定义并创建 Picture 文件夹
            string pictureFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Picture");
            Directory.CreateDirectory(pictureFolderPath);  // 如果目录不存在，则创建

            // 组合图片的完整路径
            string imagePath = Path.Combine(pictureFolderPath, $"{timestamp}.png");

            // 保存图片到指定路径
            DataChartPlot?.Plot.SavePng(imagePath, 400, 300);
            MessageBox.Show("保存图片成功");
        }

        // ---------------------------数据绑定--------------------------------
        // 是否启用新窗口
        private bool _isEnableNewWindow;
        public bool IsEnableNewWindow
        {
            get => _isEnableNewWindow;
            set
            {
                _isEnableNewWindow = value;
                RaisePropertyChanged(nameof(IsEnableNewWindow)); // 触发属性更改通知
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
        private float _thresholdInput;
        public float ThresholdInput
        {
            get => _thresholdInput;
            set
            {
                _thresholdInput = value;
                RaisePropertyChanged(nameof(ThresholdInput)); // 触发属性更改通知
            }
        }

        // 步长
        private int _stepSize;
        public int StepSize
        {
            get => _stepSize;
            set
            {
                _stepSize = value;
                RaisePropertyChanged(nameof(StepSize));
            }
        }

        // 窗口长度
        private int _windowWidth;
        public int WindowWidth
        {
            get => _windowWidth;
            set
            {
                _windowWidth = value;
                RaisePropertyChanged(nameof(WindowWidth));
            }
        }


    }
}
