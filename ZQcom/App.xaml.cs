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
    
        public static ResourceDictionary ?DefaultTheme { get; private set; }
        public static ResourceDictionary ?PurpleTheme { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化主视图模型

            // 设置主窗口
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }

        public App()
        {
            //DefaultTheme = new ResourceDictionary { Source = new Uri("Resources//Styles/DefaultStyle.xaml", UriKind.Relative) };
            //PurpleTheme = new ResourceDictionary { Source = new Uri("Resources/Styles/PurpleStyle.xaml", UriKind.Relative) };

            ////默认加载紫色主题
            //Current.Resources.MergedDictionaries.Add(PurpleTheme);
        }

        public static void SwitchTheme(string themeName)
        {
            Current.Resources.MergedDictionaries.Clear();
            if (themeName == "Default")
            {
                Current.Resources.MergedDictionaries.Add(DefaultTheme);
            }
            else if (themeName == "Purple")
            {
                Current.Resources.MergedDictionaries.Add(PurpleTheme);
            }
        }
}
}