using System.Windows;
using SteamEcho.App.Services;

namespace SteamEcho.App.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        LoadingStatus.StatusChanged += OnStatusChanged;
        Unloaded += (s, e) => LoadingStatus.StatusChanged -= OnStatusChanged;
    }

    private void OnStatusChanged(string message)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = message;
        });
    }
}