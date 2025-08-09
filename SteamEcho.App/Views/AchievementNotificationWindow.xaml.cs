using System.Windows;

namespace SteamEcho.App.Views;

/// <summary>
/// Interaction logic for AchievementNotificationWindow.xaml
/// </summary>
public partial class AchievementNotificationWindow : Window
{
    public AchievementNotificationWindow()
    {
        InitializeComponent();
        this.Opacity = 0;
        this.ContentRendered += AchievementNotificationWindow_ContentRendered;
    }

    private void AchievementNotificationWindow_ContentRendered(object? sender, EventArgs e)
    {
        var desktopWorkingArea = SystemParameters.WorkArea;
        this.Left = desktopWorkingArea.Right - this.ActualWidth - 10;
        this.Top = desktopWorkingArea.Bottom - this.ActualHeight / 2;

        this.Opacity = 1;
    }
}