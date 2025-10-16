using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SteamEcho.App.Services;
using SteamEcho.App.ViewModels;
using SteamEcho.App.Views;

namespace SteamEcho.App;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Ensure single instance
        const string mutexName = @"Global\SteamEcho_SingleInstance";;
        _singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            TryBringExistingToFront();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Setup DI
        var serviceCollection = new ServiceCollection();
        serviceCollection.ConfigureServices();
        ServiceProvider = serviceCollection.BuildServiceProvider();

        // Show splash window
        var splashWindow = ServiceProvider.GetRequiredService<SplashWindow>();
        splashWindow.Show();

        // Create main window
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

        // Asynchronously initialize the ViewModel
        await mainWindow.InitializeViewModelAsync();

        // Set it as the main window
        MainWindow = mainWindow;

        // Show the main window and close the splash screen
        mainWindow.Show();
        splashWindow.Close();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static void TryBringExistingToFront()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id == currentProcess.Id) continue;
                var hWnd = process.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                    break;
                }
            }
        }
        catch { }
    }
    
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_RESTORE = 9;
}