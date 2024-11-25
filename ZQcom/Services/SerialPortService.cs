using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace ZQcom.Services
{
    public class SerialPortService
    {
        public event EventHandler<SerialDataReceivedEventArgs> DataReceived;

        public List<string> GetAvailablePorts()
        {
            return SerialPort.GetPortNames().ToList();
        }

        public SerialPort OpenPort(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits)
        {
            var port = new SerialPort(portName, baudRate)
            {
                Parity = parity,
                StopBits = stopBits,
                DataBits = dataBits
            };
            port.DataReceived += OnDataReceived;
            port.Open();
            return port;
        }

        public void ClosePort(SerialPort port)
        {
            if (port != null && port.IsOpen)
            {
                port.DataReceived -= OnDataReceived;
                port.Close();
            }
        }

        public void SendData(SerialPort port, string data)
        {
            if (port != null && port.IsOpen)
            {
                port.WriteLine(data);
            }
        }

        public void SendData(SerialPort port, byte[] data)
        {
            if (port != null && port.IsOpen)
            {
                port.Write(data, 0, data.Length);
            }
        }

        protected virtual void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            DataReceived?.Invoke(sender, e);
        }


    }
}