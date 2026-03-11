using NotificationsPro.Helpers;

namespace NotificationsPro.Models;

public class NarrationRuleDefinition
{
    public string Keyword { get; set; } = string.Empty;
    public bool IsRegex { get; set; }
    public string Scope { get; set; } = NotificationMatchScopeHelper.TitleAndBody;
    public string AppFilter { get; set; } = string.Empty;
    public string Action { get; set; } = NarrationRuleActionHelper.ReadAloud;
    public string ReadMode { get; set; } = NarrationRuleReadModeHelper.UseGlobal;

    public NarrationRuleDefinition Clone()
    {
        return new NarrationRuleDefinition
        {
            Keyword = Keyword,
            IsRegex = IsRegex,
            Scope = NotificationMatchScopeHelper.Normalize(Scope),
            AppFilter = AppFilter,
            Action = NarrationRuleActionHelper.Normalize(Action),
            ReadMode = NarrationRuleReadModeHelper.Normalize(ReadMode)
        };
    }
}
