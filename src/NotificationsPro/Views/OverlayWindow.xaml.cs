using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using NotificationsPro.Helpers;
using NotificationsPro.Services;
using NotificationsPro.ViewModels;

namespace NotificationsPro.Views;

public partial class OverlayWindow : Window
{
    private readonly SettingsManager _settingsManager;

    // Win32 messages
    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCLBUTTONDBLCLK = 0x00A3;
    private const int HTCAPTION = 2;
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

        // SourceInitialized fires when the HWND is created — the EARLIEST point
        // we can install the Win32 hook. Loaded fires AFTER the window is shown.
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        LocationChanged += OnLocationChanged;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Install WndProc hook at the earliest possible moment
        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        hwndSource?.AddHook(WndProc);
    }

    /// <summary>
    /// Win32 message hook. Returns HTCAPTION for all hit tests, which tells
    /// Windows the entire window is a title bar — Windows handles drag natively.
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_NCHITTEST:
                handled = true;
                return (IntPtr)HTCAPTION;

            case WM_NCLBUTTONDBLCLK:
                // Prevent maximize on double-click
                handled = true;
                return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = _settingsManager.Settings;

        if (settings.OverlayLeft == null || settings.OverlayTop == null)
        {
            var workArea = SystemParameters.WorkArea;
            var pos = SnapHelper.GetDefaultPosition(Width, ActualHeight, workArea);
            Left = pos.X;
            Top = pos.Y;
        }
        else
        {
            Left = settings.OverlayLeft.Value;
            Top = settings.OverlayTop.Value;
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
