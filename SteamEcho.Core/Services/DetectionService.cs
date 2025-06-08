using System.Timers;

namespace SteamEcho.Core.Services;

public class DetectionService
{
    private readonly AchievementService _achievementService;
    private readonly System.Timers.Timer _timer;

    public DetectionService(AchievementService achievementService)
    {
        _achievementService = achievementService;
        _timer = new System.Timers.Timer(5000);
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = true;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void OnTimedEvent(object? source, ElapsedEventArgs e)
    {
        var achievements = _achievementService.GetAllAchievements();
        var firstLocked = achievements.Find(a => !a.IsUnlocked);

        if (firstLocked != null)
        {
            _achievementService.UnlockAchievement(firstLocked.Id);
            Console.WriteLine($"Succès débloqué automatiquement : {firstLocked.Name}");
            Stop();
        }
    }
}