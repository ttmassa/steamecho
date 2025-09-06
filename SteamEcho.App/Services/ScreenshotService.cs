using System.IO;
using Microsoft.Win32;
using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class ScreenshotService : IScreenshotService
{
    private FileSystemWatcher? _watcher;
    public event Action<Screenshot>? ScreenshotTaken;

    public void StartMonitoring(SteamUserInfo user, Game game)
    {
        StopMonitoring();

        var steamPath = GetSteamInstallationPath();
        if (string.IsNullOrWhiteSpace(steamPath) || game == null) return;

        var screenshotDir = Path.Combine(steamPath, "userdata", user.SteamId.ToString(), "760", "remote", game.SteamId.ToString(), "screenshots");

        if (!Directory.Exists(screenshotDir))
        {
            try
            {
                Directory.CreateDirectory(screenshotDir);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to create screenshot directory: {e.Message}");
                return;
            }
        }

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
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnScreenshotTaken;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnScreenshotTaken(object sender, FileSystemEventArgs e)
    {
        // Small delay to ensure the file is fully written
        Thread.Sleep(500);
        var screenshot = new Screenshot(e.FullPath);
        ScreenshotTaken?.Invoke(screenshot);
    }

    private static string? GetSteamInstallationPath()
    {
        // Steam path from registry
        return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
    }
}