using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;

namespace ZQcom.Models
{
    public class ChartModel
    {
        public SeriesCollection Series { get; set; }
        public List<string> Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public ChartModel()
        {
            Series = new SeriesCollection();
            Labels = new List<string>();
            YFormatter = value => value.ToString("N");

            // 初始化第一个系列
            AddSeries("Data");
        }

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
                Labels.Add((Labels.Count + 1).ToString());
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
                if (index == 0)
                {
                    Labels.Add((Labels.Count + 1).ToString());
                }
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
                if (index < Labels.Count)
                {
                    Labels.RemoveAt(index);
                }
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
                    if (series_index == 0 && index < Labels.Count)
                    {
                        Labels.RemoveAt(index);
                    }
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
    }
}