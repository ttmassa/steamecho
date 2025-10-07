using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamEcho.Core.Models;

public class Game(long steamId, string name, string? executablePath = null, string? iconUrl = null, bool? isLocal = false) : INotifyPropertyChanged
{
    public long SteamId { get; set; } = steamId;
    public string Name { get; set; } = name;
    public string ExecutablePath { get; set; } = executablePath ?? string.Empty;
    public string IconUrl { get; set; } = iconUrl ?? "/SteamEcho.App;component/Assets/Images/library_placeholder.png";
    public bool IsLocal { get; set; } = isLocal ?? false;
    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }
    }
    private bool _isProxyReady;
    public bool IsProxyReady
    {
        get => _isProxyReady;
        set
        {
            if (_isProxyReady != value)
            {
                _isProxyReady = value;
                OnPropertyChanged();
            }
        }
    }

    // List of achievements
    public ObservableCollection<Achievement> Achievements { get; } = [];
    // List of screenshots
    public ObservableCollection<Screenshot> Screenshots { get; } = [];
    public string AchievementsSummary => $"{Achievements.Count(a => a.IsUnlocked)}/{Achievements.Count}";
    public int UnlockedAchievementsCount => Achievements.Count(a => a.IsUnlocked);
    public int TotalAchievementsCount => Achievements.Count;
    public int LockedAchievementsCount => TotalAchievementsCount - UnlockedAchievementsCount;
    public double CompletionPercentage => TotalAchievementsCount > 0 ? (double)UnlockedAchievementsCount / TotalAchievementsCount * 100 : 0;
    public Achievement? RarestAchievement => Achievements.Where(a => a.IsUnlocked && a.GlobalPercentage.HasValue).OrderBy(a => a.GlobalPercentage).FirstOrDefault();
    public double AverageRarity
    {
        get
        {
            var unlockedWithRarity = Achievements.Where(a => a.IsUnlocked && a.GlobalPercentage.HasValue).ToList();
            return unlockedWithRarity.Count > 0 ? unlockedWithRarity.Average(a => a.GlobalPercentage!.Value) : 0;
        }
    }
    public double CompletionPercentile
    {
        get
        {
            if (Achievements.Count == 0)
                return 0;

            double weightedUnlocked = 0;
            double weightedTotal = 0;

            foreach (var ach in Achievements)
            {
                if (ach.GlobalPercentage is not double p)
                    continue;

                // convert 0-100 → 0-1
                p /= 100.0;

                double rarityWeight = 1 - p;
                weightedTotal += rarityWeight;

                if (ach.IsUnlocked)
                    weightedUnlocked += rarityWeight;
            }

            return weightedTotal > 0 ? 100 - weightedUnlocked / weightedTotal * 100.0 : 0;
        }
    }

    // Constructor for when creating a NEW game with achievements
    public Game(long steamId, string name, List<Achievement> achievements, string? executablePath, string? iconUrl = null, bool? isLocal = false)
        : this(steamId, name, executablePath, iconUrl, isLocal)
    {
        foreach (var ach in achievements)
        {
            AddAchievement(ach);
        }
    }

    public void AddAchievement(Achievement achievement)
    {
        // Subscribing to the event
        achievement.PropertyChanged += OnAchievementPropertyChanged;
        Achievements.Add(achievement);
    }

    public void UpdateAchievementLanguage(Achievement achievement)
    {
        var existing = GetAchievementById(achievement.Id);
        if (existing != null)
        {
            existing.Name = achievement.Name;
            existing.Description = achievement.Description;
        }
    }

    private void OnAchievementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If an achievement's IsUnlocked status changes, notify the UI
        // that the summary needs to be re-read.
        if (e.PropertyName == nameof(Achievement.IsUnlocked))
        {
            OnPropertyChanged(nameof(AchievementsSummary));
            OnPropertyChanged(nameof(UnlockedAchievementsCount));
            OnPropertyChanged(nameof(LockedAchievementsCount));
            OnPropertyChanged(nameof(CompletionPercentage));
            OnPropertyChanged(nameof(RarestAchievement));
            OnPropertyChanged(nameof(AverageRarity));
            OnPropertyChanged(nameof(CompletionPercentile));
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