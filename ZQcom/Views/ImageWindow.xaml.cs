using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using ScottPlot;

namespace ZQcom.Views
{
    
    public partial class ImageWindow : Window
    {
        // 图片类型
        private string image_type;
        /// <summary>
        ///  柱状图
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="title"></param>
        public ImageWindow(float[] inputData,string title)
        {
            InitializeComponent();

            List<double> xData = Enumerable.Range(0, inputData.Length).Select(i => (double)i).ToList();

            ImagePlot.Plot.Add.Bars(xData, inputData);

            ImagePlot.Plot.Title(title);
            ImagePlot.Plot.XLabel("Frequency Index");
            ImagePlot.Plot.YLabel("Magnitude");

            // 图片类型
            image_type = "Bars";
        }
        /// <summary>
        /// 折线图
        /// </summary>
        /// <param name="inputData"></param>
        public ImageWindow(List<double> inputData, string title)
        {
            InitializeComponent();

            List<double> xData = Enumerable.Range(0, inputData.Count).Select(i => (double)i).ToList();

            ImagePlot.Plot.Add.Scatter(xData, inputData);

            ImagePlot.Plot.Title(title);
            ImagePlot.Plot.XLabel("Index");
            ImagePlot.Plot.YLabel("Magnitude");

            // 图片类型
            image_type = "Scatter";
           
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // 定义并创建 Picture 文件夹
            string pictureFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Picture");
            Directory.CreateDirectory(pictureFolderPath);  // 如果目录不存在，则创建

            // 组合图片的完整路径
            string imagePath = Path.Combine(pictureFolderPath, $"{timestamp}_{image_type}.png");

            // 保存图片到指定路径
            ImagePlot.Plot.SavePng(imagePath, 400, 300);
            MessageBox.Show("保存图片成功");
        }
    }
}



