using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZQcom.Models
{
    public class ChartSettings
    {
        public int MaxChartPoints { get; set; } = 100;
        public bool IsDisableAnimation { get; set; } = false;
    }
}
