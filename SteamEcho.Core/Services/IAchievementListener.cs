namespace SteamEcho.Core.Services;

public interface IAchievementListener
{
    /// <summary>
    /// Starts listening for achievements.
    /// </summary>
    public void Start(string? gameExePath);

    /// <summary>
    /// Stops listening for achievements.
    /// </summary>
    public void Stop();

}