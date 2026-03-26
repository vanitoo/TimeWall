using System.Globalization;
using System.Windows.Data;

namespace WallpaperManager.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter == null)
            return false;

        var paramString = parameter.ToString();
        if (string.IsNullOrEmpty(paramString))
            return false;

        if (!Enum.IsDefined(value.GetType(), value))
            return false;

        var parameterValue = Enum.Parse(value.GetType(), paramString);
        return parameterValue.Equals(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter == null)
            return Binding.DoNothing;

        if (value is bool boolValue && boolValue)
        {
            var paramString = parameter.ToString();
            if (!string.IsNullOrEmpty(paramString))
            {
                return Enum.Parse(targetType, paramString);
            }
        }

        return Binding.DoNothing;
    }
}
