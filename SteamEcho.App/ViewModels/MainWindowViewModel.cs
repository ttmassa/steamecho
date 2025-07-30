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
    public ICommand ShowSettingsCommand { get; }
    public ICommand HideSettingsCommand { get; }
    public ICommand RefreshDataCommand { get; }
    public ICommand SetExecutableCommand { get; }
    public ICommand ToggleProxyCommand { get; }
    private readonly SteamService _steamService;
    private readonly StorageService _storageService;
    private readonly SoundPlayer _soundPlayer;
    private readonly NotificationService _notificationService;
    private readonly GameProcessService _gameProcessService;
    private readonly AchievementListener _achievementListener;
    private Game? _selectedGame;
    private SteamUserInfo? _currentUser;
    private bool _isLoadingGames;
    private bool _isSettingsVisible;
    private string? _steamApiKey;

    public Game? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (_selectedGame != value)
            {
                _selectedGame = value;
                OnPropertyChanged();
                if (_selectedGame != null)
                {
                    CheckProxyStatus(_selectedGame);
                }
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
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }
    public bool IsUserLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.SteamId);

    public bool IsLoadingGames
    {
        get => _isLoadingGames;
        set
        {
            if (_isLoadingGames != value)
            {
                _isLoadingGames = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSettingsVisible
    {
        get => _isSettingsVisible;
        set
        {
            if (_isSettingsVisible != value)
            {
                _isSettingsVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public string? SteamApiKey
    {
        get => _steamApiKey;
        set
        {
            if (_steamApiKey != value)
            {
                _steamApiKey = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusText => IsUserLoggedIn ? "Connected" : "Disconnected";

    public MainWindowViewModel()
    {
        _steamService = new SteamService();
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steamecho.db");
        _storageService = new StorageService(dbPath);
        _storageService.InitializeDatabase();
        _soundPlayer = new SoundPlayer("Assets/Sound/notification.wav");
        _notificationService = new NotificationService();

        CurrentUser = _storageService.LoadUser();
        Games = new ObservableCollection<Game>(_storageService.LoadGames());
        SelectedGame = Games.FirstOrDefault();

        // Initialize game process service
        _gameProcessService = new GameProcessService(Games);
        _gameProcessService.RunningGameChanged += OnRunningGameChanged;
        _gameProcessService.Start();

        // Initialize achievement listener
        _achievementListener = new AchievementListener();
        _achievementListener.AchievementUnlocked += OnAchievementUnlocked;

        AddGameCommand = new RelayCommand(AddGame);
        DeleteGameCommand = new RelayCommand<Game>(DeleteGame);
        BrowseFilesCommand = new RelayCommand<Game>(BrowseFiles);
        LockAchievementCommand = new RelayCommand<Achievement>(LockAchievement);
        UnlockAchievementCommand = new RelayCommand<Achievement>(UnlockAchievement);
        TogglePlayStateCommand = new RelayCommand<Game>(TogglePlayState);
        LogToSteamCommand = new RelayCommand(LogToSteam);
        LogOutFromSteamCommand = new RelayCommand(LogOutFromSteam);
        ShowSettingsCommand = new RelayCommand(() => IsSettingsVisible = true);
        HideSettingsCommand = new RelayCommand(() => IsSettingsVisible = false);
        RefreshDataCommand = new RelayCommand(RefreshData);
        SetExecutableCommand = new RelayCommand<Game>(SetExecutable);
        ToggleProxyCommand = new RelayCommand<Game>(ToggleProxy);
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
            long steamId = gameInfo.SteamId;
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
            Game game = new(steamId, gameName, achievements, dialog.FileName, iconUrl);

            // Add the game to the collection and save to database
            Games.Add(game);
            _storageService.SaveGame(game);

            SelectedGame = game;
        }
    }

    private void DeleteGame(Game game)
    {
        Games.Remove(game);
        _storageService.DeleteGame(game.SteamId);
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
            _storageService.UpdateAchievement(SelectedGame.SteamId, achievement.Id, false, null);
        }
    }

    private void UnlockAchievement(Achievement achievement)
    {
        if (achievement != null && !achievement.IsUnlocked && SelectedGame != null)
        {
            achievement.Unlock();
            _storageService.UpdateAchievement(SelectedGame.SteamId, achievement.Id, true, achievement.UnlockDate);
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
                    _storageService.UpdateAchievement(game.SteamId, achievement.Id, true, achievement.UnlockDate);

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

            IsLoadingGames = true;
            try
            {
                // Get potentiel owned games and store them
                List<Game> ownedGames = await _steamService.GetOwnedGamesAsync(userInfo);
                _storageService.SaveGames(ownedGames);
                foreach (var game in ownedGames)
                {
                    if (!Games.Any(g => g.SteamId == game.SteamId))
                    {
                        Games.Add(game);
                    }
                }
            }
            finally
            {
                IsLoadingGames = false;
            }
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
                SteamApiKey = null;
            }
        }
    }

    private async void RefreshData()
    {
        if (CurrentUser == null) return;

        IsLoadingGames = true;
        try
        {
            // Get potentiel new owned games and store them
            List<Game> ownedGames = await _steamService.GetOwnedGamesAsync(CurrentUser);
            _storageService.SaveGames(ownedGames);
            foreach (var game in ownedGames)
            {
                if (!Games.Any(g => g.SteamId == game.SteamId))
                {
                    Games.Add(game);
                }
            }
        }
        finally
        {
            IsLoadingGames = false;
        }
    }

    private void SetExecutable(Game game)
    {
        if (game == null) return;

        var dialog = new OpenFileDialog
        {
            Filter = "Executable Files (*.exe)|*.exe",
            Title = "Select Game Executable",
            InitialDirectory = Path.GetDirectoryName(game.ExecutablePath)
        };

        if (dialog.ShowDialog() == true)
        {
            game.ExecutablePath = dialog.FileName;
            _storageService.UpdateGameExecutable(game.SteamId, game.ExecutablePath);
            OnPropertyChanged(nameof(Games));
        }
    }

    private void OnRunningGameChanged(Game? runningGame)
    {
        if (runningGame != null && !string.IsNullOrEmpty(runningGame.ExecutablePath) && File.Exists(runningGame.ExecutablePath))
        {
            // Start achievement listener for the running game
            _achievementListener.Start(runningGame.ExecutablePath);
        }
        else
        {
            // Stop achievement listener if no game is running or executable path is invalid
            _achievementListener.Stop();
        }
    }

    private void ToggleProxy(Game game)
    {
        if (game == null) return;

        if (string.IsNullOrEmpty(game.ExecutablePath) || !File.Exists(game.ExecutablePath))
        {
            var dialog = new MessageDialog(
                "Executable path not set for this game. Please set it by right-clicking on the game in the library and selecting 'Set Executable'.",
                "Error"
            );
            dialog.ShowDialog();
            return;
        }

        var gameDirectory = Path.GetDirectoryName(game.ExecutablePath);
        if (gameDirectory == null || !Directory.Exists(gameDirectory))
        {
            var dialog = new MessageDialog(
                "Game directory does not exist. Please set a valid executable path.",
                "Error"
            );
            dialog.ShowDialog();
            return;
        }

        if (game.IsProxyReady)
        {
            // Ask for confirmation before disabling the proxy
            var confirmDialog = new ConfirmDialog(
                "Disabling the proxy will remove achievement tracking for this game until you set it up again. Are you sure you want to continue?",
                "Disabling Proxy"
            );
            var result = confirmDialog.ShowDialog();
            if (result != true) return;

            // Disable the proxy
            try
            {
                UnprocessSteamApiDll(gameDirectory, "x86");
                UnprocessSteamApiDll(gameDirectory, "x64");

                game.IsProxyReady = false;
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(
                    $"An error occurred while uninstalling the proxy: {ex.Message}",
                    "Error"
                );
                dialog.ShowDialog();
            }
        }
        else
        {
            // Setup the proxy
            try
            {
                bool setupDone = false;
                setupDone |= ProcessSteamApiDll(gameDirectory, "x86");
                setupDone |= ProcessSteamApiDll(gameDirectory, "x64");

                if (setupDone)
                {
                    game.IsProxyReady = true;
                }
                else
                {
                    var dialog = new MessageDialog(
                        "Proxy setup failed. Please make sure the game directory contains a valid steam_api.dll or steam_api64.dll file.",
                        "Setup Failed"
                    );
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(
                    $"An error occurred while setting up the proxy: {ex.Message}",
                    "Error"
                );
                dialog.ShowDialog();
            }
        }
    }

    private static void CheckProxyStatus(Game game)
    {
        if (string.IsNullOrEmpty(game.ExecutablePath) || !File.Exists(game.ExecutablePath))
        {
            game.IsProxyReady = false;
            return;
        }

        var gameDirectory = Path.GetDirectoryName(game.ExecutablePath);
        if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory))
        {
            game.IsProxyReady = false;
            return;
        }

        // Check if either the 32-bit or 64-bit proxy is correctly set up.
        bool isProxyReady32 = File.Exists(Path.Combine(gameDirectory, "steam_api_o.dll")) && File.Exists(Path.Combine(gameDirectory, "steam_api.dll"));
        bool isProxyReady64 = File.Exists(Path.Combine(gameDirectory, "steam_api64_o.dll")) && File.Exists(Path.Combine(gameDirectory, "steam_api64.dll"));

        game.IsProxyReady = isProxyReady32 || isProxyReady64;
    }

    private static bool ProcessSteamApiDll(string gameDirectory, string bitness) {
        if (bitness != "x86" && bitness != "x64")
        {
            throw new ArgumentException("Bitness must be either 'x86' or 'x64'.");
        }

        string dllName = bitness == "x86" ? "steam_api.dll" : "steam_api64.dll";
        string renamedDllName = bitness == "x86" ? "steam_api_o.dll" : "steam_api64_o.dll";
        
        // Look for both original and already-renamed DLLs
        var originalDllPaths = Directory.GetFiles(gameDirectory, dllName, SearchOption.AllDirectories);
        var renamedDllPaths = Directory.GetFiles(gameDirectory, renamedDllName, SearchOption.AllDirectories);

        // Combine all unique directories where DLLs were found
        var directoriesToProcess = originalDllPaths.Concat(renamedDllPaths)
            .Select(Path.GetDirectoryName)
            .Where(d => d != null)
            .Distinct()
            .ToList();

        if (directoriesToProcess.Count == 0) return false;

        foreach (var dllDirectory in directoriesToProcess)
        {
            var originalDllPath = Path.Combine(dllDirectory!, dllName);
            var renamedDllPath = Path.Combine(dllDirectory!, renamedDllName);

            // Rename original dll
            if (File.Exists(originalDllPath) && !File.Exists(renamedDllPath))
            {
                File.Move(originalDllPath, renamedDllPath);
            }

            // Copy proxy dll
            string proxyDllSourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ThirdParty", "SmokeAPI", bitness, dllName);
            if (File.Exists(proxyDllSourcePath) && !File.Exists(originalDllPath))
            {
                File.Copy(proxyDllSourcePath, originalDllPath);
            }

            // Copy config file
            string configSourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ThirdParty", "SmokeAPI", "SmokeAPI.config.json");
            string configDestPath = Path.Combine(dllDirectory!, "SmokeAPI.config.json");
            if (File.Exists(configSourcePath) && !File.Exists(configDestPath))
            {
                File.Copy(configSourcePath, configDestPath);
            }
        }
        return true;
    }

    private static void UnprocessSteamApiDll(string gameDirectory, string bitness)
    {
        if (bitness != "x86" && bitness != "x64")
        {
            throw new ArgumentException("Bitness must be either 'x86' or 'x64'.");
        }

        string dllName = bitness == "x86" ? "steam_api.dll" : "steam_api64.dll";
        string renamedDllName = bitness == "x86" ? "steam_api_o.dll" : "steam_api64_o.dll";

        // Find all directories where the original DLL was renamed.
        var renamedDllPaths = Directory.GetFiles(gameDirectory, renamedDllName, SearchOption.AllDirectories);

        foreach (var renamedDllPath in renamedDllPaths)
        {
            var dllDirectory = Path.GetDirectoryName(renamedDllPath);
            if (dllDirectory == null) continue;

            var originalDllPath = Path.Combine(dllDirectory, dllName);
            var configPath = Path.Combine(dllDirectory, "SmokeAPI.config.json");

            // Delete the proxy DLL (which has the original name)
            if (File.Exists(originalDllPath))
            {
                File.Delete(originalDllPath);
            }

            // Delete the config file
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }

            // Rename the original DLL back
            if (File.Exists(renamedDllPath))
            {
                File.Move(renamedDllPath, originalDllPath);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}