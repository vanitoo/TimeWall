namespace WallpaperManager.Models;

public enum SourceType
{
    Local,
    Online
}

public class ImageInfo
{
    public required string Id { get; set; }
    public required string FilePath { get; set; }
    public string? Url { get; set; }
    public SourceType SourceType { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public bool IsLocal => SourceType == SourceType.Local;
    public bool IsOnline => SourceType == SourceType.Online;
}
