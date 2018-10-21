using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace ReplaysGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        TaskbarIcon icon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            icon = (TaskbarIcon)FindResource("TaskIcon");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            icon.Dispose();
            base.OnExit(e);
        }
    }
}
