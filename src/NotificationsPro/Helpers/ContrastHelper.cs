namespace NotificationsPro.Helpers;

/// <summary>
/// WCAG 2.1 contrast ratio calculations.
/// </summary>
public static class ContrastHelper
{
    public static double GetContrastRatio(string hexForeground, string hexBackground)
    {
        var fgLuminance = GetRelativeLuminance(hexForeground);
        var bgLuminance = GetRelativeLuminance(hexBackground);
        var lighter = Math.Max(fgLuminance, bgLuminance);
        var darker = Math.Min(fgLuminance, bgLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    public static string GetWcagLevel(double ratio)
    {
        if (ratio >= 7.0) return "AAA";
        if (ratio >= 4.5) return "AA";
        if (ratio >= 3.0) return "AA Large";
        return "Fail";
    }

    public static string FormatRatio(string hexForeground, string hexBackground)
    {
        try
        {
            var ratio = GetContrastRatio(hexForeground, hexBackground);
            var level = GetWcagLevel(ratio);
            return $"{ratio:F1}:1 {level}";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static double GetRelativeLuminance(string hex)
    {
        var (r, g, b) = ParseHexToRgb(hex);
        var rLinear = Linearize(r / 255.0);
        var gLinear = Linearize(g / 255.0);
        var bLinear = Linearize(b / 255.0);
        return 0.2126 * rLinear + 0.7152 * gLinear + 0.0722 * bLinear;
    }

    private static double Linearize(double c)
        => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    internal static (byte R, byte G, byte B) ParseHexToRgb(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return (255, 255, 255);
        if (!byte.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) ||
            !byte.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g) ||
            !byte.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b))
            return (255, 255, 255);
        return (r, g, b);
    }
}
