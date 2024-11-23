using System;
using System.Collections.ObjectModel;
using ZQcom.Models;

namespace ZQcom.ViewModels
{
    public class ChartViewModel : BaseViewModel
    {
        private ChartModel _chartModel;

        public ChartModel ChartModel
        {
            get => _chartModel;
            set
            {
                _chartModel = value;
                OnPropertyChanged(nameof(ChartModel));
            }
        }

        public ChartViewModel()
        {
            ChartModel = new ChartModel();
        }

        public void AddDataPoint(double value)
        {
            ChartModel.Series[0].Values.Add(value);
        }
    }
}