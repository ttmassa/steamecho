using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using SteamEcho.Core.Models;
using SteamEcho.App.Services;
using SteamEcho.App.DTOs;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamEcho.App.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Game> Games { get; } = [];
    public ICommand AddGameCommand { get; }
    public ICommand DeleteGameCommand { get; }
    private readonly SteamService _steamService;
    private Game? _selectedGame;
    public Game? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (_selectedGame != value)
            {
                _selectedGame = value;
                OnPropertyChanged();
            }
        }
    }

    public MainWindowViewModel()
    {
        _steamService = new SteamService();
        AddGameCommand = new RelayCommand(AddGame);
        DeleteGameCommand = new RelayCommand<Game>(DeleteGame);
    }

    private async void AddGame()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executable Files (*.exe)|*.exe",
            Title = "Select Game Executable"
        };

        if (dialog.ShowDialog() == true)
        {
            var fileName = Path.GetFileNameWithoutExtension(dialog.FileName);

            // Use SteamService to resolve Steam ID
            GameInfo? gameInfo = await _steamService.ResolveSteamIdAsync(fileName);
            if (gameInfo == null)
            {
                Console.WriteLine("Error resolving Steam ID. Please check the game name or try again later.");
                return;
            }
            string steamId = gameInfo.SteamId;
            string gameName = gameInfo.Name;
            string iconUrl = gameInfo.IconUrl;

            // Game already exists
            if (Games.Any(g => g.SteamId == steamId))
            {
                Console.WriteLine("Game already exists in the collection.");
                return;
            }

            // Get achievements for the game
            List<Achievement> achievements = await _steamService.GetAchievementsAsync(steamId);

            // Create game instance
            Game game = new(steamId, gameName, dialog.FileName, iconUrl);
            game.AddAchievements(achievements);

            // Add the game to the collection
            Games.Add(game);
            SelectedGame = game;
        }
    }

    private void DeleteGame(Game game)
    {
        Games.Remove(game);
        if (SelectedGame == game)
            SelectedGame = Games.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}