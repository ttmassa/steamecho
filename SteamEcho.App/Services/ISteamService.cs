using SteamEcho.Core.Models;

namespace SteamEcho.App.Services;

public interface ISteamService
{
    public Task<string?> ResolveSteamIdAsync(string gameName);
    public Task<List<Achievement>> GetAchievementsAsync(string steamId);
}