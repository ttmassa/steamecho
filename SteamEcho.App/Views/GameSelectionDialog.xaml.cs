using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SteamEcho.Core.DTOs;

namespace SteamEcho.App.Views;

public partial class GameSelectionDialog : Window
{
    public List<GameInfo> Games { get; }
    public GameInfo? SelectedGame
    {
        get => _selectedGame;
        set => _selectedGame = value;
    }
    private GameInfo? _selectedGame;

    public GameSelectionDialog(List<GameInfo> games)
    {
        InitializeComponent();
        Games = games;
        DataContext = this;
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedGame = GamesListBox.SelectedItem as GameInfo;
        DialogResult = SelectedGame != null;
    }

    // Handles image load failures for game icons
    private void GameIcon_ImageFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image image)
        {
            image.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/library_placeholder.png"));
        }
    }

}