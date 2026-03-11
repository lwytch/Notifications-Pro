using System.Globalization;
using System.Windows.Data;
using NotificationsPro.Services;

namespace NotificationsPro.Converters;

public class NotificationBackgroundImageSourceConverter : IMultiValueConverter
{
    private readonly BackgroundImageService _backgroundImageService = new();

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var path = values.ElementAtOrDefault(0) as string ?? string.Empty;
        var hue = values.ElementAtOrDefault(1) is double h ? h : 0.0;
        var brightness = values.ElementAtOrDefault(2) is double b ? b : 1.0;

        return _backgroundImageService.ResolveBackgroundImage(path, hue, brightness);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
