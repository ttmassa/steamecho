using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface ISteamService
{
    public Task<GameInfo?> ResolveSteamIdAsync(string gameName);
    public Task<List<Achievement>> GetAchievementsAsync(string steamId);
}
