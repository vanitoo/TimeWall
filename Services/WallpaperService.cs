using Serilog;
using WallpaperManager.Helpers;
using WallpaperManager.Models;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.Services;

public class WallpaperService : IWallpaperService
{
    private readonly IImageService _imageService;
    private readonly ICacheService _cacheService;
    private readonly ITimerService _timerService;
    private readonly ISettingsService _settingsService;

    private ImageInfo? _currentImage;
    private readonly object _lock = new();

    public bool IsRunning => _timerService.IsRunning;

    public event EventHandler<ImageInfo>? WallpaperChanged;
    public event EventHandler<Exception>? WallpaperChangeFailed;

    public WallpaperService(
        IImageService imageService,
        ICacheService cacheService,
        ITimerService timerService,
        ISettingsService settingsService)
    {
        _imageService = imageService;
        _cacheService = cacheService;
        _timerService = timerService;
        _settingsService = settingsService;

        _timerService.Tick += OnTimerTick;
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        await ChangeWallpaperAsync();
    }

    public void StartAutoChange()
    {
        var settings = _settingsService.Settings;
        var interval = TimeSpan.FromHours(settings.TimerIntervalHours);

        _timerService.Start(interval);

        _ = PreloadImagesAsync();

        Log.Information("Auto change started with interval {Hours} hours", settings.TimerIntervalHours);
    }

    public void StopAutoChange()
    {
        _timerService.Stop();
        Log.Information("Auto change stopped");
    }

    public void ChangeNow()
    {
        Task.Run(async () => await ChangeWallpaperAsync());
    }

    public async Task<ImageInfo?> GetNextImageAsync()
    {
        return await _imageService.GetNextImageAsync();
    }

    private async Task ChangeWallpaperAsync()
    {
        try
        {
            Log.Information("Changing wallpaper...");

            var image = await _imageService.GetNextImageAsync();
            if (image == null)
            {
                Log.Warning("No image available to set");
                return;
            }

            var success = await SetWallpaperAsync(image);
            if (success)
            {
                lock (_lock)
                {
                    _currentImage = image;
                }

                _settingsService.Settings.LastWallpaperPath = image.FilePath;
                await _settingsService.SaveAsync();

                WallpaperChanged?.Invoke(this, image);

                _ = PreloadImagesAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to change wallpaper");
            WallpaperChangeFailed?.Invoke(this, ex);
        }
    }

    private async Task PreloadImagesAsync()
    {
        try
        {
            var settings = _settingsService.Settings;
            await _imageService.PreloadAsync(settings.OnlinePreloadCount);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to preload images");
        }
    }

    public async Task<bool> SetWallpaperAsync(ImageInfo image)
    {
        try
        {
            if (!File.Exists(image.FilePath))
            {
                Log.Error("Image file not found: {Path}", image.FilePath);
                return false;
            }

            Win32Interop.SetWallpaper(image.FilePath, Win32Interop.WallpaperStyle.Fill);

            Log.Information("Wallpaper set successfully: {Path}", image.FilePath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to set wallpaper");
            WallpaperChangeFailed?.Invoke(this, ex);
            return false;
        }
    }

    public ImageInfo? GetCurrentWallpaper()
    {
        lock (_lock)
        {
            return _currentImage;
        }
    }
}
