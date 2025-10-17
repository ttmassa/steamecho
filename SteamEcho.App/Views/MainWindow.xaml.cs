using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using SteamEcho.App.ViewModels;

namespace SteamEcho.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task InitializeViewModelAsync()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    // Handles hyperlink navigation requests
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    // Handles image load failures for game icons
    private void GameIcon_ImageFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image image)
        {
            image.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/library_placeholder.png"));
        }
    }

    // Handles image load failures for achievement icons
    private void AchievementIcon_ImageFailed(object? sender, ExceptionRoutedEventArgs e) {
        if (sender is Image image) {
            image.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/achievement_unlocked.png"));
        }
    }
}