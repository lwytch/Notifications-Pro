namespace NotificationsPro.Models;

public sealed class NarrationVoiceOption
{
    public NarrationVoiceOption(string id, string displayLabel, string language, bool isSystemDefault = false)
    {
        Id = id ?? string.Empty;
        DisplayLabel = displayLabel ?? string.Empty;
        Language = language ?? string.Empty;
        IsSystemDefault = isSystemDefault;
    }

    public string Id { get; }
    public string DisplayLabel { get; }
    public string Language { get; }
    public bool IsSystemDefault { get; }

    public override string ToString() => DisplayLabel;
}
