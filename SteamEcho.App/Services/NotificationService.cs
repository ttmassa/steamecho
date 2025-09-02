using System.IO;
using System.Media;
using System.Text.Json;
using System.Windows.Media;
using SteamEcho.App.Models;
using SteamEcho.App.Views;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class NotificationService : INotificationService
{
    private readonly SoundPlayer _soundPlayer;
    private readonly string _configPath;
    public NotificationConfig Config { get; private set; }

    public NotificationService()
    {
        _soundPlayer = new SoundPlayer("Assets/Sound/notification.wav");
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notification_configs.json");
        Config = LoadConfig();
    }

    public async void ShowNotification(Achievement achievement)
    {
        // Play notification sound
        _soundPlayer.Play();

        // Create and show the notification
        var notificationWindow = new AchievementNotification(Config.NotificationSize)
        {
            DataContext = achievement
        };

        // Apply configured color as a Brush on the Window.Tag so XAML can bind to it
        try
        {
            var col = (Color)ColorConverter.ConvertFromString(Config.NotificationColor);
            notificationWindow.Tag = new SolidColorBrush(col);
        }
        catch
        {
            // fallback: leave Tag null or default; don't crash notification
        }
        notificationWindow.Show();

        // Automatically close the notification after 7 seconds
        await Task.Delay(TimeSpan.FromSeconds(Config.NotificationTime));
        notificationWindow.Close();
    }

    public async void TestNotification(double? size, string? color)
    {
        // Play notification sound
        _soundPlayer.Play();
        // Create dummy achievement
        var testAchievement = new Achievement("test_achievement", "Awesome", "Awesome achievement description");

        // Create and show the notification
        var notificationWindow = new AchievementNotification(size ?? Config.NotificationSize)
        {
            DataContext = testAchievement
        };

        // Apply specified color or configured color as a Brush on the Window.Tag so XAML can bind to it
        try
        {
            var col = (Color)ColorConverter.ConvertFromString(color ?? Config.NotificationColor);
            notificationWindow.Tag = new SolidColorBrush(col);
        }
        catch
        {
            // fallback: leave Tag null or default; don't crash notification
        }
        notificationWindow.Show();

        // Automatically close the notification after 7 seconds
        await Task.Delay(TimeSpan.FromSeconds(Config.NotificationTime));
        notificationWindow.Close();
    }

    private NotificationConfig LoadConfig()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<NotificationConfig>(json) ?? new NotificationConfig();
            }
            catch
            {
                // If deserialization fails, return a new config with default values
                return new NotificationConfig();
            }
        }
        return new NotificationConfig();
    }

    public void SaveConfig()
    {
        var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
