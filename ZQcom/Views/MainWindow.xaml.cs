using ICSharpCode.AvalonEdit;
using Newtonsoft.Json;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ZQcom.Models;
using ZQcom.ViewModels;

namespace ZQcom.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;
        // 构建配置文件路径
        private readonly string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "AppSettings.json");
        public MainWindow()
        {
            InitializeComponent();

            // 确保目录存在
            string ?directoryPath = Path.GetDirectoryName(settingsFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 读取配置文件
            AppSettings appSettings;
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    var json = File.ReadAllText(settingsFilePath);
                    appSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    appSettings = new AppSettings(); // 使用默认配置
                }
            }
            catch (Exception ex)
            {
                // 记录异常或适当处理
                MessageBox.Show($"加载设置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                appSettings = new AppSettings(); // 使用默认配置
            }

            // 创建视图模型
            _mainViewModel = new MainViewModel(appSettings);


            // ----------传入组件----------
            _mainViewModel.SerialPortViewModel.LogText = textLogEditor;
            _mainViewModel.SerialPortViewModel.ExtractedText = textExtractedEditor;
            _mainViewModel.SerialPortViewModel.ConvertedText = textConvertedEditor;

            // 设置数据上下文
            DataContext = _mainViewModel;
  
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 保存设置
            var appSettings = new AppSettings
            {
                SerialPort = _mainViewModel.SerialPortViewModel.GetSettings(),
                Chart = _mainViewModel.ChartViewModel.GetSettings()
            };

            var json = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
            File.WriteAllText(settingsFilePath, json);
        }


        // 最小化窗口
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 最大化窗口
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        // 关闭窗口
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        
        }

        // 窗口标题栏拖动
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }


        // 接收数据框更新
        private void TextLogEditor_TextChanged(object sender, EventArgs e)
        {
            textLogEditor.ScrollToEnd();
        }

        private void TextExtractedEditor_TextChanged(object sender, EventArgs e)
        {
            textExtractedEditor.ScrollToEnd();
        }

        private void TextConvertedEditor_TextChanged(object sender, EventArgs e)
        {
            textConvertedEditor.ScrollToEnd();
        }
    }
}