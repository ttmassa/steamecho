using System.Data.SQLite;
using System.IO;
using SteamEcho.App.Services.Database;
using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class StorageService : IStorageService
{
    private readonly string _connectionString;

    public StorageService(string databasePath)
    {
        string directory = Path.GetDirectoryName(databasePath) ?? throw new DirectoryNotFoundException("Database directory not found.");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        _connectionString = $"Data Source={databasePath};Version=3;";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        DatabaseMigrator.Initialize(connection);
    }


    // -------- Games & Achievements --------
    public void SaveGame(Game game)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Games (Id, Name, ExecutablePath, IconUrl, IsLocal)
            VALUES (@Id, @Name, @Exe, @Icon, @IsLocal);";
        cmd.Parameters.AddWithValue("@Id", game.SteamId);
        cmd.Parameters.AddWithValue("@Name", game.Name);
        cmd.Parameters.AddWithValue("@Exe", game.ExecutablePath);
        cmd.Parameters.AddWithValue("@Icon", game.IconUrl ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IsLocal", game.IsLocal ? 1 : 0);
        cmd.ExecuteNonQuery();

        foreach (var a in game.Achievements)
        {
            SaveAchievement(game.SteamId, a);
        }
    }

    public void SaveGames(List<Game> games)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var tx = connection.BeginTransaction();

        using var gameCmd = connection.CreateCommand();
        gameCmd.Transaction = tx;
        gameCmd.CommandText = @"
            INSERT OR REPLACE INTO Games (Id, Name, ExecutablePath, IconUrl, IsLocal)
            VALUES (@Id, @Name, @Exe, @Icon, @IsLocal);";

        using var achCmd = connection.CreateCommand();
        achCmd.Transaction = tx;
        achCmd.CommandText = @"
            INSERT OR REPLACE INTO Achievements (GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsHidden, IsUnlocked, UnlockDate)
            VALUES (@GameId, @Id, @Name, @Description, @Icon, @GrayIcon, @GlobalPercentage, @IsHidden, @IsUnlocked, @UnlockDate);";

        foreach (var g in games)
        {
            gameCmd.Parameters.Clear();
            gameCmd.Parameters.AddWithValue("@Id", g.SteamId);
            gameCmd.Parameters.AddWithValue("@Name", g.Name);
            gameCmd.Parameters.AddWithValue("@Exe", g.ExecutablePath);
            gameCmd.Parameters.AddWithValue("@Icon", g.IconUrl ?? (object)DBNull.Value);
            gameCmd.Parameters.AddWithValue("@IsLocal", g.IsLocal ? 1 : 0);
            gameCmd.ExecuteNonQuery();

            foreach (var a in g.Achievements)
            {
                achCmd.Parameters.Clear();
                achCmd.Parameters.AddWithValue("@GameId", g.SteamId);
                achCmd.Parameters.AddWithValue("@Id", a.Id);
                achCmd.Parameters.AddWithValue("@Name", a.Name);
                achCmd.Parameters.AddWithValue("@Description", a.Description);
                achCmd.Parameters.AddWithValue("@Icon", a.Icon ?? (object)DBNull.Value);
                achCmd.Parameters.AddWithValue("@GrayIcon", a.GrayIcon ?? (object)DBNull.Value);
                achCmd.Parameters.AddWithValue("@GlobalPercentage", a.GlobalPercentage ?? (object)DBNull.Value);
                achCmd.Parameters.AddWithValue("@IsHidden", a.IsHidden ? 1 : 0);
                achCmd.Parameters.AddWithValue("@IsUnlocked", a.IsUnlocked ? 1 : 0);
                achCmd.Parameters.AddWithValue("@UnlockDate", a.UnlockDate ?? (object)DBNull.Value);
                achCmd.ExecuteNonQuery();
            }
        }
        tx.Commit();
    }

    public void DeleteGame(long steamId)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM Achievements WHERE GameId=@Id;
            DELETE FROM Games WHERE Id=@Id;";
        cmd.Parameters.AddWithValue("@Id", steamId);
        cmd.ExecuteNonQuery();
    }

    public void DeleteGamesByIds(List<long> ids)
    {
        if (ids.Count == 0) return;
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        var csv = string.Join(",", ids);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            DELETE FROM Achievements WHERE GameId IN ({csv});
            DELETE FROM Games WHERE Id IN ({csv});";
        cmd.ExecuteNonQuery();
    }

    public void UpdateGameExecutable(long steamId, string path)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Games SET ExecutablePath=@p WHERE Id=@Id;";
        cmd.Parameters.AddWithValue("@p", path);
        cmd.Parameters.AddWithValue("@Id", steamId);
        cmd.ExecuteNonQuery();
    }

    public void UpdateAchievement(long gameId, string achievementId, bool isUnlocked, DateTime? unlockDate = null, string? description = null)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE Achievements
            SET IsUnlocked=@IsUnlocked,
                Description=@Description,
                UnlockDate=@UnlockDate
            WHERE GameId=@GameId AND Id=@Id;";
        cmd.Parameters.AddWithValue("@GameId", gameId);
        cmd.Parameters.AddWithValue("@Id", achievementId);
        cmd.Parameters.AddWithValue("@IsUnlocked", isUnlocked ? 1 : 0);
        cmd.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@UnlockDate", unlockDate.HasValue ? (object)unlockDate.Value : DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<Game> LoadGames()
    {
        var list = new List<Game>();
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, ExecutablePath, IconUrl, IsLocal FROM Games;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Game(
                r.GetInt64(0),
                r.GetString(1),
                r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.GetInt32(4) == 1));
        }
        if (list.Count == 0) return list;

        using var achievementsCmd = connection.CreateCommand();
        achievementsCmd.CommandText = "SELECT GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsHidden, IsUnlocked, UnlockDate FROM Achievements;";
        using var achievementsReader = achievementsCmd.ExecuteReader();
        while (achievementsReader.Read())
        {
            var ach = new Achievement(
                achievementsReader.GetString(1),
                achievementsReader.GetString(2),
                achievementsReader.IsDBNull(3) ? "Hidden Description" : achievementsReader.GetString(3),
                achievementsReader.IsDBNull(4) ? null : achievementsReader.GetString(4),
                achievementsReader.IsDBNull(5) ? null : achievementsReader.GetString(5),
                achievementsReader.GetBoolean(7),
                achievementsReader.IsDBNull(6) ? null : achievementsReader.GetDouble(6))
            {
                IsUnlocked = achievementsReader.GetBoolean(8),
                UnlockDate = achievementsReader.IsDBNull(9) ? null : achievementsReader.GetDateTime(9)
            };
            list.Find(g => g.SteamId == achievementsReader.GetInt64(0))?.AddAchievement(ach);
        }
        return list;
    }

    public void SyncGames(List<Game> steamGames, List<Game> localGames)
    {
        var toUpsert = steamGames.Where(g =>
        {
            var local = localGames.FirstOrDefault(l => l.SteamId == g.SteamId);
            return local == null || !g.Achievements.SequenceEqual(local.Achievements);
        }).ToList();

        if (toUpsert.Count > 0)
            SaveGames(toUpsert);
    }
    
    // -------- User (singleton row Id=1) --------
    public void SaveUser(SteamUserInfo userInfo)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO User (Id, SteamId)
            VALUES (1, @SteamId)
            ON CONFLICT(Id) DO UPDATE SET SteamId = excluded.SteamId;";
        cmd.Parameters.AddWithValue("@SteamId", userInfo.SteamId);
        cmd.ExecuteNonQuery();
    }

    public void SaveLanguage(string cultureCode)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO User (Id, CultureCode)
            VALUES (1, @CultureCode)
            ON CONFLICT(Id) DO UPDATE SET CultureCode = excluded.CultureCode;";
        cmd.Parameters.AddWithValue("@CultureCode", cultureCode);
        cmd.ExecuteNonQuery();
    }

    public string? LoadLanguage()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT CultureCode FROM User WHERE Id=1;";
        var v = cmd.ExecuteScalar();
        return v == null || v is DBNull ? null : (string)v;
    }

    public SteamUserInfo? LoadUser()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT SteamId FROM User WHERE Id=1;";
        var v = cmd.ExecuteScalar();
        return v == null || v is DBNull ? null : new SteamUserInfo((string)v);
    }

    public void DeleteUser(string steamId)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE User SET SteamId = NULL WHERE Id=1 AND SteamId=@SteamId;";
        cmd.Parameters.AddWithValue("@SteamId", steamId);
        cmd.ExecuteNonQuery();
    }

    private void SaveAchievement(long gameId, Achievement a)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Achievements (GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsHidden, IsUnlocked, UnlockDate)
            VALUES (@GameId, @Id, @Name, @Description, @Icon, @GrayIcon, @GlobalPercentage, @IsHidden, @IsUnlocked, @UnlockDate);";
        cmd.Parameters.AddWithValue("@GameId", gameId);
        cmd.Parameters.AddWithValue("@Id", a.Id);
        cmd.Parameters.AddWithValue("@Name", a.Name);
        cmd.Parameters.AddWithValue("@Description", a.Description);
        cmd.Parameters.AddWithValue("@Icon", a.Icon ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@GrayIcon", a.GrayIcon ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@GlobalPercentage", a.GlobalPercentage.HasValue ? a.GlobalPercentage.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IsHidden", a.IsHidden ? 1 : 0);
        cmd.Parameters.AddWithValue("@IsUnlocked", a.IsUnlocked ? 1 : 0);
        cmd.Parameters.AddWithValue("@UnlockDate", a.UnlockDate.HasValue ? (object)a.UnlockDate.Value : DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}