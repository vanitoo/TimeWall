using System.ComponentModel;
using System.Windows;
using WallpaperManager.Services;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.Views;

public partial class MainWindow : Window
{
    private bool _minimizeToTray = true;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        var settingsService = App.Current.Resources["SettingsService"] as ISettingsService
            ?? GetSettingsService();

        if (settingsService?.Settings.MinimizeToTray == true)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _minimizeToTray)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    private ISettingsService? GetSettingsService()
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            return GetType()
                .Assembly
                .GetType("WallpaperManager.Services.SettingsService")
                ?.GetConstructors()[0]
                .Invoke(null) as ISettingsService;
        }
        return null;
    }
}
