namespace NotificationsPro.Models;

public class OverlayLaneChoice
{
    public string Id { get; }
    public string DisplayName { get; }

    public OverlayLaneChoice(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
}
