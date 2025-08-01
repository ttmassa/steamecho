using System.Data.SQLite;
using System.IO;
using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;

namespace SteamEcho.App.Services;

public class StorageService
{
    private readonly string _connectionString;

    public StorageService(string databasePath)
    {
        // Ensure the directory exists
        string directory = Path.GetDirectoryName(databasePath) ?? throw new DirectoryNotFoundException("Database directory not found.");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Data Source={databasePath};Version=3;";
        InitializeDatabase();
    }

    /// <summary>
    /// Initialize the database.
    /// </summary>
    public void InitializeDatabase()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Games (
                Id BIGINT PRIMARY KEY,
                Name TEXT NOT NULL,
                ExecutablePath TEXT NOT NULL,
                IconUrl TEXT
            );
            CREATE TABLE IF NOT EXISTS Achievements (
                GameId BIGINT NOT NULL,
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Description TEXT,
                Icon TEXT,
                GrayIcon TEXT,
                GlobalPercentage REAL,
                IsHidden BOOLEAN NOT NULL DEFAULT 0,
                IsUnlocked BOOLEAN NOT NULL DEFAULT 0,
                UnlockDate DATETIME,
                FOREIGN KEY (GameId) REFERENCES Games(Id)
            );
            CREATE TABLE IF NOT EXISTS User (
                SteamId TEXT PRIMARY KEY
            );
        ";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Saves a game to the database.
    /// </summary>
    public void SaveGame(Game game)
    {
        // Save the game details
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Games (Id, Name, ExecutablePath, IconUrl)
            VALUES (@Id, @Name, @ExecutablePath, @IconUrl);
        ";
        command.Parameters.AddWithValue("@Id", game.SteamId);
        command.Parameters.AddWithValue("@Name", game.Name);
        command.Parameters.AddWithValue("@ExecutablePath", game.ExecutablePath);
        command.Parameters.AddWithValue("@IconUrl", game.IconUrl);
        command.ExecuteNonQuery();

        foreach (var achievement in game.Achievements)
        {
            SaveAchievement(game.SteamId, achievement);
        }
    }

    /// <summary>
    /// Saves a list of games to the database.
    /// </summary>
    public void SaveGames(List<Game> games)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var gameCommand = connection.CreateCommand();
        gameCommand.Transaction = transaction;
        gameCommand.CommandText = @"
            INSERT OR REPLACE INTO Games (Id, Name, ExecutablePath, IconUrl)
            VALUES (@Id, @Name, @ExecutablePath, @IconUrl);
        ";

        var achievementCommand = connection.CreateCommand();
        achievementCommand.Transaction = transaction;
        achievementCommand.CommandText = @"
            INSERT OR REPLACE INTO Achievements (GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsHidden, IsUnlocked, UnlockDate)
            VALUES (@GameId, @Id, @Name, @Description, @Icon, @GrayIcon, @GlobalPercentage, @IsHidden, @IsUnlocked, @UnlockDate);
        ";

        foreach (var game in games)
        {
            gameCommand.Parameters.Clear();
            gameCommand.Parameters.AddWithValue("@Id", game.SteamId);
            gameCommand.Parameters.AddWithValue("@Name", game.Name);
            gameCommand.Parameters.AddWithValue("@ExecutablePath", game.ExecutablePath);
            gameCommand.Parameters.AddWithValue("@IconUrl", game.IconUrl ?? (object)DBNull.Value);
            gameCommand.ExecuteNonQuery();

            foreach (var achievement in game.Achievements)
            {
                achievementCommand.Parameters.Clear();
                achievementCommand.Parameters.AddWithValue("@GameId", game.SteamId);
                achievementCommand.Parameters.AddWithValue("@Id", achievement.Id);
                achievementCommand.Parameters.AddWithValue("@Name", achievement.Name);
                achievementCommand.Parameters.AddWithValue("@Description", achievement.Description);
                achievementCommand.Parameters.AddWithValue("@Icon", achievement.Icon ?? (object)DBNull.Value);
                achievementCommand.Parameters.AddWithValue("@GrayIcon", achievement.GrayIcon ?? (object)DBNull.Value);
                achievementCommand.Parameters.AddWithValue("@GlobalPercentage", achievement.GlobalPercentage.HasValue ? achievement.GlobalPercentage.Value : DBNull.Value);
                achievementCommand.Parameters.AddWithValue("@IsHidden", achievement.IsHidden ? 1 : 0);
                achievementCommand.Parameters.AddWithValue("@IsUnlocked", achievement.IsUnlocked ? 1 : 0);
                achievementCommand.Parameters.AddWithValue("@UnlockDate", achievement.UnlockDate.HasValue ? (object)achievement.UnlockDate.Value : DBNull.Value);
                achievementCommand.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }
    
    /// <summary>
    /// Deletes a game from the database.
    /// </summary>
    public void DeleteGame(long steamId)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM Games WHERE Id = @Id;
            DELETE FROM Achievements WHERE GameId = @Id;
        ";
        command.Parameters.AddWithValue("@Id", steamId);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Updates the executable path of a game in the database.
    /// </summary>
    public void UpdateGameExecutable(long steamId, string executablePath)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Games
            SET ExecutablePath = @ExecutablePath
            WHERE Id = @Id;
        ";
        command.Parameters.AddWithValue("@Id", steamId);
        command.Parameters.AddWithValue("@ExecutablePath", executablePath);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Updates an achievement in the database.
    /// </summary>
    public void UpdateAchievement(long gameId, string achievementId, bool isUnlocked, DateTime? unlockDate = null, string? description = null)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Achievements
            SET IsUnlocked = @IsUnlocked, Description = @Description, UnlockDate = @UnlockDate
            WHERE GameId = @GameId AND Id = @Id;
        ";
        command.Parameters.AddWithValue("@GameId", gameId);
        command.Parameters.AddWithValue("@Id", achievementId);
        command.Parameters.AddWithValue("@IsUnlocked", isUnlocked ? 1 : 0);
        command.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@UnlockDate", unlockDate.HasValue ? (object)unlockDate.Value : DBNull.Value);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Save user to the database.
    /// </summary>
    public void SaveUser(SteamUserInfo userInfo)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO User (SteamId)
            VALUES (@SteamId);
        ";
        command.Parameters.AddWithValue("@SteamId", userInfo.SteamId);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Deletes all users from the database.
    /// </summary>
    public void DeleteUser()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM User";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Loads all games from the database.
    /// </summary>
    public List<Game> LoadGames()
    {
        var games = new List<Game>();

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        // Load games
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, ExecutablePath, IconUrl FROM Games";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var game = new Game(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3)
            );
            games.Add(game);
        }

