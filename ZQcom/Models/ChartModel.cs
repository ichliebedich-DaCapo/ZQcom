using LiveCharts;
using LiveCharts.Wpf;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ZQcom.Models
{
    public class ChartModel
    {
        // -----------------------------数据绑定------------------------------
        // 使用序列是因为后续我想要添加更多序列
        public SeriesCollection Series { get; set; }

        public ChartModel()
        {
            Series = [];
            // 初始化第一个系列
            AddSeries("Data");
            AddSeries("FFT");
        }

        // -----------------------------对外方法(接口)------------------------------
        // 添加序列
        public void AddSeries(string title)
        {
            Series.Add(new LineSeries
            {
                Title = title,
                Values = new ChartValues<double>()
            });
        }

        // 移除序列
        public void RemoveSeries(int index)
        {
            if (index >= 0 && index < Series.Count)
            {
                Series.RemoveAt(index);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围");
            }
        }

        // 添加点
        public void AddDataPoint(double value)
        {
            if (Series.Count > 0)
            {
                Series[0].Values.Add(value);
            }
            else
            {
                throw new InvalidOperationException("没有可用的系列");
            }
        }

        public void AddDataPoint(int index, double value)
        {
            if (index >= 0 && index < Series.Count)
            {
                Series[index].Values.Add(value);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围");
            }
        }

        // 移除数据点,默认移除的是第一个系列
        public void RemoveDataPoint(int index)
        {
            if (index >= 0 && index < Series[0].Values.Count)
            {
                Series[0].Values.RemoveAt(index);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围");
            }
        }

        public void RemoveDataPoint(int series_index, int index)
        {
            if (series_index >= 0 && series_index < Series.Count)
            {
                if (index >= 0 && index < Series[series_index].Values.Count)
                {
                    Series[series_index].Values.RemoveAt(index);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(series_index), "系列索引超出范围");
            }
        }

        // 获取数据点的数量
        public int GetDataPointCount()
        {
            return Series[0].Values.Count;
        }

        public int GetDataPointCount(int series_index)
        {
            return Series[series_index].Values.Count;
        }

        public void Clear()
        {
            foreach (var series in Series)
            {
                series.Values.Clear();
            }
        }

        // ---------------------------------事件绑定-------------------------------
        public void FFT()
        {
            if (Series[0].Values.Count == 0)
            {
                MessageBox.Show("没有可用数据进行FFT变换。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (Series[1].Values.Count > 0)
            {
                // 如果 FFT 系列中有数据，清除 FFT 系列
                Series[1].Values.Clear();
            }
            else
            {
                // 获取 Data 系列的数据
                var data = Series[0].Values.Cast<double>().ToArray();

                // 创建复数数组
                var complexData = new MathNet.Numerics.Complex32[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    complexData[i] = new MathNet.Numerics.Complex32((float)data[i], 0);
                }

                // 执行 FFT
                Fourier.Forward(complexData, FourierOptions.Matlab);

                // 提取 FFT 结果的幅度
                var fftResults = complexData.Select(c => c.Magnitude).ToArray();

                // 清空 FFT 系列
                Series[1].Values.Clear();

                // 将 FFT 结果添加到 FFT 系列
                foreach (var result in fftResults)
                {
                    Series[1].Values.Add((double)result);
                }
            }
        }
    }
}