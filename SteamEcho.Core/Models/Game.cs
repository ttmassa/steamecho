namespace SteamEcho.Core.Models;

public class Game(string steamId, string name, string executablePath, string? iconUrl = null)
{
    public string SteamId { get; set; } = steamId;
    public string Name { get; set; } = name;
    public string ExecutablePath { get; set; } = executablePath;
    public string? IconUrl { get; set; } = iconUrl;
    public List<Achievement> Achievements { get; set; } = [];
    public string AchievementsSummary => $"{GetUnlockedAchievements().Count} / {Achievements.Count}";

    public void AddAchievement(Achievement achievement)
    {
        Achievements.Add(achievement);
    }

    public void AddAchievements(List<Achievement> achievements)
    {
        Achievements.AddRange(achievements);
    }

    public Achievement? GetAchievementById(string id)
    {
        return Achievements.Find(a => a.Id == id);
    }
    public List<Achievement> GetUnlockedAchievements()
    {
        return Achievements.FindAll(a => a.IsUnlocked);
    }
}