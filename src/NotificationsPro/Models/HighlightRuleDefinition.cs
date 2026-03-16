using NotificationsPro.Helpers;

namespace NotificationsPro.Models;

public class HighlightRuleDefinition
{
    public string Keyword { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsRegex { get; set; }
    public string Scope { get; set; } = NotificationMatchScopeHelper.TitleAndBody;
    public string AppFilter { get; set; } = string.Empty;
    public string Animation { get; set; } = string.Empty;
    public string BorderMode { get; set; } = string.Empty;
    public double? OverlayOpacity { get; set; }
    public double? BorderThickness { get; set; }

    public HighlightRuleDefinition Clone()
    {
        return new HighlightRuleDefinition
        {
            Keyword = Keyword,
            Color = Color,
            IsRegex = IsRegex,
            Scope = NotificationMatchScopeHelper.Normalize(Scope),
            AppFilter = AppFilter,
            Animation = string.IsNullOrWhiteSpace(Animation) ? string.Empty : HighlightAnimationHelper.Normalize(Animation),
            BorderMode = string.IsNullOrWhiteSpace(BorderMode) ? string.Empty : HighlightBorderModeHelper.Normalize(BorderMode),
            OverlayOpacity = OverlayOpacity.HasValue ? Math.Clamp(OverlayOpacity.Value, 0.05, 0.80) : null,
            BorderThickness = BorderThickness.HasValue ? Math.Clamp(BorderThickness.Value, 0.5, 8.0) : null
        };
    }
}
