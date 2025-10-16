using SteamEcho.Core.Models;
using System.Collections.ObjectModel;

namespace SteamEcho.Core.Services;

public interface IGameProcessService
{
    /// <summary>
    /// Event triggered when the currently running game changes.
    /// </summary>
    public event Action<Game?>? RunningGameChanged;
    
    /// <summary>
    /// Sets the collection of games to monitor.
    /// </summary>
    void SetGamesCollection(ObservableCollection<Game> games);

    /// <summary>
    /// Starts monitoring running processes for the games in the collection.
    /// </summary>
    public void StartMonitoring();

    /// <summary>
    /// Stops monitoring running processes.
    /// </summary>
    public void StopMonitoring();

    /// <summary>
    /// Gets the currently running game, if any.
    /// </summary>
    public Game? GetRunningGame();
}