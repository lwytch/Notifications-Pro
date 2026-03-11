using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace NotificationsPro.Converters;

public class FallbackColorToBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var colorText = values
            .Select(value => value as string)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? "#FFFFFF";

        try
        {
            var color = (MediaColor)MediaColorConverter.ConvertFromString(colorText);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            return MediaBrushes.White;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
