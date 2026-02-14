using System.Globalization;
using System.Windows.Data;
using NotificationsPro.Services;
using NotificationsPro.Models;

namespace NotificationsPro.Converters;

/// <summary>
/// Multi-value converter that resolves an icon for a notification card.
/// Bindings: [0] = AppName (string from NotificationItem),
///           [1] = IconService (from OverlayViewModel),
///           [2] = ShowNotificationIcons (bool, triggers re-evaluation on change)
/// </summary>
public class AppIconConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3) return null;
        if (values[0] is not string appName) return null;
        if (values[1] is not IconService iconService) return null;
        if (values[2] is not AppSettings settings) return null;

        return iconService.ResolveIcon(appName, settings);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