        if (games.Count == 0)
        {
            return games;
        }

        // Load achievements
        using var achievementsCommand = connection.CreateCommand();
        achievementsCommand.CommandText = "SELECT GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsHidden, IsUnlocked, UnlockDate FROM Achievements";

        using var achievementsReader = achievementsCommand.ExecuteReader();
        while (achievementsReader.Read())
        {
            long gameId = achievementsReader.GetInt64(0);
            string id = achievementsReader.GetString(1);
            string name = achievementsReader.GetString(2);
            string description = achievementsReader.GetString(3);
            string? icon = achievementsReader.IsDBNull(4) ? null : achievementsReader.GetString(4);
            string? grayIcon = achievementsReader.IsDBNull(5) ? null : achievementsReader.GetString(5);
            double? globalPercentage = achievementsReader.IsDBNull(6) ? null : achievementsReader.GetDouble(6);
            bool isHidden = achievementsReader.GetBoolean(7);
            bool isUnlocked = achievementsReader.GetBoolean(8);
            DateTime? unlockDate = achievementsReader.IsDBNull(9) ? null : achievementsReader.GetDateTime(9);

            var achievement = new Achievement(id, name, description, icon, grayIcon, isHidden, globalPercentage)
            {
                IsUnlocked = isUnlocked,
                UnlockDate = unlockDate
            };

            // Find the game and add the achievement
            var game = games.Find(g => g.SteamId == gameId);
            game?.AddAchievement(achievement);
        }
        return games;
    }

    /// <summary>
    /// Loads user from the database.
    /// </summary>
    public SteamUserInfo? LoadUser()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SteamId FROM User";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {

            var user = new SteamUserInfo(reader.GetString(0));
            return user;
        }
        return null;
    }

    /// <summary>
    /// Saves an achievement to the database.
    /// </summary>
    private void SaveAchievement(long gameId, Achievement achievement)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Achievements (GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsHidden, IsUnlocked, UnlockDate)
            VALUES (@GameId, @Id, @Name, @Description, @Icon, @GrayIcon, @GlobalPercentage, @IsHidden, @IsUnlocked, @UnlockDate);
        ";
        command.Parameters.AddWithValue("@GameId", gameId);
        command.Parameters.AddWithValue("@Id", achievement.Id);
        command.Parameters.AddWithValue("@Name", achievement.Name);
        command.Parameters.AddWithValue("@Description", achievement.Description);
        command.Parameters.AddWithValue("@Icon", achievement.Icon ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@GrayIcon", achievement.GrayIcon ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@GlobalPercentage", achievement.GlobalPercentage.HasValue ? achievement.GlobalPercentage.Value : DBNull.Value);
        command.Parameters.AddWithValue("@IsHidden", achievement.IsHidden ? 1 : 0);
        command.Parameters.AddWithValue("@IsUnlocked", achievement.IsUnlocked ? 1 : 0);
        command.Parameters.AddWithValue("@UnlockDate", achievement.UnlockDate.HasValue ? (object)achievement.UnlockDate.Value : DBNull.Value);
        command.ExecuteNonQuery();
    }

}