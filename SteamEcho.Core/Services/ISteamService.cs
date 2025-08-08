using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface ISteamService
{
    /// <summary>
    /// Searches for Steam games by name.
    /// </summary>
    public Task<List<GameInfo>> SearchSteamGamesAsync(string gameName);

    /// <summary>
    /// Fetches achievements for a game by its ID.<br/>
    /// If user is provided, fetches achievements for that user. Otherwise, fetches global achievements.
    /// </summary>
    public Task<List<Achievement>> GetAchievementsAsync(long gameId, SteamUserInfo? user);

    /// <summary>
    /// Logs in to Steam using OpenID authentication.
    /// Returns a SteamUserInfo object if successful, or null if authentication fails.
    /// </summary>
    public Task<SteamUserInfo?> LogToSteamAsync();

    /// <summary>
    /// Fetches owned games for a user.
    /// </summary>
    public Task<List<Game>> GetOwnedGamesAsync(SteamUserInfo user);
}
