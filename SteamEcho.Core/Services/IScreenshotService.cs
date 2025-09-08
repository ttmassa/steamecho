using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface IScreenshotService
{
    void StartMonitoring(SteamUserInfo? user, Game game);
    void StopMonitoring();
    event Action<Screenshot> ScreenshotTaken;
}