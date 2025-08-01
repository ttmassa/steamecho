using SteamEcho.App.Views;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class NotificationService : INotificationService
{
    /// <summary>
    /// Shows an achievement notification.
    /// </summary>
    public async void ShowNotification(Achievement achievement)
    {
        var notificationWindow = new AchievementNotificationWindow
        {
            DataContext = achievement
        };
        notificationWindow.Show();

        // Automatically close the notification after 7 seconds
        await Task.Delay(TimeSpan.FromSeconds(7));
        notificationWindow.Close();
    }
}
