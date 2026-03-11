using System.ComponentModel;
using System.Runtime.CompilerServices;
using NotificationsPro.Helpers;

namespace NotificationsPro.ViewModels;

public class MuteKeywordEntry : INotifyPropertyChanged
{
    public string Keyword { get; }

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
        Keyword = keyword;
        _isRegex = isRegex;
        _scope = NotificationMatchScopeHelper.Normalize(scope);
        _appFilter = appFilter?.Trim() ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
