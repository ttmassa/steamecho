namespace SteamEcho.Core.Services;

public interface IGameProcessService
{
    /// <summary>
    /// Starts monitoring running processes for the games in the collection.
    /// </summary>
    public void Start();

    /// <summary>
    /// Stops monitoring running processes.
    /// </summary>
    public void Stop();
}