using System.Data.SQLite;

namespace SteamEcho.App.Services.Database;

public interface IDbMigration
{
    int Version { get; }
    string Description { get; }
    void Up(SQLiteConnection connection, SQLiteTransaction transaction);
}