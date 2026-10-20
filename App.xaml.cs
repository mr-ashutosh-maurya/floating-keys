using System.Threading;
using System.Windows;

namespace FloatingKeys;

public partial class App : Application
{
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "FloatingKeys.SingleInstance", out bool created);
        if (!created) { Shutdown(); return; }   // already running (check the tray)
        base.OnStartup(e);
        new MainWindow().Show();
    }
}
