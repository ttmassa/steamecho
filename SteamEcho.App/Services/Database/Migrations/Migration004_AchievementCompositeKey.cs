using System.Data.SQLite;

namespace SteamEcho.App.Services.Database.Migrations;

public sealed class Migration004_AchievementCompositeKey : IDbMigration
{
    public int Version => 4;
    public string Description => "Change Achievements PK to (GameId, Id) composite key";

    public void Up(SQLiteConnection connection, SQLiteTransaction tx)
    {
        // 1. Rename old table
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Achievements RENAME TO Achievements_old;";
            cmd.ExecuteNonQuery();
        }

        // 2. Create new table with composite PK
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
                CREATE TABLE Achievements (
                    GameId BIGINT NOT NULL,
                    Id TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Icon TEXT,
                    GrayIcon TEXT,
                    GlobalPercentage REAL,
                    IsHidden BOOLEAN NOT NULL DEFAULT 0,
                    IsUnlocked BOOLEAN NOT NULL DEFAULT 0,
                    UnlockDate DATETIME,
                    PRIMARY KEY (GameId, Id),
                    FOREIGN KEY (GameId) REFERENCES Games(Id)
                );
            ";
            cmd.ExecuteNonQuery();
        }

        // 3. Copy data from old table to new table
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Achievements
                (GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsHidden, IsUnlocked, UnlockDate)
                SELECT GameId, Id, Name, Description, Icon, GrayIcon, GlobalPercentage, IsHidden, IsUnlocked, UnlockDate
                FROM Achievements_old;
            ";
            cmd.ExecuteNonQuery();
        }

        // 4. Drop old table
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DROP TABLE Achievements_old;";
            cmd.ExecuteNonQuery();
        }
    }
}