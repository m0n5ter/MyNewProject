using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MyNewProject.Views.Converters;

public class BooleanToVisibilityHiddenConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Visible : Visibility.Hidden;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}