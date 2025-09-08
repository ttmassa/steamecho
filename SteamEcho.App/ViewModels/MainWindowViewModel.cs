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
using System.Diagnostics;
using SteamEcho.App.Views;
using SteamEcho.App.Models;
using System.Windows.Data;

namespace SteamEcho.App.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    // Collections
    public ObservableCollection<Game> Games { get; } = [];
    public ObservableCollection<string> NotificationColors { get; } =
    [
        "#4A4A4D",
        "#000044",
        "#000000",
        "#C02222",
        "#19680b",
        "#BF00FF"
    ];
    private readonly ICollectionView _gamesView;
    public ICollectionView GamesView => _gamesView;

    // Commands
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
    public ICommand TestNotificationCommand { get; }
    public ICommand SaveNotificationSettingsCommand { get; }
    public ICommand PreviousStatPageCommand { get; }
    public ICommand NextStatPageCommand { get; }

    // Services
    private readonly SteamService _steamService;
    private readonly GameProcessService _gameProcessService;
    private readonly AchievementListener _achievementListener;
    private readonly ScreenshotService _screenshotService;
    private StorageService _storageService;
    private NotificationService _notificationService;
    private NotificationConfig? _draftNotificationConfig;

    // Properties
    private Game? _selectedGame;
    public Game? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (_selectedGame != value)
            {
                _selectedGame = value;
                if (_selectedGame != null)
                {
                    CheckProxyStatus(_selectedGame);
                    LoadLocalScreenshots(_selectedGame);
                }
                OnPropertyChanged();
            }
        }
    }
    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                // Refresh the view to re-run the filter
                _gamesView.Refresh();
                OnPropertyChanged();
            }
        }
    }
    private SteamUserInfo? _currentUser;
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
    private bool _isLoadingGames;
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
    private bool _isSettingsVisible;
    public bool IsSettingsVisible
    {
        get => _isSettingsVisible;
        set
        {
            if (_isSettingsVisible != value)
            {
                _isSettingsVisible = value;
                if (_isSettingsVisible)
                {
                    // Create a draft copy when opening settings
                    _draftNotificationConfig = new NotificationConfig
                    {
                        NotificationSize = _notificationService.Config.NotificationSize,
                        NotificationColor = _notificationService.Config.NotificationColor
                    };
                    OnPropertyChanged(nameof(NotificationSize));
                    OnPropertyChanged(nameof(NotificationColor));
                    OnPropertyChanged(nameof(IsNotificationSaved));
                }
                else
                {
                    // Discard draft when closing settings
                    _draftNotificationConfig = null;
                    OnPropertyChanged(nameof(IsNotificationSaved));
                }
                OnPropertyChanged();
            }
        }
    }
    public bool IsUserLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.SteamId);
    public string StatusText => IsUserLoggedIn ? "Connected" : "Disconnected";
    public double NotificationSize
    {
        get => _draftNotificationConfig?.NotificationSize ?? _notificationService.Config.NotificationSize;
        set
        {
            if (_draftNotificationConfig != null && _draftNotificationConfig.NotificationSize != value)
            {
                _draftNotificationConfig.NotificationSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotificationSaved));
            }
        }
    }
    public int NotificationTime
    {
        get => _draftNotificationConfig?.NotificationTime ?? _notificationService.Config.NotificationTime;
        set
        {
            if (_draftNotificationConfig != null && _draftNotificationConfig.NotificationTime != value)
            {
                _draftNotificationConfig.NotificationTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotificationSaved));
            }
        }
    }
    public string NotificationColor
    {
        get => _draftNotificationConfig?.NotificationColor ?? _notificationService.Config.NotificationColor;
        set
        {
            if (_draftNotificationConfig != null && _draftNotificationConfig.NotificationColor != value)
            {
                _draftNotificationConfig.NotificationColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotificationSaved));
            }
        }
    }
    public bool IsNotificationSaved => _draftNotificationConfig != null &&
        (_draftNotificationConfig.NotificationSize != _notificationService.Config.NotificationSize ||
         _draftNotificationConfig.NotificationColor != _notificationService.Config.NotificationColor ||
         _draftNotificationConfig.NotificationTime != _notificationService.Config.NotificationTime);
    private const int TotalStatPages = 4;
    private int _currentStatPage = 0;
    public int CurrentStatPage
    {
        get => _currentStatPage;
        set
        {
            if (_currentStatPage != value)
            {
                _currentStatPage = value;
                OnPropertyChanged();
            }
        }
    }

    public MainWindowViewModel()
    {
        // Initialize services
        _storageService = null!;
        _notificationService = null!;
        _steamService = new SteamService();
        _gameProcessService = new GameProcessService(Games);
        _achievementListener = new AchievementListener();
        _screenshotService = new ScreenshotService();

        // Setup collection view for filtering
        _gamesView = CollectionViewSource.GetDefaultView(Games);
        _gamesView.Filter = FilterGames;

        // Initialize commands
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
        ToggleProxyCommand = new RelayCommand(ToggleProxy);
        TestNotificationCommand = new RelayCommand(TestNotification);
        SaveNotificationSettingsCommand = new RelayCommand(SaveNotificationSettings);
        PreviousStatPageCommand = new RelayCommand(PreviousStatPage);
        NextStatPageCommand = new RelayCommand(NextStatPage);
    }

    // Background initialization during loading screen
    public async Task InitializeAsync()
    {
        await Task.Run(async () =>
        {
            LoadingStatus.Update("Initializing services...");
            // Initialize storage service
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steamecho.db");
            _storageService = new StorageService(dbPath);

            // Initialize notification service (needs to load notification sound)
            _notificationService = new NotificationService();

            LoadingStatus.Update("Loading your data...");
            var user = _storageService.LoadUser();
            var games = _storageService.LoadGames();

            if (user != null)
            {
                LoadingStatus.Update("Syncing with Steam...");
                // Sync with Steam data
                var steamGames = await _steamService.GetOwnedGamesAsync(user);
                _storageService.SyncGames(steamGames, games);

                // Reload games from db after sync
                games = _storageService.LoadGames();
            }

            LoadingStatus.Update("Almost there...");
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentUser = user;
                foreach (var game in games)
                {
                    Games.Add(game);
                }
                SelectedGame = Games.FirstOrDefault();

                // Notify the UI that the NotificationSize property has been loaded
                OnPropertyChanged(nameof(NotificationSize));
                OnPropertyChanged(nameof(NotificationTime));
                OnPropertyChanged(nameof(NotificationColor));

                _gameProcessService.Start();
                _achievementListener.AchievementUnlocked += OnAchievementUnlocked;
                _gameProcessService.RunningGameChanged += OnRunningGameChanged;
                _screenshotService.ScreenshotTaken += OnScreenshotTaken;
            });
        });
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

            // Search for possible games
            List<GameInfo> gamesInfo = await _steamService.SearchSteamGamesAsync(fileName);
            if (gamesInfo == null || gamesInfo.Count == 0)
            {
                MessageBox.Show("No matching games found. Please check the name or try again.", "Game Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show selection dialog
            var selectionDialog = new GameSelectionDialog(gamesInfo) { Owner = Application.Current.MainWindow };
            if (selectionDialog.ShowDialog() != true || selectionDialog.SelectedGame == null)
            {
                // User cancelled
                return;
            }

            var gameInfo = selectionDialog.SelectedGame;
            long steamId = gameInfo.SteamId;
            string gameName = gameInfo.Name;
            string? iconUrl = gameInfo.IconUrl;

            // Game already exists
            if (Games.Any(g => g.SteamId == steamId))
            {
                var errorDialog = new MessageDialog("This game is already in your library.", "Duplicate Game");
                errorDialog.ShowDialog();
                return;
            }

            // Get achievements for the game
            List<Achievement> achievements = await _steamService.GetAchievementsAsync(steamId);

            // Create game instance
            Game game = new(steamId, gameName, achievements, dialog.FileName, iconUrl);

            // Add and save
            Games.Add(game);
            _storageService.SaveGame(game);

            SelectedGame = game;
        }
    }


    private void DeleteGame(Game game)
    {
        if (game == null) return;

        // Ask for confirmation before deleting the game
        var confirmDialog = new ConfirmDialog(
            "Are you sure you want to delete this game? This action cannot be undone.",
            "Confirm Delete"
        );
        var result = confirmDialog.ShowDialog();
        if (result != true) return;

        // Delete achievements json file
        if (!string.IsNullOrEmpty(game.ExecutablePath))
        {
            var gameDir = Path.GetDirectoryName(game.ExecutablePath);
            if (gameDir != null)
            {
                var jsonFilePath = Path.Combine(gameDir, "achievement_notifications.json");
                if (File.Exists(jsonFilePath))
                {
                    File.Delete(jsonFilePath);
                }
            }
        }

        // Remove game from collection and database
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

                    // Show notification
                    _notificationService.ShowNotification(achievement);

                    // Update the game in the database
                    _storageService.UpdateAchievement(game.SteamId, achievement.Id, true, achievement.UnlockDate);

                    break;
                }
            }
            if (!achievementFound)
            {
                return;
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
            if (!game.IsProxyReady)
            {
                var dialog = new ConfirmDialog(
                    "Proxy is not set up for this game, SteamEcho won't be able to track achievements. Please set it up by clicking on the 'Setup' button.",
                    "Warning"
                );
                dialog.ShowDialog();
                return;
            }

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

    private async void LogOutFromSteam()
    {
        if (IsUserLoggedIn && CurrentUser != null)
        {
            // Ask for confirmation before logging out
            var messageBoxText = "Are you sure you want to logout from Steam? You'll lose all data related to your Steam account.";
            var caption = "Confirm Logout";

            var dialog = new ConfirmDialog(messageBoxText, caption);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                IsLoadingGames = true;

                try
                {
                    // Get steam owned games and remove them from the collection and database
                    var steamOwnedGames = await _steamService.GetOwnedGamesAsync(CurrentUser);
                    var steamIds = steamOwnedGames.Select(g => g.SteamId).ToList();
                    _storageService.DeleteGamesByIds(steamIds);

                    // Remove deleted games from the ObservableCollection
                    foreach (var id in steamIds)
                    {
                        var gameToRemove = Games.FirstOrDefault(g => g.SteamId == id);
                        if (gameToRemove != null)
                            Games.Remove(gameToRemove);
                    }

                    // Remove user from the database
                    _storageService.DeleteUser(CurrentUser.SteamId);
                    CurrentUser = null;
                }
                finally
                {
                    IsLoadingGames = false;
                }
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
            _screenshotService.StartMonitoring(CurrentUser, runningGame);
        }
        else
        {
            // Stop achievement listener if no game is running or executable path is invalid
            _achievementListener.Stop();
            _screenshotService.StopMonitoring();
        }
    }

    private void ToggleProxy()
    {
        if (SelectedGame == null) return;

        if (string.IsNullOrEmpty(SelectedGame.ExecutablePath) || !File.Exists(SelectedGame.ExecutablePath))
        {
            var dialog = new MessageDialog(
                "Executable path not set for this game. Please set it by right-clicking on the game in the library and selecting 'Set Executable'.",
                "Error"
            );
            dialog.ShowDialog();
            return;
        }

        var gameDirectory = Path.GetDirectoryName(SelectedGame.ExecutablePath);
        if (gameDirectory == null || !Directory.Exists(gameDirectory))
        {
            var dialog = new MessageDialog(
                "Game directory does not exist. Please set a valid executable path.",
                "Error"
            );
            dialog.ShowDialog();
            return;
        }

        if (SelectedGame.IsProxyReady)
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

                SelectedGame.IsProxyReady = false;
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
                    SelectedGame.IsProxyReady = true;
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

        // Search all subdirectories for proxy DLL pairs
        bool foundProxy = false;
        foreach (var dir in Directory.GetDirectories(gameDirectory, "*", SearchOption.AllDirectories).Prepend(gameDirectory))
        {
            bool isProxyReady32 = File.Exists(Path.Combine(dir, "steam_api_o.dll")) && File.Exists(Path.Combine(dir, "steam_api.dll")) && File.Exists(Path.Combine(dir, "SmokeAPI.config.json"));
            bool isProxyReady64 = File.Exists(Path.Combine(dir, "steam_api64_o.dll")) && File.Exists(Path.Combine(dir, "steam_api64.dll")) && File.Exists(Path.Combine(dir, "SmokeAPI.config.json"));
            if (isProxyReady32 || isProxyReady64)
            {
                foundProxy = true;
                break;
            }
        }

        game.IsProxyReady = foundProxy;
    }

    private void LoadLocalScreenshots(Game game)
    {
        var screenshotDir = ScreenshotService.GetScreenshotDirectory(CurrentUser, game);
        if (screenshotDir == null || !Directory.Exists(screenshotDir)) return;

        var existingScreenshots = game.Screenshots.Select(s => s.FilePath).ToHashSet();

        var files = Directory.GetFiles(screenshotDir, "*.jpg");
        foreach (var file in files)
        {
            if (!existingScreenshots.Contains(file))
            {
                game.Screenshots.Add(new Screenshot(file));
            }
        }
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

    private void TestNotification()
    {
        // Create dummy achievement for testing
        _notificationService.TestNotification(NotificationSize, NotificationColor);
    }

    private void SaveNotificationSettings()
    {
        if (_draftNotificationConfig != null)
        {
            _notificationService.Config.NotificationSize = _draftNotificationConfig.NotificationSize;
            _notificationService.Config.NotificationTime = _draftNotificationConfig.NotificationTime;
            _notificationService.Config.NotificationColor = _draftNotificationConfig.NotificationColor;
            _notificationService.SaveConfig();

            // After saving, update the draft to match the saved config
            _draftNotificationConfig.NotificationSize = _notificationService.Config.NotificationSize;
            _draftNotificationConfig.NotificationTime = _notificationService.Config.NotificationTime;
            _draftNotificationConfig.NotificationColor = _notificationService.Config.NotificationColor;

            OnPropertyChanged(nameof(NotificationSize));
            OnPropertyChanged(nameof(NotificationTime));
            OnPropertyChanged(nameof(NotificationColor));
            OnPropertyChanged(nameof(IsNotificationSaved));

            var dialog = new MessageDialog("Notification settings saved successfully.", "Success");
            dialog.ShowDialog();
         }
    }

    private bool FilterGames(object? obj)
    {
        if (obj is not Game game) return false;

        var query = _searchText?.Trim().ToLower();
        if (string.IsNullOrEmpty(query)) return true;

        // Match by name
        return game.Name?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void PreviousStatPage()
    {
        CurrentStatPage = (CurrentStatPage - 1 + TotalStatPages) % TotalStatPages;
    }

    private void NextStatPage()
    {
        CurrentStatPage = (CurrentStatPage + 1) % TotalStatPages;
    }

    private void OnScreenshotTaken(Screenshot screenshot)
    {
        var runningGame = _gameProcessService.GetRunningGame();
        if (runningGame == null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            runningGame.Screenshots.Add(screenshot);
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}