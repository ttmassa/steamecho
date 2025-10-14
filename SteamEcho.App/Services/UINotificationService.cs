using System.ComponentModel;
using System.Runtime.CompilerServices;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

class UINotificationService : IUINotificationService, INotifyPropertyChanged {
    private string? _uiNotificationMessage;
    public string? UINotificationMessage
    {
        get => _uiNotificationMessage;
        set
        {
            if (_uiNotificationMessage != value)
            {
                _uiNotificationMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUINotificationVisible));
            }
        }
    }
    private System.Timers.Timer? _uiNotificationTimer;
    public bool IsUINotificationVisible => !string.IsNullOrEmpty(UINotificationMessage);

    // Show temporary UI notification message
    public void ShowUINotification(string message, int durationSeconds = 5)
    {
        UINotificationMessage = message;

        _uiNotificationTimer?.Stop();
        _uiNotificationTimer?.Dispose();

        _uiNotificationTimer = new System.Timers.Timer(durationSeconds * 1000);
        _uiNotificationTimer.Elapsed += (s, e) =>
        {
            UINotificationMessage = null;
            _uiNotificationTimer?.Stop();
            _uiNotificationTimer?.Dispose();
            _uiNotificationTimer = null;
        };
        _uiNotificationTimer.AutoReset = false;
        _uiNotificationTimer.Start();
    }
    
    public void DismissUINotification()
    {
        UINotificationMessage = null;
        _uiNotificationTimer?.Stop();
        _uiNotificationTimer?.Dispose();
        _uiNotificationTimer = null;
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}