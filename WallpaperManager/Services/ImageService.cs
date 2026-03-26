using Serilog;
using WallpaperManager.Models;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.Services;

public class ImageService : IImageService
{
    private readonly ICacheService _cacheService;
    private readonly LocalImageSource _localSource;
    private readonly OnlineImageSource _onlineSource;
    private readonly List<IImageSource> _sources = new();

    private IImageSource _currentSource;
    private IImageSource? _fallbackSource;
    private readonly object _lock = new();

    public bool IsOnlineAvailable { get; private set; }
    public bool IsLocalAvailable { get; private set; }

    public event EventHandler<string>? SourceExhausted;

    public ImageService(ICacheService cacheService)
    {
        _cacheService = cacheService;
        _localSource = new LocalImageSource();
        _onlineSource = new OnlineImageSource(cacheService);

        _sources.Add(_onlineSource);
        _sources.Add(_localSource);

        _currentSource = _onlineSource;
        _fallbackSource = _localSource;
    }

    public void SetSource(SourceType sourceType)
    {
        lock (_lock)
        {
            _currentSource = sourceType switch
            {
                SourceType.Online => _onlineSource,
                SourceType.Local => _localSource,
                _ => _onlineSource
            };

            _fallbackSource = sourceType switch
            {
                SourceType.Online => _localSource,
                SourceType.Local => _onlineSource,
                _ => _localSource
            };

            Log.Information("Image source changed to {Source}, fallback to {Fallback}",
                _currentSource.Name, _fallbackSource?.Name ?? "none");
        }
    }

    public void SetLocalFolders(IEnumerable<string> folders)
    {
        _localSource.Configure(new Dictionary<string, object>
        {
            { "Folders", folders }
        });

        Task.Run(async () =>
        {
            IsLocalAvailable = await _localSource.IsAvailableAsync();
            Log.Information("Local source available: {Available}", IsLocalAvailable);
        });
    }

    public void SetOnlineQuery(string query)
    {
        _onlineSource.Configure(new Dictionary<string, object>
        {
            { "Query", query }
        });
    }

    public void SetOnlineCategory(string category)
    {
        _onlineSource.Configure(new Dictionary<string, object>
        {
            { "Category", category }
        });
    }

    public void SetOnlineAccessKey(string accessKey)
    {
        _onlineSource.Configure(new Dictionary<string, object>
        {
            { "AccessKey", accessKey }
        });

        Task.Run(async () =>
        {
            IsOnlineAvailable = await _onlineSource.IsAvailableAsync();
            Log.Information("Online source available: {Available}", IsOnlineAvailable);
        });
    }

    public async Task<ImageInfo?> GetNextImageAsync()
    {
        IImageSource? primary;
        IImageSource? fallback;

        lock (_lock)
        {
            primary = _currentSource;
            fallback = _fallbackSource;
        }

        Log.Debug("Getting next image from {Source}", primary.Name);

        var image = await TryGetImageAsync(primary);
        if (image != null)
        {
            Log.Information("Got image from primary source: {Source}", primary.Name);
            return image;
        }

        if (fallback != null)
        {
            Log.Warning("Primary source {Primary} failed, trying fallback {Fallback}",
                primary.Name, fallback.Name);

            image = await TryGetImageAsync(fallback);
            if (image != null)
            {
                Log.Information("Got image from fallback source: {Source}", fallback.Name);
                return image;
            }
        }

        var preloaded = await _cacheService.GetPreloadedImagesAsync(1);
        if (preloaded.Count > 0)
        {
            Log.Information("Using preloaded image from cache");
            return preloaded[0];
        }

        Log.Error("All image sources exhausted");
        SourceExhausted?.Invoke(this, "All image sources are unavailable");

        return null;
    }

    private async Task<ImageInfo?> TryGetImageAsync(IImageSource source)
    {
        try
        {
            var isAvailable = await source.IsAvailableAsync();
            if (!isAvailable)
            {
                Log.Debug("Source {Source} is not available", source.Name);
                return null;
            }

            return await source.GetRandomImageAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting image from {Source}", source.Name);
            return null;
        }
    }

    public async Task<List<ImageInfo>> PreloadAsync(int count)
    {
        Log.Information("Starting preload of {Count} images", count);

        var tasks = new List<Task<List<ImageInfo>>>
        {
            _onlineSource.PreloadImagesAsync(count),
            _localSource.PreloadImagesAsync(count)
        };

        try
        {
            await Task.WhenAll(tasks);

            var allImages = new List<ImageInfo>();
            foreach (var task in tasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    allImages.AddRange(task.Result);
                }
            }

            Log.Information("Preloaded {Count} images total", allImages.Count);
            return allImages;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during preload");
            return new List<ImageInfo>();
        }
    }
}
