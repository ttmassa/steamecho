namespace SteamEcho.Core.Services;

public interface IUINotificationService
{
    /// <summary>
    /// The current UI notification message, or null if none is visible.
    /// </summary>
    string? UINotificationMessage { get; set; }

    /// <summary>
    /// Indicates whether a UI notification is currently visible.
    /// </summary>
    bool IsUINotificationVisible { get; }

    /// <summary>
    /// Shows a UI notification with the specified message for the given duration in seconds.
    /// </summary>
    void ShowUINotification(string message, int durationSeconds = 5);

    /// <summary>
    /// Dismisses the currently visible UI notification, if any.
    /// </summary>
    void DismissUINotification();
}