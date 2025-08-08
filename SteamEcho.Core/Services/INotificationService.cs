using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface INotificationService
{
    /// <summary>
    /// Shows an achievement notification.
    /// </summary>
    public void ShowNotification(Achievement achievement);
}