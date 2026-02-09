using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using NotificationsPro.Helpers;
using NotificationsPro.Services;
using NotificationsPro.ViewModels;

namespace NotificationsPro.Views;

public partial class OverlayWindow : Window
{
    private readonly SettingsManager _settingsManager;

    // Win32 constants for click-through
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

    public OverlayWindow(OverlayViewModel viewModel, SettingsManager settingsManager)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsManager = settingsManager;

        Loaded += OnLoaded;
        LocationChanged += OnLocationChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = _settingsManager.Settings;

        // Position the window
        if (double.IsNaN(settings.OverlayLeft) || double.IsNaN(settings.OverlayTop))
        {
            var workArea = SystemParameters.WorkArea;
            var pos = SnapHelper.GetDefaultPosition(Width, ActualHeight, workArea);
            Left = pos.X;
            Top = pos.Y;
        }
        else
        {
            Left = settings.OverlayLeft;
            Top = settings.OverlayTop;
        }

        UpdateClickThrough(settings.ClickThrough);
        _settingsManager.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateClickThrough(_settingsManager.Settings.ClickThrough);
        });
    }

    public void UpdateClickThrough(bool enabled)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (enabled)
            SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hwnd, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        DragMove();
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (_settingsManager.Settings.SnapToEdges)
        {
            var workArea = SystemParameters.WorkArea;
            var snapped = SnapHelper.SnapToEdges(
                Left, Top, ActualWidth, ActualHeight,
                workArea, _settingsManager.Settings.SnapDistance);

            if (Math.Abs(snapped.X - Left) > 0.5 || Math.Abs(snapped.Y - Top) > 0.5)
            {
                LocationChanged -= OnLocationChanged;
                Left = snapped.X;
                Top = snapped.Y;
                LocationChanged += OnLocationChanged;
            }
        }

        // Save position
        _settingsManager.Settings.OverlayLeft = Left;
        _settingsManager.Settings.OverlayTop = Top;
    }

    protected override void OnClosed(EventArgs e)
    {
        _settingsManager.SettingsChanged -= OnSettingsChanged;
        _settingsManager.Save();
        base.OnClosed(e);
    }
}
