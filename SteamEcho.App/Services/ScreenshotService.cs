using System.IO;
using Microsoft.Win32;
using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class ScreenshotService : IScreenshotService
{
    private FileSystemWatcher? _watcher;
    private Timer? _directoryCheckTimer;
    private SteamUserInfo? _currentUser;
    private Game? _monitoredGame;
    public event Action<Screenshot>? ScreenshotTaken;

    public void StartMonitoring(SteamUserInfo? user, Game game)
    {
        StopMonitoring();
        _currentUser = user;
        _monitoredGame = game;

        // Start a timer that periodically checks for the screenshot directory's existence.
        _directoryCheckTimer = new Timer(CheckAndStartWatcher, null, 0, 2000);
    }

    private void CheckAndStartWatcher(object? state)
    {
        if (_monitoredGame == null)
        {
            StopMonitoring();
            return;
        }

        string? screenshotDir = GetScreenshotDirectory(_currentUser, _monitoredGame);
        if (string.IsNullOrWhiteSpace(screenshotDir)) return;

        if (!Directory.Exists(screenshotDir))
        {
            // For non-Steam games, create the directory.
            // For Steam games, we wait for Steam to create it.
            if (_currentUser == null || string.IsNullOrEmpty(_currentUser.SteamId))
            {
                try { Directory.CreateDirectory(screenshotDir); }
                catch { return; /* Failed to create, will retry */ }
            }
            return; // Directory not ready, timer will check again.
        }
        
        // Once the directory exists, stop the timer and start the FileSystemWatcher.
        _directoryCheckTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _directoryCheckTimer?.Dispose();
        _directoryCheckTimer = null;

        _watcher = new FileSystemWatcher(screenshotDir)
        {
            NotifyFilter = NotifyFilters.FileName,
            Filter = "*.jpg",
            EnableRaisingEvents = true
        };

        _watcher.Created += OnScreenshotTaken;
    }

    public void StopMonitoring()
    {
        _directoryCheckTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _directoryCheckTimer?.Dispose();
        _directoryCheckTimer = null;

        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
        _monitoredGame = null;
    }

    private void OnScreenshotTaken(object sender, FileSystemEventArgs e)
    {
        // A small delay helps ensure the file is fully written before we access it.
        Thread.Sleep(500); 
        
        try
        {
            // Verify the file is accessible before raising the event.
            using (File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var screenshot = new Screenshot(e.FullPath);
                ScreenshotTaken?.Invoke(screenshot);
            }
        }
        catch (IOException)
        {
            // The file might be temporarily locked by another process. Ignore for now.
        }
    }

    public static string? GetScreenshotDirectory(SteamUserInfo? user, Game game)
    {
        // If user is logged in and the game has a Steam ID, use the specific Steam screenshot folder.
        if (user != null && !string.IsNullOrEmpty(user.SteamId) && game.SteamId > 0)
        {
            var steamPath = GetSteamInstallationPath();
            if (string.IsNullOrWhiteSpace(steamPath)) return null;

            // Convert the user's steamID64 to the accountID used for folder paths.
            if (!long.TryParse(user.SteamId, out long steamId64)) return null;
            long accountId = steamId64 - 76561197960265728;

            // Path is .../userdata/<accountID>/760/remote/<gameID>/screenshots
            return Path.Combine(steamPath, "userdata", accountId.ToString(), "760", "remote", game.SteamId.ToString(), "screenshots");
        }

        // Fallback to a local folder in the game directory for non-steam games.
        if (!string.IsNullOrEmpty(game.ExecutablePath))
        {
            var gameDir = Path.GetDirectoryName(game.ExecutablePath);
            if (!string.IsNullOrEmpty(gameDir))
            {
                return Path.Combine(gameDir, "steamecho_screenshots");
            }
        }

        return null;
    }

    private static string? GetSteamInstallationPath()
    {
        return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
    }
}