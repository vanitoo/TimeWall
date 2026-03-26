using WallpaperManager.Models;

namespace WallpaperManager.Services.Interfaces;

public interface IImageSource
{
    SourceType Type { get; }
    string Name { get; }
    Task<ImageInfo?> GetRandomImageAsync();
    Task<List<ImageInfo>> PreloadImagesAsync(int count);
    Task<bool> IsAvailableAsync();
    void Configure(Dictionary<string, object> settings);
}
