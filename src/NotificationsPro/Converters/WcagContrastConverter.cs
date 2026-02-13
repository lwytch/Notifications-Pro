using System.Globalization;
using System.Windows.Data;
using NotificationsPro.Helpers;
using MediaColor = System.Windows.Media.Color;

namespace NotificationsPro.Converters;

/// <summary>
/// Multi-value converter: (foreground hex, background hex) → "4.8:1 AA" text.
/// </summary>
public class WcagContrastTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string fg || values[1] is not string bg)
            return string.Empty;
        return ContrastHelper.FormatRatio(fg, bg);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Multi-value converter: (foreground hex, background hex) → Brush color (green/yellow/red).
/// </summary>
public class WcagContrastColorConverter : IMultiValueConverter
{
    private static readonly System.Windows.Media.SolidColorBrush GreenBrush = new(MediaColor.FromRgb(95, 224, 157));  // Success
    private static readonly System.Windows.Media.SolidColorBrush YellowBrush = new(MediaColor.FromRgb(255, 215, 0));  // Warning
    private static readonly System.Windows.Media.SolidColorBrush RedBrush = new(MediaColor.FromRgb(255, 107, 107));    // Fail

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string fg || values[1] is not string bg)
            return RedBrush;
        try
        {
            var ratio = ContrastHelper.GetContrastRatio(fg, bg);
            if (ratio >= 4.5) return GreenBrush;
            if (ratio >= 3.0) return YellowBrush;
            return RedBrush;
        }
        catch { return RedBrush; }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
