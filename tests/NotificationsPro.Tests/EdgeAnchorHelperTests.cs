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

    [Fact]
    public void ShouldPreserveFarEdgeDuringResize_ReturnsFalse_WhenOnlyStartEdgeIsNear()
    {
        var anchored = EdgeAnchorHelper.ShouldPreserveFarEdgeDuringResize(
            currentlyAnchoredToFarEdge: false,
            nearStartEdgeBeforeResize: true,
            nearFarEdgeBeforeResize: false);

        Assert.False(anchored);
    }

    [Fact]
    public void ShouldPreserveFarEdgeDuringResize_ReturnsTrue_WhenOnlyFarEdgeIsNear()
    {
        var anchored = EdgeAnchorHelper.ShouldPreserveFarEdgeDuringResize(
            currentlyAnchoredToFarEdge: false,
            nearStartEdgeBeforeResize: false,
            nearFarEdgeBeforeResize: true);

        Assert.True(anchored);
    }

    [Fact]
    public void ShouldPreserveFarEdgeDuringResize_ReturnsFalse_WhenBothEdgesAreNearButWindowWasTopAnchored()
    {
        var anchored = EdgeAnchorHelper.ShouldPreserveFarEdgeDuringResize(
            currentlyAnchoredToFarEdge: false,
            nearStartEdgeBeforeResize: true,
            nearFarEdgeBeforeResize: true);

        Assert.False(anchored);
    }

    [Fact]
    public void ShouldPreserveFarEdgeDuringResize_ReturnsTrue_WhenWindowWasAlreadyBottomAnchored()
    {
        var anchored = EdgeAnchorHelper.ShouldPreserveFarEdgeDuringResize(
            currentlyAnchoredToFarEdge: true,
            nearStartEdgeBeforeResize: true,
            nearFarEdgeBeforeResize: true);

        Assert.True(anchored);
    }

    [Fact]
    public void ShouldPreserveFarEdgeDuringResize_KeepsExistingFarEdgeAnchor_WhenWindowIsOffsetFromEdges()
    {
        var anchored = EdgeAnchorHelper.ShouldPreserveFarEdgeDuringResize(
            currentlyAnchoredToFarEdge: true,
            nearStartEdgeBeforeResize: false,
            nearFarEdgeBeforeResize: false);

        Assert.True(anchored);
    }
}
