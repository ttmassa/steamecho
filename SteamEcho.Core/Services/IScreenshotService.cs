using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface IScreenshotService
{
    /// <summary>
    /// Event fired when a screenshot is taken.
    /// </summary>
    event Action<Screenshot> ScreenshotTaken;

    /// <summary>
    /// Starts monitoring for screenshots taken in the specified game by the specified user.
    /// </summary>
    void StartMonitoring(SteamUserInfo? user, Game game);

    /// <summary>
    /// Stops monitoring for screenshots.
    /// </summary>
    void StopMonitoring();
  
}