using System.Windows;
using NotificationsPro.Helpers;
using Point = System.Windows.Point;

namespace NotificationsPro.Tests;

public class SnapHelperTests
{
    private static readonly Rect WorkArea = new(0, 0, 1920, 1080);

    [Fact]
    public void SnapToEdges_SnapsToLeftEdge()
    {
        var result = SnapHelper.SnapToEdges(15, 500, 380, 300, WorkArea, 20);
        Assert.Equal(0, result.X);
    }

    [Fact]
    public void SnapToEdges_SnapsToTopEdge()
    {
        var result = SnapHelper.SnapToEdges(500, 10, 380, 300, WorkArea, 20);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void SnapToEdges_SnapsToRightEdge()
    {
        var result = SnapHelper.SnapToEdges(1920 - 380 - 5, 500, 380, 300, WorkArea, 20);
        Assert.Equal(1920 - 380, result.X);
    }

    [Fact]
    public void SnapToEdges_SnapsToBottomEdge()
    {
        var result = SnapHelper.SnapToEdges(500, 1080 - 300 - 10, 380, 300, WorkArea, 20);
        Assert.Equal(1080 - 300, result.Y);
    }

    [Fact]
    public void SnapToEdges_NoSnapWhenFarFromEdge()
    {
        var result = SnapHelper.SnapToEdges(500, 500, 380, 300, WorkArea, 20);
        Assert.Equal(500, result.X);
        Assert.Equal(500, result.Y);
    }

    [Fact]
    public void SnapToEdges_SnapsToCorner()
    {
        var result = SnapHelper.SnapToEdges(5, 8, 380, 300, WorkArea, 20);
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void GetDefaultPosition_PlacesTopRight()
    {
        var pos = SnapHelper.GetDefaultPosition(380, 300, WorkArea);
        Assert.Equal(1920 - 380 - 16, pos.X);
        Assert.Equal(16, pos.Y);
    }
}
