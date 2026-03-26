namespace NotificationsPro.Helpers;

internal static class OverlayMouseWheelHelper
{
    private const int MouseWheelDelta = 120;
    private const double DefaultLineScrollPixels = 16.0;

    public static double CalculateVerticalOffsetDelta(int wheelDelta, int wheelScrollLines, double viewportHeight)
    {
        if (wheelDelta == 0 || wheelScrollLines == 0)
            return 0;

        var effectiveViewportHeight = double.IsFinite(viewportHeight) && viewportHeight > 0
            ? viewportHeight
            : 0;
        var stepSize = wheelScrollLines < 0
            ? effectiveViewportHeight
            : Math.Max(1, wheelScrollLines) * DefaultLineScrollPixels;

        if (stepSize <= 0)
            return 0;

        return (-wheelDelta / (double)MouseWheelDelta) * stepSize;
    }
}
