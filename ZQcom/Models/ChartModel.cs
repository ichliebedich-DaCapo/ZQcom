using LiveCharts;
using LiveCharts.Wpf;
using System;

namespace ZQcom.Models
{
    public class ChartModel
    {
        // -----------------------------数据绑定------------------------------
        public SeriesCollection Series { get; set; }
        //public string[] Labels { get; set; }
        //public Func<double, string> YFormatter { get; set; }
        //public Axis XAxisLabelFormatter { get; set; } // 新增 X 轴属性


        public ChartModel()
        {
            Series = new SeriesCollection();
            //Labels = new string[0];
            //YFormatter = value => value.ToString("N");
            //// 设置 X 轴的 LabelFormatter
            //XAxisLabelFormatter = new Axis
            //{
            //    LabelFormatter = value => (value + 1).ToString() // 这里 +1 就是让显示的索引从 1 开始
            //};
            // 初始化第一个系列
            AddSeries("Data");
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
            //if (Series.Count > 0)
            //{
            // 先不做判断了，因为会有初始序列
                Series[0].Values.Add(value);
            //}
            //else
            //{
            //    throw new InvalidOperationException("没有可用的系列");
            //}
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
            Series.Clear();
        }
    }
}