using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using SteamEcho.Core.Models;
using SteamEcho.App.Services;
using SteamEcho.Core.DTOs;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SteamEcho.Core.Services;
using System.Windows;

namespace SteamEcho.App.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Game> Games { get; } = [];
    public ICommand AddGameCommand { get; }
    public ICommand DeleteGameCommand { get; }
    private readonly ISteamService _steamService;
    private readonly StorageService _storageService;
    private readonly AchievementListenerService _achievementListenerService;
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
        _storageService = new StorageService(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SteamEcho\\steam_echo.db");
        _achievementListenerService = new AchievementListenerService();
        AddGameCommand = new RelayCommand(AddGame);
        DeleteGameCommand = new RelayCommand<Game>(DeleteGame);

        // Load games from the database
        List<Game> gamesFromDb = _storageService.LoadGames();
        foreach (var game in gamesFromDb)
        {
            Games.Add(game);
        }

        // Set the first game as selected if available
        SelectedGame = Games.FirstOrDefault();

        // Start the achievement listener
        _achievementListenerService.AchievementUnlocked += OnAchievementUnlocked;
        _achievementListenerService.StartListening();
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

            // Create game instance using the correct constructor
            Game game = new(steamId, gameName, dialog.FileName, achievements, iconUrl);

            // Add the game to the collection and save to database
            Games.Add(game);
            _storageService.SaveGame(long.Parse(steamId), gameName, dialog.FileName, achievements, iconUrl);
            SelectedGame = game;
        }
    }

    private void DeleteGame(Game game)
    {
        Games.Remove(game);
        _storageService.DeleteGame(long.Parse(game.SteamId));
        if (SelectedGame == game)
        {
            SelectedGame = Games.FirstOrDefault();
        }
    }

    private void OnAchievementUnlocked(string achievementApiName)
    {
        Console.WriteLine($"OnAchievementUnlocked event handler triggered with: {achievementApiName}");
        Application.Current.Dispatcher.Invoke(() =>
        {
            bool achievementFound = false;
            foreach (var game in Games)
            {
                var achievement = game.GetAchievementById(achievementApiName);
                if (achievement != null && !achievement.IsUnlocked)
                {
                    achievementFound = true;
                    achievement.Unlock();

                    Console.WriteLine($"Achievement '{achievement.Name}' unlocked for game '{game.Name}' at {achievement.UnlockDate}");
                    // Update the game in the database
                    _storageService.UpdateAchievement(long.Parse(game.SteamId), achievement.Id, true, achievement.UnlockDate);

                    break;
                }
            }
            if (!achievementFound)
            {
                Console.WriteLine($"Received achievement '{achievementApiName}', but it was not found in any loaded game or was already unlocked.");
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}