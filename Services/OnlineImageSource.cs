using System.Net.Http.Json;
using System.Text.Json;
using Serilog;
using WallpaperManager.Models;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.Services;

public class OnlineImageSource : IImageSource
{
    private readonly ICacheService _cacheService;
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string _apiUrl = "https://api.unsplash.com";
    private string _accessKey = string.Empty;
    private string _query = "nature";
    private string _category = "nature";
    private List<string> _recentImageIds = new();
    private const int MaxRecentImages = 50;

    public SourceType Type => SourceType.Online;
    public string Name => "Unsplash Online";

    public OnlineImageSource(ICacheService cacheService)
    {
        _cacheService = cacheService;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void Configure(Dictionary<string, object> settings)
    {
        if (settings.TryGetValue("ApiUrl", out var url) && url is string urlStr)
        {
            _apiUrl = urlStr;
        }

        if (settings.TryGetValue("AccessKey", out var key) && key is string keyStr)
        {
            _accessKey = keyStr;
        }

        if (settings.TryGetValue("Query", out var query) && query is string queryStr)
        {
            _query = queryStr;
        }

        if (settings.TryGetValue("Category", out var category) && category is string categoryStr)
        {
            _category = categoryStr;
        }

        if (settings.TryGetValue("RecentIds", out var recentIds) && recentIds is IEnumerable<string> ids)
        {
            _recentImageIds = ids.ToList();
        }

        Log.Information("OnlineImageSource configured: query={Query}, category={Category}",
            _query, _category);
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (string.IsNullOrWhiteSpace(_accessKey))
        {
            Log.Debug("Online source unavailable: no access key");
            return false;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiUrl}/photos/random");
            request.Headers.Add("Authorization", $"Client-ID {_accessKey}");
            request.Headers.Add("Accept-Version", "v1");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Online source availability check failed");
            return false;
        }
    }

    public async Task<ImageInfo?> GetRandomImageAsync()
    {
        if (string.IsNullOrWhiteSpace(_accessKey))
        {
            Log.Warning("Cannot get image: no access key configured");
            return null;
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var endpoint = !string.IsNullOrWhiteSpace(_query)
                    ? $"{_apiUrl}/photos/random?query={Uri.EscapeDataString(_query)}&orientation=landscape"
                    : $"{_apiUrl}/photos/random?orientation=landscape";

                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Add("Authorization", $"Client-ID {_accessKey}");
                request.Headers.Add("Accept-Version", "v1");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Warning("Unsplash API error: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        Log.Error("Invalid Unsplash API key");
                        return null;
                    }

                    await Task.Delay(1000 * (attempt + 1));
                    continue;
                }

                var photoData = await response.Content.ReadFromJsonAsync<UnsplashPhoto>(_jsonOptions);

                if (photoData == null)
                {
                    Log.Warning("Failed to parse Unsplash response");
                    continue;
                }

                if (_recentImageIds.Contains(photoData.Id))
                {
                    Log.Debug("Image {Id} already used recently, retrying", photoData.Id);
                    await Task.Delay(500);
                    continue;
                }

                AddToRecent(photoData.Id);

                var imageUrl = photoData.Urls?.Regular ?? photoData.Urls?.Full;
                if (string.IsNullOrEmpty(imageUrl))
                {
                    Log.Warning("No image URL in response");
                    continue;
                }

                var cachedImage = await _cacheService.CacheImageAsync(imageUrl, photoData.Id);

                cachedImage.Author = photoData.User?.Name ?? photoData.User?.Username;
                cachedImage.Description = photoData.Description ?? photoData.AltDescription;
                cachedImage.Width = photoData.Width;
                cachedImage.Height = photoData.Height;

                Log.Information("Downloaded image from Unsplash: {Id} by {Author}",
                    photoData.Id, cachedImage.Author);

                return cachedImage;
            }
            catch (HttpRequestException ex)
            {
                Log.Warning(ex, "HTTP error getting random image, attempt {Attempt}", attempt + 1);
                await Task.Delay(2000 * (attempt + 1));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting random image");
                return null;
            }
        }

        Log.Warning("Failed to get image after 3 attempts");
        return null;
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
            await Task.Delay(500);
        }

        return result;
    }

    private void AddToRecent(string imageId)
    {
        _recentImageIds.Insert(0, imageId);
        if (_recentImageIds.Count > MaxRecentImages)
        {
            _recentImageIds.RemoveRange(MaxRecentImages, _recentImageIds.Count - MaxRecentImages);
        }
    }

    public List<string> GetRecentImageIds() => new(_recentImageIds);

    private class UnsplashPhoto
    {
        public string Id { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AltDescription { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public UnsplashUrls? Urls { get; set; }
        public UnsplashUser? User { get; set; }
    }

    private class UnsplashUrls
    {
        public string? Raw { get; set; }
        public string? Full { get; set; }
        public string? Regular { get; set; }
        public string? Small { get; set; }
        public string? Thumb { get; set; }
    }

    private class UnsplashUser
    {
        public string? Username { get; set; }
        public string? Name { get; set; }
    }
}
