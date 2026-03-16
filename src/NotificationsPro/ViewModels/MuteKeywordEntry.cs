using System.ComponentModel;
using System.Runtime.CompilerServices;
using NotificationsPro.Helpers;

namespace NotificationsPro.ViewModels;

public class MuteKeywordEntry : INotifyPropertyChanged
{
    private string _keyword;
    public string Keyword
    {
        get => _keyword;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.Equals(_keyword, normalized, StringComparison.Ordinal))
                return;

            _keyword = normalized;
            OnPropertyChanged();
        }
    }

    private bool _isRegex;
    public bool IsRegex
    {
        get => _isRegex;
        set { _isRegex = value; OnPropertyChanged(); }
    }

    private string _scope = NotificationMatchScopeHelper.TitleAndBody;
    public string Scope
    {
        get => _scope;
        set
        {
            _scope = NotificationMatchScopeHelper.Normalize(value);
            OnPropertyChanged();
        }
    }

    private string _appFilter = string.Empty;
    public string AppFilter
    {
        get => _appFilter;
        set
        {
            _appFilter = value?.Trim() ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public MuteKeywordEntry(
        string keyword,
        bool isRegex = false,
        string? scope = null,
        string? appFilter = null)
    {
        _keyword = keyword?.Trim() ?? string.Empty;
        _isRegex = isRegex;
        _scope = NotificationMatchScopeHelper.Normalize(scope);
        _appFilter = appFilter?.Trim() ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
