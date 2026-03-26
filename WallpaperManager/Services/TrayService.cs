using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Serilog;
using WallpaperManager.Models;

namespace WallpaperManager.Services;

public class TrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly IWallpaperService _wallpaperService;
    private readonly ISettingsService _settingsService;
    private Window? _mainWindow;
    private bool _disposed;

    public event EventHandler? ShowSettingsRequested;
    public event EventHandler? ShowPreviewRequested;
    public event EventHandler? ExitRequested;

    public TrayService(IWallpaperService wallpaperService, ISettingsService settingsService)
    {
        _wallpaperService = wallpaperService;
        _settingsService = settingsService;
    }

    public void Initialize(Window mainWindow)
    {
        _mainWindow = mainWindow;

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Wallpaper Manager",
            Visibility = Visibility.Visible
        };

        try
        {
            var iconStream = Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/icon.ico"))?.Stream;

            if (iconStream != null)
            {
                _trayIcon.Icon = new Icon(iconStream);
            }
            else
            {
                _trayIcon.Icon = SystemIcons.Application;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load tray icon, using default");
            _trayIcon.Icon = SystemIcons.Application;
        }

        _trayIcon.TrayMouseDoubleClick += OnTrayDoubleClick;
        _trayIcon.ContextMenu = CreateContextMenu();

        Log.Information("Tray service initialized");
    }

    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        var nextImageItem = new MenuItem { Header = "Следующее фото" };
        nextImageItem.Click += (s, e) => _wallpaperService.ChangeNow();
        menu.Items.Add(nextImageItem);

        var previewItem = new MenuItem { Header = "Предпросмотр" };
        previewItem.Click += (s, e) => ShowPreviewRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(previewItem);

        menu.Items.Add(new Separator());

        var settingsItem = new MenuItem { Header = "Настройки" };
        settingsItem.Click += (s, e) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Separator());

        var statusItem = new MenuItem
        {
            Header = _wallpaperService.IsRunning ? "Автосмена: ВКЛ" : "Автосмена: ВЫКЛ",
            IsEnabled = false
        };
        menu.Items.Add(statusItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Выход" };
        exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OnTrayDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowPreviewRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ShowNotification(string title, string message, BalloonIcon icon = BalloonIcon.Info)
    {
        _trayIcon?.ShowBalloonTip(title, message, icon);
    }

    public void UpdateStatus()
    {
        if (_trayIcon?.ContextMenu != null)
        {
            var statusItem = _trayIcon.ContextMenu.Items
                .OfType<MenuItem>()
                .FirstOrDefault(m => m.Header?.ToString()?.StartsWith("Автосмена") == true);

            if (statusItem != null)
            {
                statusItem.Header = _wallpaperService.IsRunning
                    ? "Автосмена: ВКЛ"
                    : "Автосмена: ВЫКЛ";
            }
        }
    }

    public void RequestShowSettings() => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
    public void RequestShowPreview() => ShowPreviewRequested?.Invoke(this, EventArgs.Empty);
    public void RequestExit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        if (!_disposed)
        {
            _trayIcon?.Dispose();
            _disposed = true;
        }
    }
}
