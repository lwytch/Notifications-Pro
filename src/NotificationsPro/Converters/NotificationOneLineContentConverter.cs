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

        var index = 0;
        var includeApp = false;
        var showApp = false;
        var app = string.Empty;

        // Extended mode optionally includes app name values first.
        if (values.Length >= 6)
        {
            includeApp = true;
            showApp = values[0] is bool appEnabled && appEnabled;
            app = values[1]?.ToString()?.Trim() ?? string.Empty;
            index = 2;
        }

        var showTitle = values[index] is bool titleEnabled && titleEnabled;
        var title = values[index + 1]?.ToString()?.Trim() ?? string.Empty;
        var showBody = values[index + 2] is bool bodyEnabled && bodyEnabled;
        var body = values[index + 3]?.ToString()?.Trim() ?? string.Empty;
        var compactBody = !string.Equals(parameter?.ToString(), "full", StringComparison.OrdinalIgnoreCase);
        if (values.Length > index + 4 && values[index + 4] is bool explicitCompact)
            compactBody = explicitCompact;

        var parts = new List<string>(3);

        if (includeApp && showApp && !string.IsNullOrWhiteSpace(app))
            parts.Add(app);

        if (showTitle && !string.IsNullOrWhiteSpace(title))
            parts.Add(title);

        if (showBody && !string.IsNullOrWhiteSpace(body))
        {
            if (compactBody)
            {
                body = body.Length > BodyPreviewLength
                    ? body[..BodyPreviewLength].TrimEnd() + "..."
                    : body;
            }

            parts.Add(body);
        }

        return string.Join(" - ", parts);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
