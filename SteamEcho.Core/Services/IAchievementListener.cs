
namespace SteamEcho.Core.Services;

public interface IAchievementListener
{
    /// <summary>
    /// Event triggered when an achievement is unlocked.
    /// </summary>
    public event Action<string>? AchievementUnlocked;

    /// <summary>
    /// Starts listening for achievements.
    /// </summary>
    public void Start(string? gameExePath);

    /// <summary>
    /// Stops listening for achievements.
    /// </summary>
    public void Stop();

}