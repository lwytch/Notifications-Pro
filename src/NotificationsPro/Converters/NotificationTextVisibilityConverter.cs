using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NotificationsPro.Converters;

/// <summary>
/// Shows text only when the display toggle is enabled and the text is non-empty.
/// </summary>
public class NotificationTextVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Visibility.Collapsed;

        var isEnabled = values[0] is bool b && b;
        var text = values[1]?.ToString();

        return isEnabled && !string.IsNullOrWhiteSpace(text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
