using System.Windows;
using SteamEcho.App.Views;

namespace SteamEcho.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Show splash window
        var splashWindow = new SplashWindow();
        splashWindow.Show();

        // Create main window
        var mainWindow = new MainWindow();
        
        // Asynchronously initialize the ViewModel
        await mainWindow.InitializeViewModelAsync();

        // Set it as the main window
        MainWindow = mainWindow;

        // Show the main window and close the splash screen
        mainWindow.Show();
        splashWindow.Close();
    }
}