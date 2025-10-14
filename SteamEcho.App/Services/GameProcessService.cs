using System.Collections.ObjectModel;
using System.Diagnostics;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class GameProcessService(ObservableCollection<Game> games) : IGameProcessService
{
    private readonly ObservableCollection<Game> _games = games;
    private Timer? _timer;
    private Game? _lastRunningGame;

    public event Action<Game?>? RunningGameChanged;

    public void StartMonitoring()
    {
        _timer?.Change(Timeout.Infinite, 0);
        _timer = new Timer(CheckProcesses, null, 0, 2000);
    }

    public void StopMonitoring()
    {
        _timer?.Change(Timeout.Infinite, 0);
    }

    public Game? GetRunningGame()
    {
        return _lastRunningGame;
    }


    # region Helper methods

    private void CheckProcesses(object? state)
    {
        var runningProcesses = Process.GetProcesses();
        var gamesCopy = _games.ToList();
        Game? runningGame = null;

        foreach (var game in gamesCopy)
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
            if (game.IsRunning)
                runningGame = game;
        }

        // Only fire event if the running game changed
        if (!ReferenceEquals(runningGame, _lastRunningGame))
        {
            _lastRunningGame = runningGame;
            RunningGameChanged?.Invoke(runningGame);
        }
    }

    #endregion
}