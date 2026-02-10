using System.Globalization;
using System.Windows.Data;

namespace NotificationsPro.Converters;

/// <summary>
/// Builds one-line content text from title/body based on display toggles.
/// </summary>
public class NotificationOneLineContentConverter : IMultiValueConverter
{
    private const int BodyPreviewLength = 80;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4)
            return string.Empty;

        var showTitle = values[0] is bool titleEnabled && titleEnabled;
        var title = values[1]?.ToString()?.Trim() ?? string.Empty;
        var showBody = values[2] is bool bodyEnabled && bodyEnabled;
        var body = values[3]?.ToString()?.Trim() ?? string.Empty;

        if (showTitle && !string.IsNullOrWhiteSpace(title))
        {
            if (!showBody || string.IsNullOrWhiteSpace(body))
                return title;

            var compactBody = body.Length > BodyPreviewLength
                ? body[..BodyPreviewLength].TrimEnd() + "..."
                : body;
            return $"{title} - {compactBody}";
        }

        if (showBody && !string.IsNullOrWhiteSpace(body))
            return body;

        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
