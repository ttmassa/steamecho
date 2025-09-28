using System.Data.SQLite;

namespace SteamEcho.App.Services.Database.Migrations;

// Rebuild User as singleton row (Id=1), SteamId optional
public sealed class Migration002_UserSingleton : IDbMigration
{
    public int Version => 2;
    public string Description => "Convert User table to singleton (Id=1)";

    public void Up(SQLiteConnection connection, SQLiteTransaction tx)
    {
        if (!TableExists(connection, "User")) return;
        if (HasIdColumn(connection, tx)) return; // already migrated

        using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            CREATE TABLE User_new (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                SteamId TEXT UNIQUE,
                CultureCode TEXT
            );
            INSERT INTO User_new (Id, SteamId, CultureCode)
            SELECT 1, SteamId, CultureCode FROM User LIMIT 1;
            DROP TABLE User;
            ALTER TABLE User_new RENAME TO User;
        ";
        cmd.ExecuteNonQuery();
    }

    private static bool TableExists(SQLiteConnection c, string name)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n;";
        cmd.Parameters.AddWithValue("@n", name);
        return cmd.ExecuteScalar() != null;
    }

    private static bool HasIdColumn(SQLiteConnection c, SQLiteTransaction tx)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "PRAGMA table_info(User);";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            if (string.Equals(r.GetString(1), "Id", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}