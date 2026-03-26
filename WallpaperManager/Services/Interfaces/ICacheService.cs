using WallpaperManager.Models;

namespace WallpaperManager.Services.Interfaces;

public interface ICacheService
{
    Task<ImageInfo?> GetCachedImageAsync(string imageId);
    Task<ImageInfo> CacheImageAsync(string url, string imageId);
    Task<List<ImageInfo>> GetPreloadedImagesAsync(int count);
    Task CleanupOldCacheAsync();
    Task<long> GetCacheSizeAsync();
    string CacheDirectory { get; }
}
