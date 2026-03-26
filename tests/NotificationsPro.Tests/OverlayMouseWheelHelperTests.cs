using NotificationsPro.Helpers;

namespace NotificationsPro.Tests;

public class OverlayMouseWheelHelperTests
{
    [Fact]
    public void CalculateVerticalOffsetDelta_ScrollsDown_ForNegativeWheelDelta()
    {
        var delta = OverlayMouseWheelHelper.CalculateVerticalOffsetDelta(-120, 3, 400);

        Assert.Equal(48, delta);
    }

    [Fact]
    public void CalculateVerticalOffsetDelta_ScrollsUp_ForPositiveWheelDelta()
    {
        var delta = OverlayMouseWheelHelper.CalculateVerticalOffsetDelta(120, 3, 400);

        Assert.Equal(-48, delta);
    }

    [Fact]
    public void CalculateVerticalOffsetDelta_UsesViewportHeight_ForPageScrollSetting()
    {
        var delta = OverlayMouseWheelHelper.CalculateVerticalOffsetDelta(-120, -1, 512);

        Assert.Equal(512, delta);
    }

    [Fact]
    public void CalculateVerticalOffsetDelta_ReturnsZero_WhenScrollingDisabled()
    {
        Assert.Equal(0, OverlayMouseWheelHelper.CalculateVerticalOffsetDelta(-120, 0, 400));
        Assert.Equal(0, OverlayMouseWheelHelper.CalculateVerticalOffsetDelta(0, 3, 400));
    }
}
