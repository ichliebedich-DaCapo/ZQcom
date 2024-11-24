using System.Windows;

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
    }
}