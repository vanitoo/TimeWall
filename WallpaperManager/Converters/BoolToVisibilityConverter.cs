using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WallpaperManager.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        var invert = parameter?.ToString()?.ToLower() == "invert";

        if (invert)
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = (Visibility)value;
        var invert = parameter?.ToString()?.ToLower() == "invert";

        var result = visibility == Visibility.Visible;
        return invert ? !result : result;
    }
}
