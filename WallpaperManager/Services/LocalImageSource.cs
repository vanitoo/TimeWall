using Serilog;
using WallpaperManager.Models;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.Services;

public class LocalImageSource : IImageSource
{
    private readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp"
    };

    private List<string> _folders = new();
    private readonly Random _random = new();
    private readonly object _lock = new();

    public SourceType Type => SourceType.Local;
    public string Name => "Local Folders";

    public void Configure(Dictionary<string, object> settings)
    {
        if (settings.TryGetValue("Folders", out var foldersObj) && foldersObj is IEnumerable<string> folders)
        {
            lock (_lock)
            {
                _folders = folders.Where(Directory.Exists).ToList();
            }
            Log.Information("LocalImageSource configured with {Count} folders", _folders.Count);
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            return _folders.Count > 0 && _folders.Any(Directory.Exists);
        }
    }

    public async Task<ImageInfo?> GetRandomImageAsync()
    {
        List<string> folders;
        lock (_lock)
        {
            folders = _folders.Where(Directory.Exists).ToList();
        }

        if (folders.Count == 0)
        {
            Log.Warning("No valid folders available in LocalImageSource");
            return null;
        }

        try
        {
            var allImages = new List<string>();

            foreach (var folder in folders)
            {
                try
                {
                    var images = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                        .Where(f => _supportedExtensions.Contains(Path.GetExtension(f)))
                        .ToList();
                    allImages.AddRange(images);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to scan folder {Folder}", folder);
                }
            }

            if (allImages.Count == 0)
            {
                Log.Warning("No images found in local folders");
                return null;
            }

            var selectedPath = allImages[_random.Next(allImages.Count)];
            var fileInfo = new FileInfo(selectedPath);

            return new ImageInfo
            {
                Id = ComputeFileHash(selectedPath),
                FilePath = selectedPath,
                SourceType = SourceType.Local,
                FileSize = fileInfo.Length
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get random image from local source");
            return null;
        }
    }

    public async Task<List<ImageInfo>> PreloadImagesAsync(int count)
    {
        var result = new List<ImageInfo>();

        for (int i = 0; i < count; i++)
        {
            var image = await GetRandomImageAsync();
            if (image != null)
            {
                result.Add(image);
            }
        }

        return result;
    }

    private static string ComputeFileHash(string path)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(path);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}
