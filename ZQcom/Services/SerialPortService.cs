using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;

namespace ZQcom.Services
{
    public class SerialPortService
    {
        SerialDataReceivedEventHandler? _handler;

        public static List<string> GetAvailablePorts()
        {
            return [.. SerialPort.GetPortNames()];
        }

        public SerialPort OpenPort(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits)
        {
            var port = new SerialPort(portName, baudRate)
            {
                Parity = parity,
                StopBits = stopBits,
                DataBits = dataBits
            };
            return port;
        }

        /// <summary>
        /// 用于绑定接收数据函数和启动串口
        /// </summary>
        /// <param name="port"></param>
        /// <param name="handler"></param>
        public void Start(SerialPort? port, SerialDataReceivedEventHandler handler)
        {
            if (port != null)
            {
                _handler = handler;
                port.DataReceived += handler;
                port.Open();
            }
            else
            {
                MessageBox.Show("串口丢失");
            }
        }


        public void ClosePort(SerialPort port)
        {
            if (port != null && port.IsOpen)
            {
                port.DataReceived -= _handler;
                _handler = null;
                port.Close();
            }
        }

        // 要的就是速度
        public void SendData(SerialPort? port, string data)
        {
                port.WriteLine(data);
        }

        public void SendData(SerialPort? port, byte[] data)
        {
                port.Write(data, 0, data.Length);
        }

        
    }
}