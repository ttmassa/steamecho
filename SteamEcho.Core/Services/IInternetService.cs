namespace SteamEcho.Core.Services;

public interface IInternetService
{
    /// <summary>
    /// Indicates whether there is an active internet connection.
    /// </summary>
    bool HasInternet { get; }

    /// <summary>
    /// Event fired when the internet connection status changes.
    /// </summary>
    event Action<bool>? InternetStatusChanged;
    
    /// <summary>
    /// Starts monitoring internet connection status.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops monitoring internet connection status.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Checks internet connection.
    /// </summary>
    Task<bool> CheckInternetConnectivityAsync();
}