using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace NotificationsPro.Converters;

public class FallbackColorToOpacityBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var overrideColor = values.ElementAtOrDefault(0) as string;
        var fallbackColor = values.ElementAtOrDefault(1) as string ?? "#202020";
        var opacity = values.ElementAtOrDefault(2) is double o ? o : 1.0;
        var colorText = !string.IsNullOrWhiteSpace(overrideColor) ? overrideColor : fallbackColor;

        try
        {
            var color = (MediaColor)MediaColorConverter.ConvertFromString(colorText);
            color.A = (byte)(Math.Clamp(opacity, 0.0, 1.0) * 255);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            return MediaBrushes.Transparent;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
