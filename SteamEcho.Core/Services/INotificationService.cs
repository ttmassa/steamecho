using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface INotificationService
{
    public void ShowNotification(Achievement achievement);
}