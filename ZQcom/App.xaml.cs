using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ZQcom.ViewModels;
using ZQcom.Views;

namespace ZQcom
{
    public partial class App : Application
    {
        private MainViewModel _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化主视图模型
            _mainViewModel = new MainViewModel();

            // 设置主窗口
            MainWindow = new MainWindow
            {
                DataContext = _mainViewModel
            };
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}