using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using SteamEcho.App.ViewModels;

namespace SteamEcho.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    // Handles hyperlink navigation requests
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}