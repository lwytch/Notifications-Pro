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

    public KeywordHighlightEntry(string keyword, string color)
    {
        Keyword = keyword;
        _color = color;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
