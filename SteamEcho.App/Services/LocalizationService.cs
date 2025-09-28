using System.ComponentModel;
using System.Globalization;
using Resx = SteamEcho.App.Resources.Resources;

namespace SteamEcho.App.Services;

public class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    // Indexer to bind with Loc[Key]
    public string this[string key] =>
        Resx.ResourceManager.GetString(key, Resx.Culture ?? CultureInfo.CurrentUICulture) ?? key;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}