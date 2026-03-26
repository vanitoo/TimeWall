using WallpaperManager.Models;

namespace WallpaperManager.Services.Interfaces;

public interface IWallpaperService
{
    Task<bool> SetWallpaperAsync(ImageInfo image);
    Task<ImageInfo?> GetNextImageAsync();
    void StartAutoChange();
    void StopAutoChange();
    void ChangeNow();
    event EventHandler<ImageInfo>? WallpaperChanged;
    event EventHandler<Exception>? WallpaperChangeFailed;
    bool IsRunning { get; }
}
