using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;

namespace NotificationsPro.Services;

public sealed class BackgroundImageService
{
    private static readonly string CustomBackgroundsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotificationsPro",
        "backgrounds");

    private readonly ConcurrentDictionary<string, BitmapSource?> _imageCache = new(StringComparer.OrdinalIgnoreCase);

    public BitmapSource? ResolveBackgroundImage(string filePath, double hueDegrees, double brightness)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var cacheKey = $"{filePath}|{hueDegrees:F0}|{brightness:F2}";
        if (_imageCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var image = LoadAndTransform(filePath, hueDegrees, brightness);
        _imageCache[cacheKey] = image;
        return image;
    }

    public static void EnsureBackgroundsDirExists()
    {
        try { Directory.CreateDirectory(CustomBackgroundsDir); } catch { }
    }

    public static string GetCustomBackgroundsDir() => CustomBackgroundsDir;

    private static BitmapSource? LoadAndTransform(string filePath, double hueDegrees, double brightness)
    {
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var customDir = Path.GetFullPath(CustomBackgroundsDir);
            if (!fullPath.StartsWith(customDir, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!File.Exists(fullPath))
                return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 768;
            bitmap.EndInit();
            bitmap.Freeze();

            var formatted = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Bgra32, null, 0);
            formatted.Freeze();

            var pixelWidth = formatted.PixelWidth;
            var pixelHeight = formatted.PixelHeight;
            var stride = pixelWidth * 4;
            var pixels = new byte[stride * pixelHeight];
            formatted.CopyPixels(pixels, stride, 0);

            var normalizedHue = hueDegrees / 360.0;
            var normalizedBrightness = Math.Clamp(brightness, 0.2, 2.0);

            for (var index = 0; index < pixels.Length; index += 4)
            {
                var b = pixels[index];
                var g = pixels[index + 1];
                var r = pixels[index + 2];

                RgbToHsv(r, g, b, out var hue, out var saturation, out var value);
                hue = (hue + normalizedHue) % 1.0;
                if (hue < 0)
                    hue += 1.0;

                value = Math.Clamp(value * normalizedBrightness, 0.0, 1.0);
                HsvToRgb(hue, saturation, value, out r, out g, out b);

                pixels[index] = b;
                pixels[index + 1] = g;
                pixels[index + 2] = r;
            }

            var transformed = BitmapSource.Create(
                pixelWidth,
                pixelHeight,
                formatted.DpiX,
                formatted.DpiY,
                System.Windows.Media.PixelFormats.Bgra32,
                null,
                pixels,
                stride);
            transformed.Freeze();
            return transformed;
        }
        catch
        {
            return null;
        }
    }

    private static void RgbToHsv(byte rByte, byte gByte, byte bByte, out double hue, out double saturation, out double value)
    {
        var r = rByte / 255.0;
        var g = gByte / 255.0;
        var b = bByte / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        hue = 0;
        if (delta > 0.0001)
        {
            if (Math.Abs(max - r) < 0.0001)
                hue = ((g - b) / delta) % 6.0;
            else if (Math.Abs(max - g) < 0.0001)
                hue = ((b - r) / delta) + 2.0;
            else
                hue = ((r - g) / delta) + 4.0;

            hue /= 6.0;
            if (hue < 0)
                hue += 1.0;
        }

        saturation = max <= 0.0001 ? 0 : delta / max;
        value = max;
    }

    private static void HsvToRgb(double hue, double saturation, double value, out byte rByte, out byte gByte, out byte bByte)
    {
        if (saturation <= 0.0001)
        {
            var gray = (byte)Math.Round(value * 255);
            rByte = gray;
            gByte = gray;
            bByte = gray;
            return;
        }

        var scaledHue = (hue % 1.0 + 1.0) % 1.0 * 6.0;
        var sector = (int)Math.Floor(scaledHue);
        var fraction = scaledHue - sector;
        var p = value * (1.0 - saturation);
        var q = value * (1.0 - saturation * fraction);
        var t = value * (1.0 - saturation * (1.0 - fraction));

        var (r, g, b) = sector switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q)
        };

        rByte = (byte)Math.Round(r * 255);
        gByte = (byte)Math.Round(g * 255);
        bByte = (byte)Math.Round(b * 255);
    }
}
