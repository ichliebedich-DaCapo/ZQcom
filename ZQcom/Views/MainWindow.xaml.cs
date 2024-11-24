using System.Windows;
using System.Windows.Input;

namespace ZQcom.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // 由于已经在App.xaml.cs中设置了DataContext，所以这里要注释掉
            //DataContext = new ViewModels.MainViewModel();
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
    }
}