using WallpaperManager.Models;

namespace WallpaperManager.Services.Interfaces;

public interface ISettingsService
{
    WallpaperSettings Settings { get; }
    Task LoadAsync();
    Task SaveAsync();
    event EventHandler? SettingsChanged;
}
