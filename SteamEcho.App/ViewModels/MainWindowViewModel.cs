using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using SteamEcho.Core.Models;
using SteamEcho.App.Services;
using System.Text.Json;

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
            string? searchResult = await _steamService.ResolveSteamIdAsync(fileName);
            if (string.IsNullOrEmpty(searchResult))
            {
                Console.WriteLine("Failed to resolve Steam ID for the game.");
                return;
            }

            // Get first result from search
            using var doc = JsonDocument.Parse(searchResult);
            var items = doc.RootElement.GetProperty("items");
            if (items.GetArrayLength() == 0)
            {
                Console.WriteLine("No Steam game with that name exists.");
                return;   
            }

            // Get steam ID from the first game
            var firstGame = items[0];
            string steamId = firstGame.GetProperty("id").GetInt32().ToString() ?? throw new InvalidDataException("Steam ID not found in search result.");
            string gameName = firstGame.GetProperty("name").GetString() ?? "Unknown Name";

            // Game already exists
            if (Games.Any(g => g.SteamId == steamId))
            {
                Console.WriteLine("Game already exists in the collection.");
                return;
            }

            // Get achievements for the game
            List<Achievement> achievements = await _steamService.GetAchievementsAsync(steamId);

            // Create game instance
            Game game = new(steamId, gameName, dialog.FileName);
            game.AddAchievements(achievements);

            // Add the game to the collection
            Games.Add(game);
        }
    }
}