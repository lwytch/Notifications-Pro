using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using NotificationsPro.Models;

namespace NotificationsPro.Services;

/// <summary>
/// Resolves notification icons per app name.
/// Icons are assigned per-app (not per-notification) — all notifications from the same app share the same icon.
/// In-memory icon cache (RAM only, never persisted — privacy safe).
/// </summary>
public class IconService
{
    private static readonly string CustomIconsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotificationsPro", "icons");

    private readonly ConcurrentDictionary<string, ImageSource?> _iconCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DrawingImage?> _geometryCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolve an icon for the given app name based on current settings.
    /// Returns null if no icon should be shown.
    /// </summary>
    public ImageSource? ResolveIcon(string appName, AppSettings settings)
    {
        if (!settings.ShowNotificationIcons) return null;

        // Check per-app override
        var iconKey = settings.DefaultIconPreset;
        if (!string.IsNullOrWhiteSpace(appName) && settings.PerAppIcons.TryGetValue(appName, out var appIcon))
            iconKey = appIcon;

        if (string.IsNullOrWhiteSpace(iconKey) || iconKey == "None") return null;

        // Check if it's a built-in preset
        if (IconPreset.BuiltInIcons.TryGetValue(iconKey, out var geometryPath) && !string.IsNullOrEmpty(geometryPath))
            return GetOrCreateGeometryIcon(iconKey, geometryPath, settings);

        // Check if it's a custom image file path
        return GetOrCreateCustomIcon(iconKey);
    }

    private DrawingImage? GetOrCreateGeometryIcon(string presetName, string geometryPath, AppSettings settings)
    {
        var cacheKey = $"{presetName}_{settings.AccentColor}";
        if (_geometryCache.TryGetValue(cacheKey, out var cached)) return cached;

        try
        {
            var geometry = Geometry.Parse(geometryPath);
            var accentColor = (MediaColor)MediaColorConverter.ConvertFromString(settings.AccentColor);
            var brush = new SolidColorBrush(accentColor);
            brush.Freeze();

            var drawing = new GeometryDrawing(brush, null, geometry);
            drawing.Freeze();

            var image = new DrawingImage(drawing);
            image.Freeze();

            _geometryCache[cacheKey] = image;
            return image;
        }
        catch
        {
            return null;
        }
    }

    private ImageSource? GetOrCreateCustomIcon(string filePath)
    {
        if (_iconCache.TryGetValue(filePath, out var cached)) return cached;

        try
        {
            if (!File.Exists(filePath)) return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.DecodePixelWidth = 128; // Pre-scale for performance
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            _iconCache[filePath] = bitmap;
            return bitmap;
        }
        catch
        {
            _iconCache[filePath] = null;
            return null;
        }
    }

    /// <summary>
    /// Clear the icon cache (e.g., when accent color changes).
    /// </summary>
    public void ClearCache()
    {
        _iconCache.Clear();
        _geometryCache.Clear();
    }

    public static void EnsureIconsDirExists()
    {
        try { Directory.CreateDirectory(CustomIconsDir); } catch { }
    }

    public static string GetCustomIconsDir() => CustomIconsDir;
}
