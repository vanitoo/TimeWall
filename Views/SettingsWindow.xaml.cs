using System.Windows;
using WallpaperManager.ViewModels;

namespace WallpaperManager.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SettingsViewModel vm)
        {
            vm.SettingsSaved += (s, args) => { DialogResult = true; Close(); };
            vm.SettingsCancelled += (s, args) => { DialogResult = false; Close(); };
        }
    }
}
