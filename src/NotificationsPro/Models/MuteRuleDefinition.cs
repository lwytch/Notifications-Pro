using NotificationsPro.Helpers;

namespace NotificationsPro.Models;

public class MuteRuleDefinition
{
    public string Keyword { get; set; } = string.Empty;
    public bool IsRegex { get; set; }
    public string Scope { get; set; } = NotificationMatchScopeHelper.TitleAndBody;
    public string AppFilter { get; set; } = string.Empty;

    public MuteRuleDefinition Clone()
    {
        return new MuteRuleDefinition
        {
            Keyword = Keyword,
            IsRegex = IsRegex,
            Scope = NotificationMatchScopeHelper.Normalize(Scope),
            AppFilter = AppFilter
        };
    }
}
