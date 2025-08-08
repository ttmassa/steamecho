using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface ISteamService
{
    public Task<List<GameInfo>> SearchSteamGamesAsync(string gameName);
    public Task<List<Achievement>> GetAchievementsAsync(long gameId, SteamUserInfo? user);
}
