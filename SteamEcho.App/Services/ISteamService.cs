using SteamEcho.App.DTOs;
using SteamEcho.Core.Models;

namespace SteamEcho.App.Services;

public interface ISteamService
{
    public Task<GameInfo?> ResolveSteamIdAsync(string gameName);
    public Task<List<Achievement>> GetAchievementsAsync(string steamId);
}