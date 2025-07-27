using System.Windows;
using Steamworks;

namespace SteamEcho.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!SteamAPI.Init())
        {
            MessageBox.Show("Steam must be running to use this app.", "Steam Not Running", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SteamAPI.Shutdown();
        base.OnExit(e);
    }
}

