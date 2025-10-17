using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SteamEcho.App.Services;
using SteamEcho.App.ViewModels;
using SteamEcho.App.Views;
using SteamEcho.Core.Services;

namespace SteamEcho.App;

public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        // Determine database path
        string dbPath;
        #if DEBUG
                dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steamecho.db");
        #else
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "SteamEcho");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            dbPath = Path.Combine(appDataPath, "steamecho.db");
        #endif
        
        // Register services as singletons
        services.AddSingleton<IAchievementListener, AchievementListener>();
        services.AddSingleton<IInternetService, InternetService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IProxyService, ProxyService>();
        services.AddSingleton<IScreenshotService, ScreenshotService>();
        services.AddSingleton<ISteamService, SteamService>();
        services.AddSingleton<IStorageService>(sp => new StorageService(dbPath));
        services.AddSingleton<IUINotificationService, UINotificationService>();
        services.AddSingleton<IGameProcessService, GameProcessService>();

        // Register ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<GameDetailsViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();
        services.AddTransient<SplashWindow>();

        return services;
    }
}