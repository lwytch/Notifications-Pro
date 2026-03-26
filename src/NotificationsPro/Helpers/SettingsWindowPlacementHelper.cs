using System.Windows;

namespace NotificationsPro.Helpers;

internal static class SettingsWindowPlacementHelper
{
    private const double PopupFallbackWidth = 640;
    private const double PopupMinimumWidth = 320;
    private const double PopupPreferredMinimumWidth = 400;
    private const double PopupMinimumHeight = 280;
    private const double PopupPreferredMinimumHeight = 380;
    private const double PopupHeightFloorFactor = 0.55;

    public static bool HasStoredPosition(double? left, double? top)
    {
        return left.HasValue
            && top.HasValue
            && double.IsFinite(left.Value)
            && double.IsFinite(top.Value);
    }

    public static Rect CreatePopupBounds(double requestedWidth, double requestedHeight, Rect workArea, double? preferredLeft = null, double? preferredTop = null)
    {
        var maxWidth = Math.Max(PopupMinimumWidth, workArea.Width - 24);
        var minWidth = Math.Min(PopupPreferredMinimumWidth, maxWidth);
        var width = Math.Clamp(requestedWidth > 0 ? requestedWidth : PopupFallbackWidth, minWidth, maxWidth);

        var maxHeight = Math.Max(PopupMinimumHeight, workArea.Height - 24);
        var minHeight = Math.Min(PopupPreferredMinimumHeight, maxHeight);
        var preferredHeight = Math.Max(requestedHeight, workArea.Height * PopupHeightFloorFactor);
        var height = Math.Clamp(preferredHeight, minHeight, maxHeight);

        if (HasStoredPosition(preferredLeft, preferredTop))
        {
            return ClampBoundsToWorkArea(new Rect(preferredLeft!.Value, preferredTop!.Value, width, height), workArea);
        }

        return new Rect(
            workArea.Left + ((workArea.Width - width) / 2.0),
            workArea.Top + ((workArea.Height - height) / 2.0),
            width,
            height);
    }

    public static Rect ClampBoundsToWorkArea(Rect desiredBounds, Rect workArea)
    {
        var width = NormalizeDimension(desiredBounds.Width, workArea.Width);
        var height = NormalizeDimension(desiredBounds.Height, workArea.Height);

        var minLeft = workArea.Left;
        var maxLeft = Math.Max(minLeft, workArea.Right - width);
        var minTop = workArea.Top;
        var maxTop = Math.Max(minTop, workArea.Bottom - height);

        var left = HasStoredCoordinate(desiredBounds.Left)
            ? Math.Clamp(desiredBounds.Left, minLeft, maxLeft)
            : minLeft;
        var top = HasStoredCoordinate(desiredBounds.Top)
            ? Math.Clamp(desiredBounds.Top, minTop, maxTop)
            : minTop;

        return new Rect(left, top, width, height);
    }

    private static double NormalizeDimension(double requested, double maximum)
    {
        var normalizedMaximum = Math.Max(1, maximum);
        var desired = double.IsFinite(requested) && requested > 0
            ? requested
            : normalizedMaximum;

        return Math.Clamp(desired, 1, normalizedMaximum);
    }

    private static bool HasStoredCoordinate(double value)
    {
        return double.IsFinite(value);
    }
}
