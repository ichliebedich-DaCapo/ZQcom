using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZQcom.Models
{
    public class AppSettings
    {
        public SerialPortSettings SerialPort { get; set; }
        public ChartSettings Chart { get; set; }

        public AppSettings()
        {
            SerialPort = new SerialPortSettings();
            Chart = new ChartSettings();
        }
    }

}
