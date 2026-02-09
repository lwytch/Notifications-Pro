using System.Globalization;
using System.Windows.Data;

namespace NotificationsPro.Converters;

/// <summary>
/// Builds one-line content text from title/body based on display toggles.
/// </summary>
public class NotificationOneLineContentConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4)
            return string.Empty;

        var showTitle = values[0] is bool titleEnabled && titleEnabled;
        var title = values[1]?.ToString()?.Trim() ?? string.Empty;
        var showBody = values[2] is bool bodyEnabled && bodyEnabled;
        var body = values[3]?.ToString()?.Trim() ?? string.Empty;

        var parts = new List<string>(2);
        if (showTitle && !string.IsNullOrWhiteSpace(title))
            parts.Add(title);

        if (showBody && !string.IsNullOrWhiteSpace(body))
        {
            if (parts.Count == 0)
                parts.Add(body);
            else
                parts.Add($"- {body}");
        }

        return string.Join(" ", parts);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
