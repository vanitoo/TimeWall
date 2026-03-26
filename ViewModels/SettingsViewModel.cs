using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Serilog;
using WallpaperManager.Models;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IImageService _imageService;
    private readonly ITimerService _timerService;

    [ObservableProperty]
    private int _timerIntervalHours;

    [ObservableProperty]
    private SourceType _selectedSource;

    [ObservableProperty]
    private string _localFolderPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _localFolders = new();

    [ObservableProperty]
    private string _onlineQuery = string.Empty;

    [ObservableProperty]
    private string _onlineCategory = string.Empty;

    [ObservableProperty]
    private string _unsplashAccessKey = string.Empty;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private int _onlinePreloadCount;

    [ObservableProperty]
    private bool _isOnlineSourceAvailable;

    [ObservableProperty]
    private bool _isLocalSourceAvailable;

    public event EventHandler? SettingsSaved;
    public event EventHandler? SettingsCancelled;

    public IReadOnlyList<SourceType> SourceTypes { get; } = Enum.GetValues<SourceType>();

    public IReadOnlyList<string> Categories { get; } = new[]
    {
        "nature", "architecture", "travel", "animals", "technology",
        "food", "people", "business", "sports", "art"
    };

    public IReadOnlyList<int> PreloadCounts { get; } = new[] { 3, 5, 10, 15 };

    public SettingsViewModel(
        ISettingsService settingsService,
        IImageService imageService,
        ITimerService timerService)
    {
        _settingsService = settingsService;
        _imageService = imageService;
        _timerService = timerService;

        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;

        TimerIntervalHours = settings.TimerIntervalHours;
        SelectedSource = settings.CurrentSource;
        OnlineQuery = settings.OnlineQuery;
        OnlineCategory = settings.OnlineCategory;
        UnsplashAccessKey = settings.UnsplashAccessKey;
        StartWithWindows = settings.StartWithWindows;
        MinimizeToTray = settings.MinimizeToTray;
        OnlinePreloadCount = settings.OnlinePreloadCount;

        LocalFolders.Clear();
        foreach (var folder in settings.LocalFolders)
        {
            LocalFolders.Add(folder);
        }

        if (string.IsNullOrEmpty(LocalFolderPath) && LocalFolders.Count > 0)
        {
            LocalFolderPath = LocalFolders[0];
        }

        CheckSourceAvailability();
    }

    private async void CheckSourceAvailability()
    {
        IsOnlineSourceAvailable = _imageService.IsOnlineAvailable;
        IsLocalSourceAvailable = _imageService.IsLocalAvailable;
    }

    [RelayCommand]
    private void AddLocalFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выберите папку с изображениями"
        };

        if (dialog.ShowDialog() == true)
        {
            var path = dialog.FolderName;
            if (!LocalFolders.Contains(path))
            {
                LocalFolders.Add(path);
                Log.Information("Added local folder: {Path}", path);
            }
        }
    }

    [RelayCommand]
    private void RemoveLocalFolder(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            LocalFolders.Remove(path);
            Log.Information("Removed local folder: {Path}", path);
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        Task.Run(async () =>
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { IsBusy = true; StatusMessage = "Сохранение настроек..."; });

                var settings = _settingsService.Settings;

                settings.TimerIntervalHours = TimerIntervalHours;
                settings.CurrentSource = SelectedSource;
                settings.LocalFolders = LocalFolders.ToList();
                settings.OnlineQuery = OnlineQuery;
                settings.OnlineCategory = OnlineCategory;
                settings.UnsplashAccessKey = UnsplashAccessKey;
                settings.StartWithWindows = StartWithWindows;
                settings.MinimizeToTray = MinimizeToTray;
                settings.OnlinePreloadCount = OnlinePreloadCount;

                await _settingsService.SaveAsync();

                _imageService.SetSource(SelectedSource);
                _imageService.SetLocalFolders(LocalFolders);
                _imageService.SetOnlineQuery(OnlineQuery);
                _imageService.SetOnlineCategory(OnlineCategory);
                _imageService.SetOnlineAccessKey(UnsplashAccessKey);

                if (_timerService.IsRunning)
                {
                    _timerService.SetInterval(TimeSpan.FromHours(TimerIntervalHours));
                }

                UpdateStartupRegistry();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Настройки сохранены";
                    SettingsSaved?.Invoke(this, EventArgs.Empty);
                });
                Log.Information("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { StatusMessage = $"Ошибка сохранения: {ex.Message}"; });
            }
            finally
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { IsBusy = false; });
            }
        });
    }

    private void UpdateStartupRegistry()
    {
        try
        {
            const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string valueName = "WallpaperManager";

            using var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            if (key == null) return;

            if (StartWithWindows)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(valueName, $"\"{exePath}\"");
                    Log.Information("Added to startup registry");
                }
            }
            else
            {
                key.DeleteValue(valueName, false);
                Log.Information("Removed from startup registry");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to update startup registry");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        SettingsCancelled?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void TestOnlineSource()
    {
        Task.Run(async () =>
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { IsBusy = true; StatusMessage = "Проверка онлайн-источника..."; });

                _imageService.SetOnlineAccessKey(UnsplashAccessKey);
                var isAvailable = _imageService.IsOnlineAvailable;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsOnlineSourceAvailable = isAvailable;
                    StatusMessage = isAvailable
                        ? "Онлайн-источник доступен"
                        : "Онлайн-источник недоступен. Проверьте API ключ.";
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Ошибка: {ex.Message}";
                    IsOnlineSourceAvailable = false;
                });
            }
            finally
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { IsBusy = false; });
            }
        });
    }
}
