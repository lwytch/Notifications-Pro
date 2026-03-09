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
    private const int DwmWindowCornerPreference = 33;
    private const int DWMWCP_DONOTROUND = 1;
    private const int DWMWCP_ROUND = 2;
    private const int DWMWCP_ROUNDSMALL = 3;

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
        // Ctrl+Z = Undo
        else if (e.Key == WpfInput.Key.Z && WpfInput.Keyboard.Modifiers == WpfInput.ModifierKeys.Control)
        {
            if (DataContext is SettingsViewModel vm && vm.UndoCommand.CanExecute(null))
                vm.UndoCommand.Execute(null);
            e.Handled = true;
        }
        // Ctrl+Y = Redo
        else if (e.Key == WpfInput.Key.Y && WpfInput.Keyboard.Modifiers == WpfInput.ModifierKeys.Control)
        {
            if (DataContext is SettingsViewModel vm && vm.RedoCommand.CanExecute(null))
                vm.RedoCommand.Execute(null);
            e.Handled = true;
        }
        // Escape = Close settings window
        else if (e.Key == WpfInput.Key.Escape)
        {
            Close();
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

        // Use a Win32 owner wrapper to prevent crashes when AllowsTransparency=true (popup mode)
        var owner = new WinForms.NativeWindow();
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
            owner.AssignHandle(hwnd);

        if (dialog.ShowDialog(owner) != WinForms.DialogResult.OK)
            return;

        var selected = dialog.Color;
        var hex = $"#{selected.R:X2}{selected.G:X2}{selected.B:X2}";
        property.SetValue(DataContext, hex);
    }

    private void OnPickKeywordColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { DataContext: ViewModels.KeywordHighlightEntry entry }) return;
        if (DataContext is not ViewModels.SettingsViewModel vm) return;

        var start = ParseHex(entry.Color);
        using var dialog = new WinForms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
            Color = System.Drawing.Color.FromArgb(start.A, start.R, start.G, start.B)
        };

        var owner2 = new WinForms.NativeWindow();
        var hwnd2 = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd2 != IntPtr.Zero)
            owner2.AssignHandle(hwnd2);

        if (dialog.ShowDialog(owner2) != WinForms.DialogResult.OK) return;

        var selected = dialog.Color;
        entry.Color = $"#{selected.R:X2}{selected.G:X2}{selected.B:X2}";
        vm.NotifyKeywordColorChanged();
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

            // Apply DWM corner preference based on settings radius
            var radius = _settingsManager?.Settings.SettingsWindowCornerRadius ?? 12;
            var cornerPref = radius <= 0 ? DWMWCP_DONOTROUND
                           : radius <= 6 ? DWMWCP_ROUNDSMALL
                           : DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DwmWindowCornerPreference, ref cornerPref, size);
        }
        catch
        {
            // Non-critical visual enhancement.
        }
    }

    public void NavigateToTab(string tabHeader)
    {
        for (var i = 0; i < MainTabControl.Items.Count; i++)
        {
            if (MainTabControl.Items[i] is System.Windows.Controls.TabItem tab
                && string.Equals(tab.Header?.ToString(), tabHeader, StringComparison.OrdinalIgnoreCase))
            {
                MainTabControl.SelectedIndex = i;
                return;
            }
        }
    }

    private bool IsPopupDisplayMode()
    {
        if (_settingsManager == null)
            return WindowStyle == WindowStyle.None;

        return string.Equals(_settingsManager.Settings.SettingsDisplayMode, "Popup", StringComparison.OrdinalIgnoreCase)
               || WindowStyle == WindowStyle.None;
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
