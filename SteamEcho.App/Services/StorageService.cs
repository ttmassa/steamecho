using System.Data.SQLite;
using System.IO;
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
                IsUnlocked BOOLEAN NOT NULL DEFAULT 0,
                FOREIGN KEY (GameId) REFERENCES Games(Id)
            );
        ";
        command.ExecuteNonQuery();
    }

    public void SaveGame(long steamId, string name, string executablePath, List<Achievement> achievements, string? iconUrl = null)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Games (Id, Name, ExecutablePath, IconUrl)
            VALUES (@Id, @Name, @ExecutablePath, @IconUrl);
        ";
        command.Parameters.AddWithValue("@Id", steamId);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@ExecutablePath", executablePath);
        command.Parameters.AddWithValue("@IconUrl", iconUrl);
        command.ExecuteNonQuery();

        foreach (var achievement in achievements)
        {
            SaveAchievement(steamId, achievement.Id, achievement.Name, achievement.Description, achievement.Icon, achievement.GrayIcon, achievement.GlobalPercentage, achievement.IsUnlocked);
        }
    }

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

    public void UpdateAchievement(long gameId, string achievementId, bool isUnlocked, DateTime? unlockDate = null)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Achievements
            SET IsUnlocked = @IsUnlocked, UnlockDate = @UnlockDate
            WHERE GameId = @GameId AND Id = @Id;
        ";
        command.Parameters.AddWithValue("@GameId", gameId);
        command.Parameters.AddWithValue("@Id", achievementId);
        command.Parameters.AddWithValue("@IsUnlocked", isUnlocked ? 1 : 0);
        command.Parameters.AddWithValue("@UnlockDate", unlockDate.HasValue ? (object)unlockDate.Value : DBNull.Value);
        command.ExecuteNonQuery();
    }

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
                reader.GetInt64(0).ToString(),
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
        achievementsCommand.CommandText = "SELECT GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsUnlocked FROM Achievements";

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
            bool isUnlocked = achievementsReader.GetBoolean(7);

            var achievement = new Achievement(id, name, description, icon, grayIcon, globalPercentage)
            {
                IsUnlocked = isUnlocked
            };

            // Find the game and add the achievement
            var game = games.Find(g => g.SteamId == gameId.ToString());
            game?.AddAchievement(achievement);
        }
        return games;
    }
    private void SaveAchievement(long gameId, string id, string name, string description, string? icon = null, string? grayIcon = null, double? globalPercentage = null, bool isUnlocked = false)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Achievements (GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsUnlocked)
            VALUES (@GameId, @Id, @Name, @Description, @Icon, @GrayIcon, @GlobalPercentage, @IsUnlocked);
        ";
        command.Parameters.AddWithValue("@GameId", gameId);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@Description", description);
        command.Parameters.AddWithValue("@Icon", icon);
        command.Parameters.AddWithValue("@GrayIcon", grayIcon);
        command.Parameters.AddWithValue("@GlobalPercentage", globalPercentage);
        command.Parameters.AddWithValue("@IsUnlocked", isUnlocked ? 1 : 0);
        command.ExecuteNonQuery();
    }

}