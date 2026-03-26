using System.Text.Json.Serialization;

namespace WallpaperManager.Models;

public class WallpaperSettings
{
    [JsonPropertyName("timerIntervalHours")]
    public int TimerIntervalHours { get; set; } = 2;

    [JsonPropertyName("currentSource")]
    public SourceType CurrentSource { get; set; } = SourceType.Online;

    [JsonPropertyName("localFolders")]
    public List<string> LocalFolders { get; set; } = new();

    [JsonPropertyName("onlineCategory")]
    public string OnlineCategory { get; set; } = "nature";

    [JsonPropertyName("onlineQuery")]
    public string OnlineQuery { get; set; } = "landscape";

    [JsonPropertyName("lastWallpaperPath")]
    public string? LastWallpaperPath { get; set; }

    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; }

    [JsonPropertyName("minimizeToTray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonPropertyName("unsplashAccessKey")]
    public string UnsplashAccessKey { get; set; } = string.Empty;

    [JsonPropertyName("onlinePreloadCount")]
    public int OnlinePreloadCount { get; set; } = 5;

    [JsonPropertyName("lastOnlineImageIds")]
    public List<string> LastOnlineImageIds { get; set; } = new();
}
