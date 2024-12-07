using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZQcom.Models
{
    public class SerialPortSettings
    {
        public string SelectedSerialPort { get; set; } = "";
        public int SelectedBaudRate { get; set; } = 9600;
        public Parity SelectedParity { get; set; } = Parity.None;
        public StopBits SelectedStopBits { get; set; } = StopBits.One;
        public int SelectedDataBits { get; set; } = 8;
    }
}
