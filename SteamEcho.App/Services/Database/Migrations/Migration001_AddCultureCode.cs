using System.Data.SQLite;

namespace SteamEcho.App.Services.Database.Migrations;

// Legacy: Add CultureCode to old User table (pre‑singleton)
public sealed class Migration001_AddCultureCode : IDbMigration
{
    public int Version => 1;
    public string Description => "Add CultureCode column to legacy User table";

    public void Up(SQLiteConnection connection, SQLiteTransaction tx)
    {
        if (!TableExists(connection, "User")) return;
        if (ColumnExists(connection, tx, "User", "CultureCode")) return;

        using var alter = connection.CreateCommand();
        alter.Transaction = tx;
        alter.CommandText = "ALTER TABLE User ADD COLUMN CultureCode TEXT;";
        alter.ExecuteNonQuery();
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