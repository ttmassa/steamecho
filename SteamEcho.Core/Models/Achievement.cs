using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamEcho.Core.Models;

public class Achievement(string id, string name, string description, string? icon = null, string? grayIcon = null, double? globalPercentage = null) : INotifyPropertyChanged
{
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public string? Icon { get; set; } = icon;
    public string? GrayIcon { get; set; } = grayIcon;
    public double? GlobalPercentage { get; set; } = globalPercentage;

    private bool _isUnlocked;
    public bool IsUnlocked
    {
        get => _isUnlocked;
        set
        {
            if (_isUnlocked != value)
            {
                _isUnlocked = value;
                OnPropertyChanged();
            }
        }
    }

    private DateTime? _unlockDate;
    public DateTime? UnlockDate
    {
        get => _unlockDate;
        set
        {
            if (_unlockDate != value)
            {
                _unlockDate = value;
                OnPropertyChanged();
            }
        }
    }

    public void Unlock()
    {
        IsUnlocked = true;
        UnlockDate = DateTime.Now;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}