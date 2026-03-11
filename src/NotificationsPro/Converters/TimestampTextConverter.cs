using System.Globalization;
using System.Windows.Data;
using NotificationsPro.Helpers;

namespace NotificationsPro.Converters;

/// <summary>
/// Formats notification timestamp text according to the selected display mode.
/// </summary>
public class TimestampTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var relative = values.Length > 0 ? values[0]?.ToString() : string.Empty;
        var receivedAt = values.Length > 1 && values[1] is DateTime dt ? dt : DateTime.Now;
        var mode = values.Length > 2 ? values[2]?.ToString() : "Relative";
        return TimestampTextFormatter.Format(relative, receivedAt, mode, culture);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
