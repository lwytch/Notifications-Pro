using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using NotificationsPro.Helpers;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace NotificationsPro.Converters;

/// <summary>
/// Blends a per-app tint color into the card background.
/// Parameters: AppName, PerAppTintEnabled, PerAppTintOpacity, BackgroundColor, BackgroundOpacity
/// </summary>
public class AppTintBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Fallback: return standard bg
        if (values.Length < 5)
            return new SolidColorBrush(Color.FromArgb(230, 30, 30, 46));

        var appName = values[0] as string ?? string.Empty;
        var tintEnabled = values[1] is true;
        var tintOpacity = values[2] is double t ? t : 0.15;
        var bgHex = values[3] as string ?? "#1E1E2E";
        var bgOpacity = values[4] is double o ? o : 0.92;

        try
        {
            var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);

            if (tintEnabled && !string.IsNullOrWhiteSpace(appName))
            {
                var tintHex = AppTintHelper.GetTintColor(appName);
                var tintColor = (Color)ColorConverter.ConvertFromString(tintHex);

                // Blend: result = bg * (1 - tintOpacity) + tint * tintOpacity
                bgColor = Color.FromRgb(
                    (byte)(bgColor.R * (1 - tintOpacity) + tintColor.R * tintOpacity),
                    (byte)(bgColor.G * (1 - tintOpacity) + tintColor.G * tintOpacity),
                    (byte)(bgColor.B * (1 - tintOpacity) + tintColor.B * tintOpacity));
            }

            bgColor.A = (byte)(bgOpacity * 255);
            return new SolidColorBrush(bgColor);
        }
        catch
        {
            return new SolidColorBrush(Color.FromArgb(230, 30, 30, 46));
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
