using System.IO;
using System.Windows;
using Serilog;
using WallpaperManager.Services;
using WallpaperManager.Services.Interfaces;
using WallpaperManager.ViewModels;
using WallpaperManager.Views;

namespace WallpaperManager;

public partial class App : Application
{
    private ISettingsService? _settingsService;
    private ICacheService? _cacheService;
    private ITimerService? _timerService;
    private IImageService? _imageService;
    private IWallpaperService? _wallpaperService;
    private TrayService? _trayService;
    private MainViewModel? _mainViewModel;
    private MainWindow? _mainWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SetupExceptionHandling();
        InitializeLogging();

        Log.Information("Application starting...");

        try
        {
            await InitializeServicesAsync();

            _mainWindow = new MainWindow();
            _mainWindow.DataContext = _mainViewModel;

            _trayService!.Initialize(_mainWindow);
            _trayService.ShowSettingsRequested += OnShowSettingsRequested;
            _trayService.ShowPreviewRequested += OnShowPreviewRequested;
            _trayService.ExitRequested += OnExitRequested;

            _mainWindow.Show();

            var settings = _settingsService!.Settings;
            if (settings.StartWithWindows)
            {
                _wallpaperService!.StartAutoChange();
            }

            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show($"Ошибка запуска приложения: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Log.Fatal(ex, "Unhandled domain exception");
            Environment.Exit(1);
        };

        DispatcherUnhandledException += (s, e) =>
        {
            Log.Error(e.Exception, "Unhandled dispatcher exception");
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }

    private void InitializeLogging()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDirectory = Path.Combine(appDataPath, "WallpaperManager", "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "wallpaper-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private async Task InitializeServicesAsync()
    {
        _settingsService = new SettingsService();
        await _settingsService.LoadAsync();

        _cacheService = new CacheService();
        _timerService = new TimerService();
        _imageService = new ImageService(_cacheService);
        _wallpaperService = new WallpaperService(
            _imageService,
            _cacheService,
            _timerService,
            _settingsService);

        _trayService = new TrayService(_wallpaperService, _settingsService);

        var settings = _settingsService.Settings;

        _imageService.SetSource(settings.CurrentSource);
        _imageService.SetLocalFolders(settings.LocalFolders);
        _imageService.SetOnlineQuery(settings.OnlineQuery);
        _imageService.SetOnlineCategory(settings.OnlineCategory);
        _imageService.SetOnlineAccessKey(settings.UnsplashAccessKey);

        _mainViewModel = new MainViewModel(
            _wallpaperService,
            _imageService,
            _cacheService,
            _settingsService,
            _trayService);

        Log.Information("Services initialized");
    }

    private void OnShowSettingsRequested(object? sender, EventArgs e)
    {
        ShowSettingsWindow();
    }

    private void OnShowPreviewRequested(object? sender, EventArgs e)
    {
        _mainWindow?.Show();
        _mainWindow?.Activate();
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        Log.Information("Application exit requested");
        Shutdown();
    }

    private void ShowSettingsWindow()
    {
        var settingsViewModel = new SettingsViewModel(
            _settingsService!,
            _imageService!,
            _timerService!);

        settingsViewModel.SettingsSaved += (s, e) =>
        {
            _mainViewModel?.RefreshSettings();
        };

        var settingsWindow = new SettingsWindow
        {
            DataContext = settingsViewModel,
            Owner = _mainWindow
        };

        settingsWindow.ShowDialog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting with code {ExitCode}", e.ApplicationExitCode);
        _trayService?.Dispose();
        _timerService?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
