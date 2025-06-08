using SteamEcho.Core.Models;

namespace SteamEcho.Core.Services;

public class AchievementService(Game game)
{
    private Game _game = game;

    public List<Achievement> GetAllAchievements()
    {
        return _game.Achievements;
    }

    public void UnlockAchievement(string id)
    {
        var achievement = _game.GetAchievementById(id);
        if (achievement != null && !achievement.IsUnlocked)
        {
            achievement.Unlock();
        }
    }

    public bool IsAchievementUnlocked(string id)
    {
        var achievement = _game.GetAchievementById(id);
        return achievement != null && achievement.IsUnlocked;
    }
}