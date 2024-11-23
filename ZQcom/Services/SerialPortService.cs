using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Threading.Tasks;
using ZQcom.Models;

namespace ZQcom.Services
{
    public class SerialService
    {
        private SerialPort _serialPort;

        public event EventHandler<string> DataReceived;

        public async Task OpenAsync(SerialPortModel model)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort = new SerialPort
            {
                PortName = model.PortName,
                BaudRate = model.BaudRate,
                Parity = model.Parity,
                DataBits = model.DataBits,
                StopBits = model.StopBits
            };

            _serialPort.DataReceived += (s, e) =>
            {
                var data = _serialPort.ReadExisting();
                DataReceived?.Invoke(this, data);
            };

            await Task.Run(() => _serialPort.Open());
        }

        public void Close()
        {
            _serialPort?.Close();
        }

        public void Write(string data)
        {
            _serialPort?.Write(data);
        }
    }
}