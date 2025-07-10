using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using SteamEcho.Core.Models;
using SteamEcho.App.Services;
using SteamEcho.Core.DTOs;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Media;
using System.Diagnostics;
using SteamEcho.App.Views;

namespace SteamEcho.App.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Game> Games { get; } = [];
    public ICommand AddGameCommand { get; }
    public ICommand DeleteGameCommand { get; }
    public ICommand BrowseFilesCommand { get; }
    public ICommand LockAchievementCommand { get; }
    public ICommand UnlockAchievementCommand { get; }
    public ICommand TogglePlayStateCommand { get; }
    public ICommand LogToSteamCommand { get; }
    public ICommand LogOutFromSteamCommand { get; }
    private readonly SteamService _steamService;
    private readonly StorageService _storageService;
    private readonly AchievementListenerService _achievementListenerService;
    private readonly SoundPlayer _soundPlayer;
    private readonly NotificationService _notificationService;
    private readonly GameProcessService _gameProcessService;
    private Game? _selectedGame;
    private SteamUserInfo? _currentUser;
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
    public SteamUserInfo? CurrentUser
    {
        get => _currentUser;
        set
        {
            if (_currentUser != value)
            {
                _currentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUserLoggedIn));
            }
        }
    }
    public bool IsUserLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.SteamId);

    public MainWindowViewModel()
    {
        _steamService = new SteamService();
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steamecho.db");
        _storageService = new StorageService(dbPath);
        _storageService.InitializeDatabase();
        _achievementListenerService = new AchievementListenerService();
        _achievementListenerService.AchievementUnlocked += OnAchievementUnlocked;
        _achievementListenerService.StartListening();
        _soundPlayer = new SoundPlayer("Assets/Sound/notification.wav");
        _notificationService = new NotificationService();

        CurrentUser = _storageService.LoadUser();
        Games = new ObservableCollection<Game>(_storageService.LoadGames());
        SelectedGame = Games.FirstOrDefault();

        // Initialize game process service
        _gameProcessService = new GameProcessService(Games);
        _gameProcessService.Start();

        AddGameCommand = new RelayCommand(AddGame);
        DeleteGameCommand = new RelayCommand<Game>(DeleteGame);
        BrowseFilesCommand = new RelayCommand<Game>(BrowseFiles);
        LockAchievementCommand = new RelayCommand<Achievement>(LockAchievement);
        UnlockAchievementCommand = new RelayCommand<Achievement>(UnlockAchievement);
        TogglePlayStateCommand = new RelayCommand<Game>(TogglePlayState);
        LogToSteamCommand = new RelayCommand(LogToSteam);
        LogOutFromSteamCommand = new RelayCommand(LogOutFromSteam);
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
            string? iconUrl = gameInfo.IconUrl;

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

    private void BrowseFiles(Game game)
    {
        if (game == null || string.IsNullOrEmpty(game.ExecutablePath))
        {
            Console.WriteLine("Selected game is null or does not have an executable path.");
            return;
        }

        try
        {
            string? directoryPath = Path.GetDirectoryName(game.ExecutablePath);
            if (directoryPath != null && Directory.Exists(directoryPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = directoryPath,
                    UseShellExecute = true
                });
            }
            else
            {
                Console.WriteLine("Directory does not exist: " + directoryPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error opening file explorer: " + ex.Message);
        }
    }

    private void LockAchievement(Achievement achievement)
    {
        if (achievement != null && achievement.IsUnlocked && SelectedGame != null)
        {
            achievement.IsUnlocked = false;
            achievement.UnlockDate = null;
            _storageService.UpdateAchievement(long.Parse(SelectedGame.SteamId), achievement.Id, false, null);
        }
    }

    private void UnlockAchievement(Achievement achievement)
    {
        if (achievement != null && !achievement.IsUnlocked && SelectedGame != null)
        {
            achievement.Unlock();
            _storageService.UpdateAchievement(long.Parse(SelectedGame.SteamId), achievement.Id, true, achievement.UnlockDate);
        }
    }

    private void OnAchievementUnlocked(string achievementApiName)
    {
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

                    // Play notification sound
                    _soundPlayer.Play();

                    // Show on-screen notification
                    _notificationService.ShowNotification(achievement);

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

    private void TogglePlayState(Game game)
    {
        if (game == null) return;

        if (game.IsRunning)
        {
            try
            {
                var process = Process.GetProcesses().FirstOrDefault(p => !p.HasExited && string.Equals(p.MainModule?.FileName, game.ExecutablePath, StringComparison.OrdinalIgnoreCase));
                process?.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping game: {ex.Message}");
            }
        }
        else
        {
            try
            {
                Process.Start(new ProcessStartInfo(game.ExecutablePath)
                {
                    WorkingDirectory = Path.GetDirectoryName(game.ExecutablePath),
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting game: {ex.Message}");
            }
        }
    }

    private async void LogToSteam()
    {
        var userInfo = await _steamService.LogToSteamAsync();
        if (userInfo != null && !string.IsNullOrEmpty(userInfo.SteamId))
        {
            _storageService.SaveUser(userInfo);
            CurrentUser = userInfo;
        }
    }

    private void LogOutFromSteam()
    {
        if (IsUserLoggedIn)
        {
            var messageBoxText = "Are you sure you want to logout from Steam? You'll lose all data related to your Steam account.";
            var caption = "Confirm Logout";

            var dialog = new ConfirmDialog(messageBoxText, caption);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                _storageService.DeleteUser();
                CurrentUser = null;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}