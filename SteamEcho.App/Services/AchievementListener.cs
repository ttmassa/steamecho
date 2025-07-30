using System.IO;
using System.Text.Json;
using System.Windows;
using SteamEcho.App.Views;

namespace SteamEcho.App.Services;

public class AchievementListener
{
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _cts;
    private string? _jsonFilePath;
    private HashSet<long> _seenTimestamps = new();
    private string? _currentWatchedDir;

    public event Action<string>? AchievementUnlocked;

    /// <summary>
    /// Call this when a game starts running. Pass the game's executable path.
    /// </summary>
    public void Start(string? gameExePath)
    {
        Stop();

        if (string.IsNullOrEmpty(gameExePath) || !File.Exists(gameExePath))
        {
            // No executable path: prompt user to set it in the UI
            var dialog = new MessageDialog("Please set the game executable path by right-clicking on the game in library and selecting 'Set Executable'.", "No Executable Path");
            dialog.ShowDialog();
            return;
        }

        var gameDir = Path.GetDirectoryName(gameExePath)!;
        _jsonFilePath = Path.Combine(gameDir, "achievement_notifications.json");
        _currentWatchedDir = gameDir;
        _cts = new CancellationTokenSource();

        _watcher = new FileSystemWatcher(gameDir, "achievement_notifications.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;

        // Initial read in case file already exists
        _ = Task.Run(() => ProcessFileAsync(_cts.Token));
    }

    /// <summary>
    /// Call this to stop listening for achievements.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
        _seenTimestamps.Clear();
        _currentWatchedDir = null;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_cts?.IsCancellationRequested == false)
            _ = Task.Run(() => ProcessFileAsync(_cts.Token));
    }

    private async Task ProcessFileAsync(CancellationToken token)
    {
        if (_jsonFilePath == null) return;

        // Wait briefly to avoid file lock issues
        await Task.Delay(200, token);

        try
        {
            if (!File.Exists(_jsonFilePath)) return;

            string json = await File.ReadAllTextAsync(_jsonFilePath, token);
            var notifications = JsonSerializer.Deserialize<List<AchievementNotification>>(json);

            if (notifications != null)
            {
                foreach (var notif in notifications)
                {
                    if (notif.event_type == "unlocked" && !_seenTimestamps.Contains(notif.timestamp))
                    {
                        _seenTimestamps.Add(notif.timestamp);
                        AchievementUnlocked?.Invoke(notif.achievement_id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading achievement notifications: {ex.Message}");
        }
    }

    private class AchievementNotification
    {
        public string achievement_id { get; set; } = "";
        public string achievement_name { get; set; } = "";
        public string event_type { get; set; } = "";
        public long timestamp { get; set; }
    }
}