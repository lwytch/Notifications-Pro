using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NotificationsPro.ViewModels;

public class KeywordHighlightEntry : INotifyPropertyChanged
{
    public string Keyword { get; }

    private string _color;
    public string Color
    {
        get => _color;
        set { _color = value; OnPropertyChanged(); }
    }

    private bool _isRegex;
    public bool IsRegex
    {
        get => _isRegex;
        set { _isRegex = value; OnPropertyChanged(); }
    }

    public KeywordHighlightEntry(string keyword, string color, bool isRegex = false)
    {
        Keyword = keyword;
        _color = color;
        _isRegex = isRegex;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
