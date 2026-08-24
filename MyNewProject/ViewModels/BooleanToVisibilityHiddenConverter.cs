using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MyNewProject.ViewModels;

public class BooleanToVisibilityHiddenConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool and true ? Visibility.Visible : Visibility.Hidden;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}