using System.Runtime.InteropServices;

namespace WallpaperManager.Helpers;

public static class Win32Interop
{
    public enum WallpaperStyle : int
    {
        Fill = 0,
        Fit = 1,
        Stretch = 2,
        Tile = 3,
        Center = 4,
        Span = 5
    }

    public enum TileWallpaper : int
    {
        NoTile = 0,
        Tile = 1
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SystemParametersInfo(
        int uAction,
        int uParam,
        string lpvParam,
        int fuWinIni);

    [DllImport("user32.dll")]
    public static extern bool SystemParametersInfo(
        uint uiAction,
        uint uiParam,
        ref uint pvParam,
        uint fWinIni);

    public const int SPI_SETDESKWALLPAPER = 0x0014;
    public const int SPIF_UPDATEINIFILE = 0x01;
    public const int SPIF_SENDCHANGE = 0x02;

    public static void SetWallpaper(string imagePath, WallpaperStyle style = WallpaperStyle.Fill)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Desktop", true);

            if (key == null)
                throw new InvalidOperationException("Cannot open desktop registry key");

            var wallPaperStyle = style switch
            {
                WallpaperStyle.Fill => "10",
                WallpaperStyle.Fit => "6",
                WallpaperStyle.Stretch => "2",
                WallpaperStyle.Tile => "1",
                WallpaperStyle.Center => "0",
                WallpaperStyle.Span => "22",
                _ => "10"
            };

            var tileWallpaper = style == WallpaperStyle.Tile
                ? ((int)TileWallpaper.Tile).ToString()
                : ((int)TileWallpaper.NoTile).ToString();

            key.SetValue("WallpaperStyle", wallPaperStyle);
            key.SetValue("TileWallpaper", tileWallpaper);

            var result = SystemParametersInfo(
                SPI_SETDESKWALLPAPER,
                0,
                imagePath,
                SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            if (result == 0)
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to set wallpaper. Win32 error: {error}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set wallpaper: {ex.Message}", ex);
        }
    }

    public static string? GetCurrentWallpaper()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"Control Panel\Desktop", false);

        return key?.GetValue("WallPaper") as string;
    }
}
