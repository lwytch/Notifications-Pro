using System.Windows;
using Point = System.Windows.Point;

namespace NotificationsPro.Helpers;

public static class SnapHelper
{
    /// <summary>
    /// Adjusts a window position to snap to screen edges if within the snap distance.
    /// </summary>
    public static Point SnapToEdges(double left, double top, double width, double height,
        Rect workArea, double snapDistance)
    {
        double snappedLeft = left;
        double snappedTop = top;

        // Snap left edge
        if (Math.Abs(left - workArea.Left) <= snapDistance)
            snappedLeft = workArea.Left;

        // Snap right edge
        if (Math.Abs((left + width) - workArea.Right) <= snapDistance)
            snappedLeft = workArea.Right - width;

        // Snap top edge
        if (Math.Abs(top - workArea.Top) <= snapDistance)
            snappedTop = workArea.Top;

        // Snap bottom edge
        if (Math.Abs((top + height) - workArea.Bottom) <= snapDistance)
            snappedTop = workArea.Bottom - height;

        return new Point(snappedLeft, snappedTop);
    }

    /// <summary>
    /// Positions a window in the default location (top-right corner with margin).
    /// </summary>
    public static Point GetDefaultPosition(double width, double height, Rect workArea)
    {
        return new Point(
            workArea.Right - width - 16,
            workArea.Top + 16
        );
    }
}
