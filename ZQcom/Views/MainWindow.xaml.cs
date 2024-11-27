using ICSharpCode.AvalonEdit;
using System.Windows;
using System.Windows.Input;
using ZQcom.ViewModels;

namespace ZQcom.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _mainViewModel;
        public MainWindow()
        {
            InitializeComponent();
            // 由于已经在App.xaml.cs中设置了DataContext，所以这里要注释掉
            //DataContext = new ViewModels.MainViewModel();

            // 初始化主视图模型
            _mainViewModel = new MainViewModel();

            // ----------传入组件----------
            _mainViewModel.SerialPortViewModel.TextEditor = textEditor;

            // 设置数据上下文
            DataContext = _mainViewModel;
  
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
        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            textEditor.ScrollToEnd();
        }
    }
}