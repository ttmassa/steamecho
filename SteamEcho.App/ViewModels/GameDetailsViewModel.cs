using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SteamEcho.App.Views;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.ViewModels;

public class GameDetailsViewModel : INotifyPropertyChanged
{
    private readonly Game _game;
    public Game Game => _game;
    private readonly MainWindowViewModel _parentViewModel;

    // Services
    private readonly IStorageService _storageService;

    // Commands
    public ICommand LockAchievementCommand { get; }
    public ICommand UnlockAchievementCommand { get; }
    public ICommand PreviousStatPageCommand { get; }
    public ICommand NextStatPageCommand { get; }
    public ICommand PreviousScreenshotCommand { get; }
    public ICommand NextScreenshotCommand { get; }
    public ICommand ViewScreenshotCommand { get; }

    // Properties
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
    public Screenshot? CurrentScreenshot => _game?.Screenshots.Count > 0 && CurrentScreenshotIndex >= 0 && CurrentScreenshotIndex < _game.Screenshots.Count
        ? _game.Screenshots[CurrentScreenshotIndex]
        : null;
    public string ScreenshotCounterText => _game?.Screenshots.Count > 0
        ? $"{CurrentScreenshotIndex + 1} / {_game.Screenshots.Count}"
        : "0 / 0";


    public GameDetailsViewModel(Game game, IStorageService storageService, MainWindowViewModel parentViewModel)
    {
        // Initialize services
        _game = game;
        _storageService = storageService;
        _parentViewModel = parentViewModel;

        // Commands
        LockAchievementCommand = new RelayCommand<Achievement>(LockAchievement);
        UnlockAchievementCommand = new RelayCommand<Achievement>(UnlockAchievement);
        PreviousStatPageCommand = new RelayCommand(PreviousStatPage);
        NextStatPageCommand = new RelayCommand(NextStatPage);
        PreviousScreenshotCommand = new RelayCommand(PreviousScreenshot);
        NextScreenshotCommand = new RelayCommand(NextScreenshot);
        ViewScreenshotCommand = new RelayCommand<Screenshot>(ViewScreenshot);
    }

    private void LockAchievement(Achievement achievement)
    {
        if (achievement != null && achievement.IsUnlocked && _game != null)
        {
            achievement.IsUnlocked = false;
            achievement.UnlockDate = null;
            _storageService.UpdateAchievement(_game.SteamId, achievement.Id, false, null);
        }
    }

    private void UnlockAchievement(Achievement achievement)
    {
        if (achievement != null && !achievement.IsUnlocked && _game != null)
        {
            achievement.Unlock();
            _storageService.UpdateAchievement(_game.SteamId, achievement.Id, true, achievement.UnlockDate);
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
        if (_game == null || _game.Screenshots.Count == 0) return;
        CurrentScreenshotIndex = (CurrentScreenshotIndex + 1) % _game.Screenshots.Count;
    }

    private void NextScreenshot()
    {
        if (_game == null || _game.Screenshots.Count == 0) return;
        CurrentScreenshotIndex = (CurrentScreenshotIndex + 1) % _game.Screenshots.Count;
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}