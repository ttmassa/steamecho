using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamEcho.Core.Models;

public class Game(string steamId, string name, string executablePath, string? iconUrl) : INotifyPropertyChanged
{
    public string SteamId { get; set; } = steamId;
    public string Name { get; set; } = name;
    public string ExecutablePath { get; set; } = executablePath;
    public string IconUrl { get; set; } = iconUrl ?? "/SteamEcho.App;component/Assets/Images/library_placeholder.png";

    public ObservableCollection<Achievement> Achievements { get; } = [];

    public string AchievementsSummary => $"{Achievements.Count(a => a.IsUnlocked)}/{Achievements.Count}";

    // Constructor for when creating a NEW game with achievements
    public Game(string steamId, string name, string executablePath, List<Achievement> achievements, string? iconUrl = null)
        : this(steamId, name, executablePath, iconUrl)
    {
        foreach (var ach in achievements)
        {
            AddAchievement(ach);
        }
    }

    public void AddAchievement(Achievement achievement)
    {
        // Subscribing to the event.
        achievement.PropertyChanged += OnAchievementPropertyChanged;
        Achievements.Add(achievement);
    }

    private void OnAchievementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If an achievement's IsUnlocked status changes, notify the UI
        // that the summary needs to be re-read.
        if (e.PropertyName == nameof(Achievement.IsUnlocked))
        {
            OnPropertyChanged(nameof(AchievementsSummary));
        }
    }

    public Achievement? GetAchievementById(string achievementId)
    {
        return Achievements.FirstOrDefault(a => a.Id == achievementId);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}