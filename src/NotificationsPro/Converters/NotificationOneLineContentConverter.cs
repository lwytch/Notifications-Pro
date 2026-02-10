using System.Globalization;
using System.Windows.Data;

namespace NotificationsPro.Converters;

/// <summary>
/// Builds one-line content text from title/body based on display toggles.
/// </summary>
public class NotificationOneLineContentConverter : IMultiValueConverter
{
    private const int BodyPreviewLength = 80;
    private const string Separator = " - ";

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4)
            return string.Empty;

        var options = ParseOptions(parameter);
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
        var compactBody = options.CompactBody;
        if (values.Length > index + 4 && values[index + 4] is bool explicitCompact)
            compactBody = explicitCompact;

        if (showBody && !string.IsNullOrWhiteSpace(body))
        {
            if (compactBody)
            {
                body = body.Length > BodyPreviewLength
                    ? body[..BodyPreviewLength].TrimEnd() + "..."
                    : body;
            }
        }

        var hasApp = includeApp && showApp && !string.IsNullOrWhiteSpace(app);
        var hasTitle = showTitle && !string.IsNullOrWhiteSpace(title);
        var hasBody = showBody && !string.IsNullOrWhiteSpace(body);

        if (!string.IsNullOrWhiteSpace(options.Segment))
        {
            return options.Segment switch
            {
                "app" => hasApp ? app + (hasTitle || hasBody ? Separator : string.Empty) : string.Empty,
                "title" => hasTitle ? title + (hasBody ? Separator : string.Empty) : string.Empty,
                "body" => hasBody ? body : string.Empty,
                _ => string.Empty
            };
        }

        var parts = new List<string>(3);
        if (hasApp)
            parts.Add(app);
        if (hasTitle)
            parts.Add(title);
        if (hasBody)
            parts.Add(body);

        return string.Join(" - ", parts);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static RenderOptions ParseOptions(object? parameter)
    {
        var raw = parameter?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return new RenderOptions(null, true);

        var normalized = raw.Trim().ToLowerInvariant();
        string? segment = null;
        if (normalized.Contains("segment:app", StringComparison.Ordinal))
            segment = "app";
        else if (normalized.Contains("segment:title", StringComparison.Ordinal))
            segment = "title";
        else if (normalized.Contains("segment:body", StringComparison.Ordinal))
            segment = "body";

        var compactBody = true;
        if (normalized.Contains("full", StringComparison.Ordinal))
            compactBody = false;
        else if (normalized.Contains("compact", StringComparison.Ordinal))
            compactBody = true;

        return new RenderOptions(segment, compactBody);
    }

    private sealed record RenderOptions(string? Segment, bool CompactBody);
}
