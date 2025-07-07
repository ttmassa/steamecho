using System.Collections.ObjectModel;
using System.Diagnostics;
using SteamEcho.Core.Models;

namespace SteamEcho.App.Services;

public class GameProcessService
{
    private readonly ObservableCollection<Game> _games;
    private Timer? _timer;

    public GameProcessService(ObservableCollection<Game> games)
    {
        _games = games;
    }

    public void Start()
    {
        // Ensure only one timer is running
        _timer?.Change(Timeout.Infinite, 0);
        _timer = new Timer(CheckProcesses, null, 0, 2000);
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, 0);
    }

    private void CheckProcesses(object? state)
    {
        var runningProcesses = Process.GetProcesses();
        foreach (var game in _games)
        {
            var process = runningProcesses.FirstOrDefault(p =>
            {
                try
                {
                    return !p.HasExited && string.Equals(p.MainModule?.FileName, game.ExecutablePath, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            });

            game.IsRunning = process != null;
        }
    }
}