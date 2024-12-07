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
        // 选择串口
        public string SelectedSerialPort { get; set; } = "";
        // 选择波特率
        public int SelectedBaudRate { get; set; } = 9600;
        // 选择奇偶校验
        public Parity SelectedParity { get; set; } = Parity.None;
        // 选择停止位
        public StopBits SelectedStopBits { get; set; } = StopBits.One;
        // 选择数据位
        public int SelectedDataBits { get; set; } = 8;
        // 发送数据文本
        public string SendDataText { get; set; } = "01040000000271CB";
        // 是否启用16进制发送
        public bool IsHexSend {  get; set; }=false;
        // 十六进制显示
        public bool IsHexDisplay { get; set;}=false;
        // 添加回车
        public bool AddNewline { get; set; } = false;
        // 定时发送间隔
        public int TimedSendInterval { get; set; } = 100;
        // 是否处理数据
        public bool IsExtractedData { get; set; } = false;
        // 是否转换数据
        public bool IsConvertedData { get; set; } = false;
        // 截取数据的起始位置
        public int StartPosition { get; set; } = 7;
        // 截取数据的长度
        public int Length { get; set; } = -1;
        // 是否禁用时间戳
        public bool IsDisableTimestamp { get; set; } = false;
        // 启用图表
        public bool IsEnableChart { get; set; } = false;
        // 是否启用高频接收模式
        public bool IsHighFrequencyReceiving { get; set; } = false;
    }
}
