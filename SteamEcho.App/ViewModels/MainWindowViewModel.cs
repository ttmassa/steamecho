using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using SteamEcho.Core.Models;
using SteamEcho.App.Services;
using System.Text.Json;
using SteamEcho.App.DTOs;

namespace SteamEcho.App.ViewModels;

public class MainWindowViewModel
{
    public ObservableCollection<Game> Games { get; } = [];
    public ICommand AddGameCommand { get; }
    private readonly SteamService _steamService;

    public MainWindowViewModel()
    {
        _steamService = new SteamService();
        AddGameCommand = new RelayCommand(AddGame);
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
            GameInfo gameInfo = await _steamService.ResolveSteamIdAsync(fileName);
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
        }
    }
}