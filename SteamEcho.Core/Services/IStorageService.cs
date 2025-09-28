using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public interface IStorageService
{
    /// <summary>
    /// Saves a game to the database.
    /// </summary>
    public void SaveGame(Game game);

    /// <summary>
    /// Saves a list of games to the database.
    /// </summary>
    public void SaveGames(List<Game> games);

    /// <summary>
    /// Deletes a game from the database.
    /// </summary>
    public void DeleteGame(long steamId);

    /// <summary>
    /// Deletes multiple games from the database by their Steam IDs.
    /// </summary>
    public void DeleteGamesByIds(List<long> steamIds);

    /// <summary>
    /// Updates the executable path of a game in the database.
    /// </summary>
    public void UpdateGameExecutable(long steamId, string executablePath);

    /// <summary>
    /// Updates an achievement in the database.
    /// </summary>
    public void UpdateAchievement(long gameId, string achievementId, bool isUnlocked, DateTime? unlockDate = null, string? description = null);

    /// <summary>
    /// Save user to the database.
    /// </summary>
    public void SaveUser(SteamUserInfo userInfo);

    /// <summary>
    /// Deletes a user from the database.
    /// </summary>
    public void DeleteUser(string steamId);

    /// <summary>
    /// Loads all games from the database.
    /// </summary>
    public List<Game> LoadGames();

    /// <summary>
    /// Loads user from the database.
    /// </summary>
    public SteamUserInfo? LoadUser();

    /// <summary>
    /// Sync local database with Steam data to handle case where user has new games or unlocked achievements without using the app.
    /// </summary>
    public void SyncGames(List<Game> steamGames, List<Game> localGames);

    /// <summary>
    /// Save the selected language to the database.
    /// </summary>
    public void SaveLanguage(string cultureCode);

    /// <summary>
    /// Loads the selected language from the database.
    /// </summary>
    public string? LoadLanguage();
}