using NotificationsPro.Helpers;

namespace NotificationsPro.Models;

public class HighlightRuleDefinition
{
    public string Keyword { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsRegex { get; set; }
    public string Scope { get; set; } = NotificationMatchScopeHelper.TitleAndBody;
    public string AppFilter { get; set; } = string.Empty;

    public HighlightRuleDefinition Clone()
    {
        return new HighlightRuleDefinition
        {
            Keyword = Keyword,
            Color = Color,
            IsRegex = IsRegex,
            Scope = NotificationMatchScopeHelper.Normalize(Scope),
            AppFilter = AppFilter
        };
    }
}
