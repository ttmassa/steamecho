using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SteamEcho.App.Models;
using SteamEcho.App.Services;
using SteamEcho.Core.Services;

namespace SteamEcho.App.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    // Collections
    public ObservableCollection<string> NotificationColors { get; } =
    [
        "#4A4A4D",
        "#000044",
        "#000000",
        "#C02222",
        "#19680b",
        "#BF00FF"
    ];

    public ObservableCollection<LanguageOption> AvailableLanguages { get; } =
    [
        new() { DisplayName = "English", CultureName = "en-US", SteamCode = "english", FlagPath = "/SteamEcho.App;component/Assets/Images/us_flag_icon.png" },
        new() { DisplayName = "Français", CultureName = "fr-FR", SteamCode = "french", FlagPath = "/SteamEcho.App;component/Assets/Images/french_flag_icon.png" }
    ];

    // Services
    private readonly INotificationService _notificationService;
    private readonly IUINotificationService _uiNotificationService;
    private readonly IStorageService _storageService;
    private readonly ISteamService _steamService;
    private NotificationConfig? _draftNotificationConfig;
    public static LocalizationService Loc => LocalizationService.Instance;
    // Commands
    public ICommand TestNotificationCommand { get; }
    public ICommand SaveNotificationSettingsCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand HideSettingsCommand { get; }

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
                    _draftNotificationConfig = new NotificationConfig
                    {
                        NotificationSize = _notificationService.Config.NotificationSize,
                        NotificationTime = _notificationService.Config.NotificationTime,
                        NotificationColor = _notificationService.Config.NotificationColor
                    };
                }
                else
                {
                    _draftNotificationConfig = null;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(NotificationSize));
                OnPropertyChanged(nameof(NotificationTime));
                OnPropertyChanged(nameof(NotificationColor));
                OnPropertyChanged(nameof(IsNotificationSaved));
            }
        }
    }

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

    private LanguageOption? _selectedLanguage;
    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value && value != null)
            {
                _selectedLanguage = value;
                OnPropertyChanged();
                _steamService.ApiLanguage = value.SteamCode;
                ApplyLanguage(value.CultureName);
                LanguageChanged?.Invoke();
            }
        }
    }

    // Event fired when language is changed
    public event Action? LanguageChanged;

    public SettingsViewModel(INotificationService notificationService, IUINotificationService uINotificationService, IStorageService storageService, ISteamService steamService)
    {
        // Initializating services
        _notificationService = notificationService;
        _uiNotificationService = uINotificationService;
        _storageService = storageService;
        _steamService = steamService;

        // Commands
        TestNotificationCommand = new RelayCommand(TestNotification);
        SaveNotificationSettingsCommand = new RelayCommand(SaveNotificationSettings);
        ShowSettingsCommand = new RelayCommand(() => IsSettingsVisible = true);
        HideSettingsCommand = new RelayCommand(() => IsSettingsVisible = false);

        // Setting culture code
        var cultureCode = _storageService.LoadLanguage();
        SelectedLanguage = AvailableLanguages.FirstOrDefault(lang => lang.CultureName == cultureCode)
                           ?? AvailableLanguages.First(lang => lang.CultureName == "en-US");
        _steamService.ApiLanguage = SelectedLanguage.SteamCode;
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

            OnPropertyChanged(nameof(IsNotificationSaved));
            _uiNotificationService.ShowUINotification(Resources.Resources.MessageNotificationSettingsMessage);
        }
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}