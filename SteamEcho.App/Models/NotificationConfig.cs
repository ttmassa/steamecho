using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamEcho.App.Models;

public class NotificationConfig : INotifyPropertyChanged
{
    private double _notificationSize = 5;
    private string _notificationColor = "#4a4a4dac";

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