using LiveCharts;
using LiveCharts.Wpf;

namespace ZQcom.Models
{
    public class ChartModel
    {
        public SeriesCollection Series { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public ChartModel()
        {
            Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Data",
                    Values = new ChartValues<double>()
                }
            };

            Labels = new string[0];
            YFormatter = value => value.ToString("N");
        }
    }
}