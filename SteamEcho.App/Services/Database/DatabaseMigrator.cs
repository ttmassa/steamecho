using System.Data.SQLite;
using System.Reflection;

namespace SteamEcho.App.Services.Database;

internal static class DatabaseMigrator
{
    // Table that tracks which migrations have been applied
    private const string MigrationTable = "__MigrationsApplied";
    // Cache of discovered migrations
    private static List<IDbMigration>? _cached;

    public static void Initialize(SQLiteConnection connection)
    {
        // Ensure the migrations table exists
        EnsureMigrationTable(connection);

        // Check if database is new
        bool fresh = IsFresh(connection);
        // Get all available migrations
        var migrations = LoadMigrations();

        if (fresh)
        {
            // Create base tables
            CreateLatestSnapshot(connection);
            // Mark all migrations as applied
            using var tx = connection.BeginTransaction();
            foreach (var m in migrations)
                RecordApplied(connection, tx, m);
            tx.Commit();
            return;
        }

        // Apply any pending migrations
        ApplyPending(connection, migrations);
    }

    // Load and cache all migrations from the current assembly
    private static List<IDbMigration> LoadMigrations()
    {
        if (_cached != null) return _cached;
        _cached = [.. Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IDbMigration).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => (IDbMigration)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Version)];
        return _cached;
    }

    private static bool IsFresh(SQLiteConnection c) =>
        !TableExists(c, "Games") && !TableExists(c, "Achievements") && !TableExists(c, "User");

    private static void EnsureMigrationTable(SQLiteConnection c)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {MigrationTable} (
                Version INTEGER PRIMARY KEY,
                AppliedOn TEXT NOT NULL,
                Description TEXT
            );";
        cmd.ExecuteNonQuery();
    }

    // Get a set of all applied migration versions
    private static HashSet<int> GetApplied(SQLiteConnection c)
    {
        var set = new HashSet<int>();
        using var cmd = c.CreateCommand();
        cmd.CommandText = $"SELECT Version FROM {MigrationTable};";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            set.Add(r.GetInt32(0));
        return set;
    }

    // Apply all migrations that have not yet been applied
    private static void ApplyPending(SQLiteConnection c, List<IDbMigration> migrations)
    {
        var applied = GetApplied(c);
        using var tx = c.BeginTransaction();
        foreach (var m in migrations)
        {
            if (applied.Contains(m.Version)) continue;
            m.Up(c, tx);
            RecordApplied(c, tx, m);
        }
        tx.Commit();
    }

    // Save in the migrations table that a migration has been applied
    private static void RecordApplied(SQLiteConnection c, SQLiteTransaction tx, IDbMigration m)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $@"
            INSERT OR IGNORE INTO {MigrationTable} (Version, AppliedOn, Description)
            VALUES (@Version, datetime('now'), @Description);";
        cmd.Parameters.AddWithValue("@Version", m.Version);
        cmd.Parameters.AddWithValue("@Description", m.Description);
        cmd.ExecuteNonQuery();
    }

    private static void CreateLatestSnapshot(SQLiteConnection c)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
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
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                SteamId TEXT UNIQUE,
                CultureCode TEXT
            );";
        cmd.ExecuteNonQuery();
    }

    private static bool TableExists(SQLiteConnection c, string name)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n;";
        cmd.Parameters.AddWithValue("@n", name);
        return cmd.ExecuteScalar() != null;
    }
}