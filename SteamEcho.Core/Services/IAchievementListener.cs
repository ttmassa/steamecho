namespace SteamEcho.Core.Services;

public interface IAchievementListener
{
    /// <summary>
    /// Call this when a game starts running. Pass the game's executable path.
    /// </summary>
    public void Start(string? gameExePath);

    /// <summary>
    /// Call this to stop listening for achievements.
    /// </summary>
    public void Stop();

}