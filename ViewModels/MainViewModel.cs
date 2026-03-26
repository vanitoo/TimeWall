using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WallpaperManager.Models;
using WallpaperManager.Services;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IWallpaperService _wallpaperService;
    private readonly IImageService _imageService;
    private readonly ICacheService _cacheService;
    private readonly ISettingsService _settingsService;
    private readonly TrayService _trayService;

    [ObservableProperty]
    private BitmapImage? _currentWallpaperImage;

    [ObservableProperty]
    private string _currentWallpaperPath = string.Empty;

    [ObservableProperty]
    private string _currentSourceName = string.Empty;

    [ObservableProperty]
    private bool _isAutoChangeEnabled;

    [ObservableProperty]
    private string _timerIntervalText = string.Empty;

    [ObservableProperty]
    private string _cacheSizeText = string.Empty;

    [ObservableProperty]
    private string _nextChangeText = string.Empty;

    public MainViewModel(
        IWallpaperService wallpaperService,
        IImageService imageService,
        ICacheService cacheService,
        ISettingsService settingsService,
        TrayService trayService)
    {
        _wallpaperService = wallpaperService;
        _imageService = imageService;
        _cacheService = cacheService;
        _settingsService = settingsService;
        _trayService = trayService;

        _wallpaperService.WallpaperChanged += OnWallpaperChanged;
        _wallpaperService.WallpaperChangeFailed += OnWallpaperChangeFailed;

        UpdateFromSettings();
    }

    private void UpdateFromSettings()
    {
        var settings = _settingsService.Settings;
        IsAutoChangeEnabled = _wallpaperService.IsRunning;
        CurrentSourceName = settings.CurrentSource == SourceType.Online
            ? $"Онлайн ({settings.OnlineQuery})"
            : "Локальные папки";
        TimerIntervalText = $"Интервал: {settings.TimerIntervalHours} ч.";
    }

    private void OnWallpaperChanged(object? sender, ImageInfo image)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                if (File.Exists(image.FilePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(image.FilePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 800;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    CurrentWallpaperImage = bitmap;
                }

                CurrentWallpaperPath = image.FilePath;
                StatusMessage = $"Обои установлены: {Path.GetFileName(image.FilePath)}";
                Log.Information("Wallpaper updated in UI: {Path}", image.FilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update wallpaper preview");
            }

            _ = UpdateCacheSizeAsync();
        });
    }

    private void OnWallpaperChangeFailed(object? sender, Exception ex)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            StatusMessage = $"Ошибка: {ex.Message}";
            _trayService.ShowNotification("Ошибка смены обоев", ex.Message,
                Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
        });
    }

    private async Task UpdateCacheSizeAsync()
    {
        try
        {
            var size = await _cacheService.GetCacheSizeAsync();
            var sizeMb = size / (1024.0 * 1024.0);
            CacheSizeText = $"Кэш: {sizeMb:F1} MB";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to update cache size");
        }
    }

    [RelayCommand]
    private void ToggleAutoChange()
    {
        if (IsAutoChangeEnabled)
        {
            _wallpaperService.StopAutoChange();
            IsAutoChangeEnabled = false;
            StatusMessage = "Автосмена остановлена";
        }
        else
        {
            _wallpaperService.StartAutoChange();
            IsAutoChangeEnabled = true;
            StatusMessage = "Автосмена запущена";
        }

        _trayService.UpdateStatus();
        UpdateFromSettings();
    }

    [RelayCommand]
    private void ChangeNow()
    {
        _wallpaperService.ChangeNow();
        StatusMessage = "Смена обоев...";
    }

    [RelayCommand]
    private void ShowSettings()
    {
        _trayService.RequestShowSettings();
    }

    [RelayCommand]
    private void ShowPreview()
    {
        _trayService.RequestShowPreview();
    }

    public void RefreshSettings()
    {
        UpdateFromSettings();
        _ = UpdateCacheSizeAsync();
    }
}
