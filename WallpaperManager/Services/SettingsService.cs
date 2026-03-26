using System.Text.Json;
using Serilog;
using WallpaperManager.Models;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private WallpaperSettings _settings = new();
    private readonly object _lock = new();

    public WallpaperSettings Settings
    {
        get
        {
            lock (_lock)
            {
                return _settings;
            }
        }
    }

    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "WallpaperManager");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var loaded = JsonSerializer.Deserialize<WallpaperSettings>(json, _jsonOptions);
                if (loaded != null)
                {
                    lock (_lock)
                    {
                        _settings = loaded;
                    }
                    Log.Information("Settings loaded from {Path}", _settingsPath);
                }
            }
            else
            {
                Log.Information("No settings file found, using defaults");
                await SaveAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load settings, using defaults");
            _settings = new WallpaperSettings();
            await SaveAsync();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            WallpaperSettings current;
            lock (_lock)
            {
                current = _settings;
            }

            var json = JsonSerializer.Serialize(current, _jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);
            Log.Debug("Settings saved to {Path}", _settingsPath);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save settings");
            throw;
        }
    }
}
