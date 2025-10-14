using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface IProxyService
{
    /// <summary>
    /// Checks if a game's proxy dll is correctly configured and set its IsProxyReady property to the result.
    /// </summary>
    public bool CheckProxyStatus(Game game);

    /// <summary>
    /// Toggles the proxy state for a game by renaming the relevant DLL files.
    /// </summary>
    public void ToggleProxy(Game game);

}