using SteamEcho.App.Models;
using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface INotificationService
{
    public NotificationConfig Config { get; }

    /// <summary>
    /// Shows an achievement notification.
    /// </summary>
    public void ShowNotification(Achievement achievement);

    /// <summary>
    /// Sends a test notification to verify functionality.
    /// </summary>
    public void TestNotification(double? size, string? color);

    /// <summary>
    /// Saves the current notification configuration to file.
    /// </summary>
    public void SaveConfig();
}