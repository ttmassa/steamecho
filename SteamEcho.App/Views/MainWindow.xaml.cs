using System.Windows;
using SteamEcho.App.ViewModels;

namespace SteamEcho.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}