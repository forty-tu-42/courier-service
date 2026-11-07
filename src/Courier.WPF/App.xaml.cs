using System.Windows;
using System.Windows.Threading;
using Courier.WPF.Helpers;

namespace Courier.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var splash = new SplashWindow();
        splash.Show();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            splash.Close();
            ShowLogin();
        };
        timer.Start();
    }

    private void ShowLogin()
    {
        var login = new LoginWindow();
        if (login.ShowDialog() == true && login.Session is not null)
        {
            AppSession.CurrentUser = login.Session;
            var main = new MainWindow();
            MainWindow = main;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }
        else
        {
            Shutdown();
        }
    }
}
