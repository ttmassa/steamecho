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
using System.Globalization;
using SteamEcho.Core.Services;

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
    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = [
        new LanguageOption { DisplayName = "English", CultureName = "en-US", SteamCode = "english", FlagPath = "/SteamEcho.App;component/Assets/Images/us_flag_icon.png"},
        new LanguageOption { DisplayName = "Français", CultureName = "fr-FR", SteamCode = "french", FlagPath = "/SteamEcho.App;component/Assets/Images/french_flag_icon.png"}
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
    public ICommand PreviousScreenshotCommand { get; }
    public ICommand NextScreenshotCommand { get; }
    public ICommand ViewScreenshotCommand { get; }
    public ICommand DismissUINotificationCommand { get; }

    // Services
    private readonly ISteamService _steamService;
    private readonly IGameProcessService _gameProcessService;
    private readonly IAchievementListener _achievementListener;
    private readonly IScreenshotService _screenshotService;
    private readonly IUINotificationService _uiNotificationService;
    private readonly IProxyService _proxyService;
    private readonly IInternetService _internetService;
    private readonly IStorageService _storageService;
    private readonly INotificationService _notificationService;
    private NotificationConfig? _draftNotificationConfig;
    public static LocalizationService Loc => LocalizationService.Instance;

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
                    _proxyService.CheckProxyStatus(_selectedGame);
                    LoadLocalScreenshots(_selectedGame);
                }
                OnPropertyChanged();
                CurrentScreenshotIndex = 0;
                OnPropertyChanged(nameof(CurrentScreenshot));
                OnPropertyChanged(nameof(ScreenshotCounterText));
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

                // Ensure a game is selected when the filtered list has items.
                var visible = _gamesView.Cast<Game>().ToList();
                var firstVisible = visible.FirstOrDefault();
                if (firstVisible == null)
                {
                    // No match -> clear selection
                    SelectedGame = null;
                }
                else
                {
                    // If current selection is not among visible items, set to first visible.
                    if (_selectedGame == null || !visible.Contains(_selectedGame))
                    {
                        SelectedGame = firstVisible;
                    }
                }
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
                OnPropertyChanged(nameof(LoginText));
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
    public string StatusText => IsUserLoggedIn ? Resources.Resources.ConnectedText : Resources.Resources.DisconnectedText;
    public string LoginText => IsUserLoggedIn ? Resources.Resources.LogoutText : Resources.Resources.LoginText;
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
    private int _currentScreenshotIndex = 0;
    public int CurrentScreenshotIndex
    {
        get => _currentScreenshotIndex;
        set
        {
            if (_currentScreenshotIndex != value)
            {
                _currentScreenshotIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentScreenshot));
                OnPropertyChanged(nameof(ScreenshotCounterText));
            }
        }
    }
    public Screenshot? CurrentScreenshot => SelectedGame?.Screenshots.Count > 0 && CurrentScreenshotIndex >= 0 && CurrentScreenshotIndex < SelectedGame.Screenshots.Count
        ? SelectedGame.Screenshots[CurrentScreenshotIndex]
        : null;
    public string ScreenshotCounterText => SelectedGame?.Screenshots.Count > 0
        ? $"{CurrentScreenshotIndex + 1} / {SelectedGame.Screenshots.Count}"
        : "0 / 0";
    private LanguageOption? _selectedLanguage;
    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value)
            {
                _selectedLanguage = value;
                OnPropertyChanged();
                if (value != null)
                {
                    // Propagate to Steam Service for data language
                    _steamService.ApiLanguage = value.SteamCode;
                    ApplyLanguage(value.CultureName);
                    if (!_isInitializing)
                    {
                        RefreshLanguage();
                    }
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(LoginText));
                }
            }
        }
    }
    public bool IsUINotificationVisible => _uiNotificationService.IsUINotificationVisible;
    public string? UINotificationMessage => _uiNotificationService.UINotificationMessage;
    private bool _hasInternet = true;
    public bool HasInternet
    {
        get => _hasInternet;
        set
        {
            if (_hasInternet != value)
            {
                _hasInternet = value;
                OnPropertyChanged();
            }
        }
    }
    private bool _isInitializing = false;

    public MainWindowViewModel(
        ISteamService steamService,
        IStorageService storageService,
        INotificationService notificationService,
        IGameProcessService gameProcessService,
        IAchievementListener achievementListener,
        IScreenshotService screenshotService,
        IUINotificationService uiNotificationService,
        IProxyService proxyService,
        IInternetService internetService)
    {
        // Assign injected services
        _steamService = steamService;
        _storageService = storageService;
        _notificationService = notificationService;
        _gameProcessService = gameProcessService;
        _achievementListener = achievementListener;
        _screenshotService = screenshotService;
        _uiNotificationService = uiNotificationService;
        _proxyService = proxyService;
        _internetService = internetService;

        // Pass the games collection to the service
        _gameProcessService.SetGamesCollection(Games);

        // Setup collection view for filtering
        _gamesView = CollectionViewSource.GetDefaultView(Games);
        _gamesView.Filter = FilterGames;
        // Sort games alphabetically
        _gamesView.SortDescriptions.Add(new SortDescription(nameof(Game.Name), ListSortDirection.Ascending));

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
        ToggleProxyCommand = new RelayCommand<Game>(_proxyService.ToggleProxy);
        TestNotificationCommand = new RelayCommand(TestNotification);
        SaveNotificationSettingsCommand = new RelayCommand(SaveNotificationSettings);
        PreviousStatPageCommand = new RelayCommand(PreviousStatPage);
        NextStatPageCommand = new RelayCommand(NextStatPage);
        PreviousScreenshotCommand = new RelayCommand(PreviousScreenshot);
        NextScreenshotCommand = new RelayCommand(NextScreenshot);
        ViewScreenshotCommand = new RelayCommand<Screenshot>(ViewScreenshot);
        DismissUINotificationCommand = new RelayCommand(_uiNotificationService.DismissUINotification);
    }

    // Background initialization during loading screen
    public async Task InitializeAsync()
    {
        _isInitializing = true;

        await Application.Current.Dispatcher.InvokeAsync(() =>
            LoadingStatus.Update(Resources.Resources.LoadingStatusInitialization));

        // Storage and notification services are already initialized via DI

        // Check internet connection
        await Application.Current.Dispatcher.InvokeAsync(() =>
            LoadingStatus.Update(Resources.Resources.LoadingStatusInternet));
        HasInternet = await _internetService.CheckInternetConnectivityAsync();

        // Load data from db
        await Application.Current.Dispatcher.InvokeAsync(() =>
            LoadingStatus.Update(Resources.Resources.LoadingStatusData));
        var user = _storageService.LoadUser();
        var games = _storageService.LoadGames();
        var cultureCode = _storageService.LoadLanguage();

        // Set language
        await Application.Current.Dispatcher.InvokeAsync(() =>
            LoadingStatus.Update(Resources.Resources.LoadingStatusSetup));
        SelectedLanguage = AvailableLanguages.FirstOrDefault(lang => lang.CultureName == cultureCode)
                        ?? AvailableLanguages.First(lang => lang.CultureName == "en-US");
        _steamService.ApiLanguage = SelectedLanguage.SteamCode;

        if (user != null && HasInternet)
        {
            // Sync with Steam data
            await Application.Current.Dispatcher.InvokeAsync(() =>
                LoadingStatus.Update(Resources.Resources.LoadingStatusSteam));
            var steamGames = await _steamService.GetOwnedGamesAsync(user);
            _storageService.SyncGames(steamGames, games);

            // Reload games from db after sync
            games = _storageService.LoadGames();
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
            LoadingStatus.Update(Resources.Resources.LoadingStatusFinalization));

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

        // Start timers
        _gameProcessService.StartMonitoring();
        _internetService.StartMonitoring();
        // Subscriptions
        _achievementListener.AchievementUnlocked += OnAchievementUnlocked;
        _gameProcessService.RunningGameChanged += OnRunningGameChanged;
        _screenshotService.ScreenshotTaken += OnScreenshotTaken;
        _uiNotificationService.PropertyChanged += OnUINotificationServicePropertyChanged;
        _internetService.InternetStatusChanged += OnInternetStatusChanged;

        _isInitializing = false;
    }
    
    #region Command Methods
    private async void AddGame()
    {
        if (!HasInternet) return;

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
                var errorDialog = new MessageDialog(Resources.Resources.ErrorDuplicateGameMessage, Resources.Resources.ErrorDuplicateGameTitle);
                errorDialog.ShowDialog();
                return;
            }

            // Get achievements for the game
            List<Achievement> achievements = await _steamService.GetAchievementsAsync(steamId, null);

            // Create game instance
            Game game = new(steamId, gameName, achievements, dialog.FileName, iconUrl, true);

            // Add and save to db
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
            Resources.Resources.ConfirmDeleteGameMessage,
            Resources.Resources.ConfirmDeleteGameTitle
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
                    Resources.Resources.ConfirmPlayWithoutProxyMessage,
                    Resources.Resources.ConfirmPlayWithoutProxyTitle
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
            var dialog = new ConfirmDialog(Resources.Resources.ConfirmLogoutMessage, Resources.Resources.ConfirmLogoutTitle);
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
        if (!HasInternet) return;

        IsLoadingGames = true;

        try
        {
            List<Game> updatedGames = [];

            // Get potential new Steam games and and update existing ones
            if (IsUserLoggedIn && CurrentUser != null)
            {
                // Get owned games and update local data
                var ownedGames = await _steamService.GetOwnedGamesAsync(CurrentUser);
                updatedGames.AddRange(ownedGames);
            }

            // Update the games in the db
            _storageService.SaveGames(updatedGames);

            foreach (var game in updatedGames)
            {
                var existing = Games.FirstOrDefault(g => g.SteamId == game.SteamId);
                if (existing != null)
                {
                    existing = game;
                }
                else
                {
                    Games.Add(game);
                }
            }

            SelectedGame = Games.FirstOrDefault();
        }
        finally
        {
            IsLoadingGames = false;
        }
    }

    private async void RefreshLanguage()
    {
        IsLoadingGames = true;

        try
        {
            foreach (var game in Games)
            {
                if (game.Achievements.Count == 0) continue;

                // Get new achievements
                var achievements = await _steamService.GetAchievementsAsync(game.SteamId, null);

                if (achievements.Count != game.Achievements.Count)
                {
                    Console.WriteLine($"Skipping language update for {game.Name} due to achievement count mismatch. Original: {game.Achievements.Count}, New: {achievements.Count}");
                    continue;
                }

                foreach (var a in achievements)
                {
                    game.UpdateAchievementLanguage(a);
                }
            }

            // Update the games in the db
            _storageService.SaveGames([.. Games]);
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

            // Show UI notification
            _uiNotificationService.ShowUINotification(Resources.Resources.MessageNotificationSettingsMessage);
        }
    }
    
    private void PreviousStatPage()
    {
        CurrentStatPage = (CurrentStatPage - 1 + TotalStatPages) % TotalStatPages;
    }

    private void NextStatPage()
    {
        CurrentStatPage = (CurrentStatPage + 1) % TotalStatPages;
    }

    private void PreviousScreenshot()
    {
        if (SelectedGame == null || SelectedGame.Screenshots.Count == 0) return;
        CurrentScreenshotIndex = (CurrentScreenshotIndex + 1) % SelectedGame.Screenshots.Count;
    }

    private void NextScreenshot()
    {
        if (SelectedGame == null || SelectedGame.Screenshots.Count == 0) return;
        CurrentScreenshotIndex = (CurrentScreenshotIndex + 1) % SelectedGame.Screenshots.Count;
    }

    private void ViewScreenshot(Screenshot screenshot)
    {
        if (!File.Exists(screenshot.FilePath)) return;

        var viewer = new ScreenshotViewer(this)
        {
            Owner = Application.Current.MainWindow
        };
        viewer.PreviousRequested += PreviousScreenshot;
        viewer.NextRequested += NextScreenshot;

        viewer.Show();
    }

    #endregion


    #region Event Handlers
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

    private void OnScreenshotTaken(Screenshot screenshot)
    {
        var runningGame = _gameProcessService.GetRunningGame();
        if (runningGame == null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            runningGame.Screenshots.Add(screenshot);
        });
    }

    private void OnInternetStatusChanged(bool isOnline)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            HasInternet = isOnline;
            if (isOnline)
                _uiNotificationService.ShowUINotification(Resources.Resources.UINotificationOnlineMode);
            else
                _uiNotificationService.ShowUINotification(Resources.Resources.UINotificationOfflineMode);
        });
    }

    private void OnUINotificationServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_uiNotificationService.IsUINotificationVisible))
        {
            OnPropertyChanged(nameof(IsUINotificationVisible));
        }
        if (e.PropertyName == nameof(_uiNotificationService.UINotificationMessage))
        {
            OnPropertyChanged(nameof(UINotificationMessage));
        }
    } 

    #endregion

    #region Helper Methods

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

    private bool FilterGames(object? obj)
    {
        if (obj is not Game game) return false;

        var query = _searchText?.Trim().ToLower();
        if (string.IsNullOrEmpty(query)) return true;

        // Match by name
        return game.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyLanguage(string cultureCode)
    {
        if (string.IsNullOrEmpty(cultureCode)) return;
        try
        {
            var culture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Resources.Resources.Culture = culture;

            // Save language in db
            _storageService.SaveLanguage(cultureCode);

            Loc.Refresh();
        }
        catch (CultureNotFoundException)
        {
            // Fallback to english
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Resources.Resources.Culture = culture;

            // Save language in db
            _storageService.SaveLanguage("en-US");

            Loc.Refresh();
        }
    }

    #endregion
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}