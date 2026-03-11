using System.Globalization;
using System.Windows.Data;
using NotificationsPro.Helpers;

namespace NotificationsPro.Converters;

public class VoiceAccessAutomationNameConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var title = values.Length > 0 ? values[0]?.ToString() : null;
        var body = values.Length > 1 ? values[1]?.ToString() : null;
        var relativeTimestamp = values.Length > 2 ? values[2]?.ToString() : null;
        var receivedAt = values.Length > 3 && values[3] is DateTime dt ? dt : DateTime.Now;
        var mode = values.Length > 4 ? values[4]?.ToString() : null;
        var timestampDisplayMode = values.Length > 5 ? values[5]?.ToString() : null;

        return VoiceAccessTextFormatter.BuildAutomationName(
            title,
            body,
            relativeTimestamp,
            receivedAt,
            mode,
            timestampDisplayMode,
            culture);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
