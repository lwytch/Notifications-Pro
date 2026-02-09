using System.Reflection;
using System.Windows;
using NotificationsPro.ViewModels;
using WinForms = System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;

namespace NotificationsPro.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnPickColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string propertyName }) return;
        if (DataContext == null) return;

        var property = DataContext.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public);
        if (property == null || property.PropertyType != typeof(string)) return;

        var currentHex = property.GetValue(DataContext) as string ?? "#FFFFFF";
        var start = ParseHex(currentHex);

        using var dialog = new WinForms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
            Color = System.Drawing.Color.FromArgb(start.A, start.R, start.G, start.B)
        };

        if (dialog.ShowDialog() != WinForms.DialogResult.OK)
            return;

        var selected = dialog.Color;
        var hex = $"#{selected.R:X2}{selected.G:X2}{selected.B:X2}";
        property.SetValue(DataContext, hex);
    }

    private static MediaColor ParseHex(string hex)
    {
        try
        {
            return (MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return System.Windows.Media.Colors.White;
        }
    }
}
