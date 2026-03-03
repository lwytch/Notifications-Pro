using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    public MuteKeywordEntry(string keyword, bool isRegex = false)
    {
        Keyword = keyword;
        _isRegex = isRegex;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
