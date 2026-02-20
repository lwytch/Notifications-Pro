using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using NotificationsPro.Services;
using WpfInput = System.Windows.Input;
using NotificationsPro.ViewModels;
using WinForms = System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;

namespace NotificationsPro.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsManager? _settingsManager;
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmUseImmersiveDarkModeLegacy = 19;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public SettingsWindow(SettingsViewModel viewModel, SettingsManager? settingsManager = null)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsManager = settingsManager;

        Loaded += OnLoaded;
        Closing += OnClosing;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (IsPopupDisplayMode())
            return;

        ApplyWindowedTitleBarTheme();

        // Restore saved window position
        if (_settingsManager != null)
        {
            var s = _settingsManager.Settings;
            if (s.SettingsWindowLeft.HasValue && s.SettingsWindowTop.HasValue)
            {
                // Validate position is on a visible screen
                var targetLeft = s.SettingsWindowLeft.Value;
                var targetTop = s.SettingsWindowTop.Value;
                var screen = WinForms.Screen.FromPoint(
                    new System.Drawing.Point((int)targetLeft, (int)targetTop));
                var workArea = screen.WorkingArea;

                if (targetLeft >= workArea.Left - 50 && targetLeft < workArea.Right &&
                    targetTop >= workArea.Top - 50 && targetTop < workArea.Bottom)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Left = targetLeft;
                    Top = targetTop;
                }
            }
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (IsPopupDisplayMode())
            return;

        // Save window position
        if (_settingsManager != null)
        {
            _settingsManager.Settings.SettingsWindowLeft = Left;
            _settingsManager.Settings.SettingsWindowTop = Top;
            _settingsManager.Save();
        }
    }

    private void OnKeyDown(object sender, WpfInput.KeyEventArgs e)
    {
        // Ctrl+T sends a test notification
        if (e.Key == WpfInput.Key.T && WpfInput.Keyboard.Modifiers == WpfInput.ModifierKeys.Control)
        {
            if (DataContext is SettingsViewModel vm)
                vm.PreviewNotificationCommand.Execute(null);
            e.Handled = true;
        }
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

    private void OnPickChromaPreset(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string propertyName, CommandParameter: string colorValue }) return;
        if (DataContext == null) return;

        var property = DataContext.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public);
        if (property == null || property.PropertyType != typeof(string)) return;

        property.SetValue(DataContext, colorValue);
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

    private void ApplyWindowedTitleBarTheme()
    {
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            var enabled = 1;
            var size = Marshal.SizeOf<int>();
            var result = DwmSetWindowAttribute(hwnd, DwmUseImmersiveDarkMode, ref enabled, size);
            if (result != 0)
            {
                // Fallback for older Windows builds.
                DwmSetWindowAttribute(hwnd, DwmUseImmersiveDarkModeLegacy, ref enabled, size);
            }
        }
        catch
        {
            // Non-critical visual enhancement.
        }
    }

    private bool IsPopupDisplayMode()
    {
        if (_settingsManager == null)
            return WindowStyle == WindowStyle.None;

        return string.Equals(_settingsManager.Settings.SettingsDisplayMode, "Popup", StringComparison.OrdinalIgnoreCase)
               || WindowStyle == WindowStyle.None;
    }
}
