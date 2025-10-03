using System.Data.SQLite;

namespace SteamEcho.App.Services.Database.Migrations;

// Rebuild User as singleton row (Id=1), SteamId optional
public sealed class Migration003_AddIsLocal : IDbMigration
{
    public int Version => 3;
    public string Description => "Add IsLocal column to Games table";

    public void Up(SQLiteConnection connection, SQLiteTransaction tx)
    {
        if (!TableExists(connection, "Games")) return;
        if (ColumnExists(connection, tx, "Games", "IsLocal")) return;

        using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            ALTER TABLE Games ADD COLUMN IsLocal INTEGER NOT NULL DEFAULT 0;
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

    private static bool ColumnExists(SQLiteConnection c, SQLiteTransaction tx, string table, string column)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"PRAGMA table_info({table});";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}