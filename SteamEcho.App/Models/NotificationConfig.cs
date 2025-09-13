using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamEcho.App.Models;

public class NotificationConfig : INotifyPropertyChanged
{
    private double _notificationSize = 5;
    private int _notificationTime = 7;
    private string _notificationColor = "#4A4A4D";

    public double NotificationSize
    {
        get => _notificationSize;
        set
        {
            if (_notificationSize != value)
            {
                _notificationSize = value;
                OnPropertyChanged();
            }
        }
    }
    public int NotificationTime
    {
        get => _notificationTime;
        set {
            if (_notificationTime != value)
            {
                _notificationTime = value;
                OnPropertyChanged();
            }
        }
    }
    public string NotificationColor
    {
        get => _notificationColor;
        set
        {
            if (_notificationColor != value)
            {
                _notificationColor = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}