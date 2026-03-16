using NotificationsPro.Helpers;

namespace NotificationsPro.Tests;

public class EdgeAnchorHelperTests
{
    [Fact]
    public void ResolveFarEdgeAnchor_ReturnsFalse_WhenOnlyStartEdgeIsNear()
    {
        var anchored = EdgeAnchorHelper.ResolveFarEdgeAnchor(
            currentlyAnchoredToFarEdge: true,
            nearStartEdge: true,
            nearFarEdge: false);

        Assert.False(anchored);
    }

    [Fact]
    public void ResolveFarEdgeAnchor_ReturnsTrue_WhenOnlyFarEdgeIsNear()
    {
        var anchored = EdgeAnchorHelper.ResolveFarEdgeAnchor(
            currentlyAnchoredToFarEdge: false,
            nearStartEdge: false,
            nearFarEdge: true);

        Assert.True(anchored);
    }

    [Fact]
    public void ResolveFarEdgeAnchor_PreservesTopOrLeftAnchoring_WhenBothEdgesAreNear()
    {
        var anchored = EdgeAnchorHelper.ResolveFarEdgeAnchor(
            currentlyAnchoredToFarEdge: false,
            nearStartEdge: true,
            nearFarEdge: true);

        Assert.False(anchored);
    }

    [Fact]
    public void ResolveFarEdgeAnchor_PreservesBottomOrRightAnchoring_WhenBothEdgesAreNear()
    {
        var anchored = EdgeAnchorHelper.ResolveFarEdgeAnchor(
            currentlyAnchoredToFarEdge: true,
            nearStartEdge: true,
            nearFarEdge: true);

        Assert.True(anchored);
    }
}
