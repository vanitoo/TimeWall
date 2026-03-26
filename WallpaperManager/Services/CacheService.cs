using Serilog;
using WallpaperManager.Helpers;
using WallpaperManager.Models;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.Services;

public class CacheService : ICacheService
{
    private readonly string _cacheDirectory;
    private readonly AsyncLock _lock = new();
    private readonly Dictionary<string, ImageInfo> _cacheIndex = new();
    private const long MaxCacheSizeBytes = 500L * 1024 * 1024;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string CacheDirectory => _cacheDirectory;

    public CacheService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _cacheDirectory = Path.Combine(appDataPath, "WallpaperManager", "Cache");
        Directory.CreateDirectory(_cacheDirectory);

        Task.Run(LoadCacheIndexAsync);
    }

    private async Task LoadCacheIndexAsync()
    {
        try
        {
            var indexPath = Path.Combine(_cacheDirectory, "index.json");
            if (File.Exists(indexPath))
            {
                var json = await File.ReadAllTextAsync(indexPath);
                var index = JsonSerializer.Deserialize<Dictionary<string, ImageInfo>>(json, _jsonOptions);
                if (index != null)
                {
                    lock (_cacheIndex)
                    {
                        foreach (var kvp in index)
                        {
                            if (File.Exists(kvp.Value.FilePath))
                            {
                                _cacheIndex[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    Log.Information("Loaded {Count} cached images", _cacheIndex.Count);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load cache index");
        }
    }

    private async Task SaveCacheIndexAsync()
    {
        try
        {
            Dictionary<string, ImageInfo> snapshot;
            lock (_cacheIndex)
            {
                snapshot = new Dictionary<string, ImageInfo>(_cacheIndex);
            }

            var indexPath = Path.Combine(_cacheDirectory, "index.json");
            var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            await File.WriteAllTextAsync(indexPath, json);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to save cache index");
        }
    }

    public async Task<ImageInfo?> GetCachedImageAsync(string imageId)
    {
        await Task.CompletedTask;
        lock (_cacheIndex)
        {
            if (_cacheIndex.TryGetValue(imageId, out var image) && File.Exists(image.FilePath))
            {
                return image;
            }
        }
        return null;
    }

    public async Task<ImageInfo> CacheImageAsync(string url, string imageId)
    {
        using var releaser = await _lock.LockAsync();

        var extension = Path.GetExtension(new Uri(url).AbsolutePath);
        if (string.IsNullOrEmpty(extension) || extension == "/")
        {
            extension = ".jpg";
        }
        extension = extension.TrimStart('.');

        var fileName = $"{imageId}.{extension}";
        var filePath = Path.Combine(_cacheDirectory, fileName);

        if (File.Exists(filePath))
        {
            lock (_cacheIndex)
            {
                if (_cacheIndex.TryGetValue(imageId, out var existing))
                {
                    return existing;
                }
            }
        }

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Add("User-Agent", "WallpaperManager/1.0");

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var imageData = await response.Content.ReadAsByteArrayAsync();

            await File.WriteAllBytesAsync(filePath, imageData);

            var imageInfo = new ImageInfo
            {
                Id = imageId,
                FilePath = filePath,
                Url = url,
                SourceType = SourceType.Online,
                DownloadedAt = DateTime.UtcNow,
                FileSize = imageData.Length
            };

            lock (_cacheIndex)
            {
                _cacheIndex[imageId] = imageInfo;
            }

            await SaveCacheIndexAsync();

            await CleanupOldCacheAsync();

            Log.Information("Cached image {ImageId}, size: {Size} bytes", imageId, imageData.Length);
            return imageInfo;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to cache image {ImageId} from {Url}", imageId, url);
            throw;
        }
    }

    public async Task<List<ImageInfo>> GetPreloadedImagesAsync(int count)
    {
        await Task.CompletedTask;
        var result = new List<ImageInfo>();

        lock (_cacheIndex)
        {
            var available = _cacheIndex.Values
                .Where(i => File.Exists(i.FilePath))
                .OrderByDescending(i => i.DownloadedAt)
                .Take(count)
                .ToList();

            result.AddRange(available);
        }

        return result;
    }

    public async Task CleanupOldCacheAsync()
    {
        try
        {
            var currentSize = await GetCacheSizeAsync();
            if (currentSize <= MaxCacheSizeBytes)
                return;

            Log.Information("Cache size {Size}MB exceeds limit, cleaning up...", currentSize / (1024 * 1024));

            List<ImageInfo> sortedImages;
            lock (_cacheIndex)
            {
                sortedImages = _cacheIndex.Values
                    .Where(i => File.Exists(i.FilePath))
                    .OrderBy(i => i.DownloadedAt)
                    .ToList();
            }

            long bytesToFree = currentSize - (MaxCacheSizeBytes * 80 / 100);

            foreach (var image in sortedImages)
            {
                if (bytesToFree <= 0)
                    break;

                try
                {
                    var fileInfo = new FileInfo(image.FilePath);
                    var fileSize = fileInfo.Length;

                    File.Delete(image.FilePath);

                    lock (_cacheIndex)
                    {
                        _cacheIndex.Remove(image.Id);
                    }

                    bytesToFree -= fileSize;
                    Log.Debug("Removed cached image {ImageId}, freed {Size} bytes", image.Id, fileSize);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to remove cached image {ImageId}", image.Id);
                }
            }

            await SaveCacheIndexAsync();
            Log.Information("Cache cleanup completed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to cleanup cache");
        }
    }

    public async Task<long> GetCacheSizeAsync()
    {
        await Task.CompletedTask;
        long totalSize = 0;

        lock (_cacheIndex)
        {
            foreach (var image in _cacheIndex.Values)
            {
                try
                {
                    if (File.Exists(image.FilePath))
                    {
                        var fileInfo = new FileInfo(image.FilePath);
                        totalSize += fileInfo.Length;
                    }
                }
                catch
                {
                    // Ignore inaccessible files
                }
            }
        }

        return totalSize;
    }
}
