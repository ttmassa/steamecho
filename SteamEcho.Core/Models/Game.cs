namespace SteamEcho.Core.Models;

public class Game(string name, string executablePath)
{
    public string Name { get; set; } = name;
    public string ExecutablePath { get; set; } = executablePath;
    public List<Achievement> Achievements { get; set; } = [];

    public void AddAchievement(Achievement achievement)
    {
        Achievements.Add(achievement);
    }

    public Achievement? GetAchievementById(string id)
    {
        return Achievements.Find(a => a.Id == id);
    }
}