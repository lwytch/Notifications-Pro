using System.Globalization;
using System.Windows.Data;

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

        if (string.Equals(mode, "Time", StringComparison.OrdinalIgnoreCase))
            return receivedAt.ToString("HH:mm", culture);

        if (string.Equals(mode, "DateTime", StringComparison.OrdinalIgnoreCase))
            return receivedAt.ToString("g", culture);

        return string.IsNullOrWhiteSpace(relative) ? "just now" : relative;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
