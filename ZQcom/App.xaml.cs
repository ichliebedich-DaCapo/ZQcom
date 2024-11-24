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
        private CancellationTokenSource _cancellationTokenSource;
        private Task _processingTask;
        private MainViewModel _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化主视图模型
            _mainViewModel = new MainViewModel();
            _cancellationTokenSource = new CancellationTokenSource();

            // 启动数据处理任务
            //_processingTask = Task.Run(async () => await _mainViewModel._serialPortViewModel.ProcessDataQueueAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

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

            // 停止数据处理任务
            _cancellationTokenSource.Cancel();
            _processingTask.Wait();
        }
    }
}