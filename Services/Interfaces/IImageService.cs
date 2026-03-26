using WallpaperManager.Models;

namespace WallpaperManager.Services.Interfaces;

public interface IImageService
{
    Task<ImageInfo?> GetNextImageAsync();
    Task<List<ImageInfo>> PreloadAsync(int count);
    void SetSource(SourceType sourceType);
    void SetLocalFolders(IEnumerable<string> folders);
    void SetOnlineQuery(string query);
    void SetOnlineCategory(string category);
    void SetOnlineAccessKey(string accessKey);
    bool IsOnlineAvailable { get; }
    bool IsLocalAvailable { get; }
    event EventHandler<string>? SourceExhausted;
}
