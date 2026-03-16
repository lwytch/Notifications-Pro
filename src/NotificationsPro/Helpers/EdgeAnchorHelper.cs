namespace NotificationsPro.Helpers;

public static class EdgeAnchorHelper
{
    public static bool ResolveFarEdgeAnchor(
        bool currentlyAnchoredToFarEdge,
        bool nearStartEdge,
        bool nearFarEdge)
    {
        if (nearStartEdge && nearFarEdge)
            return currentlyAnchoredToFarEdge;

        return nearFarEdge;
    }
}
