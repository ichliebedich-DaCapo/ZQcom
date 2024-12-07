using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZQcom.Models
{
    public class DataDisplayChartSettings
    {
        // 是否启用新窗口
        public bool IsEnableNewFFTWindow { get; set; } = false;
        // 开始位置
        public int FFTStartIndexInput { get; set; } = 0;
        // 显示长度
        public int FFTLengthInput { get; set; } = -1;
        // 阈值
        public int ThresholdInput { get; set; } = 100;


    }
}
