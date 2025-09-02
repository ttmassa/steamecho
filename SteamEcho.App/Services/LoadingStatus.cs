namespace SteamEcho.App.Services;

public static class LoadingStatus
{
    public static event Action<string>? StatusChanged;

    // Update the loading status message in the splash screen
    public static void Update(string message)
    {
        StatusChanged?.Invoke(message);
    }
}