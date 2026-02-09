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
    private HwndSource? _hwndSource;
    private bool _hookInstalled;
    private bool _isInternalMove;
    private bool _anchorToRightEdge;
    private bool _anchorToBottomEdge;
    private double _rightEdgeOffset = 16;
    private double _bottomEdgeOffset = 16;

    // Win32 messages
    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCLBUTTONDBLCLK = 0x00A3;
    private const int HTCAPTION = 2;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const double ResizeBorderThickness = 8;

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
        SizeChanged += OnSizeChanged;
        PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        EnsureWindowHook();
    }

    private void EnsureWindowHook()
    {
        if (_hookInstalled) return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        _hwndSource = HwndSource.FromHwnd(hwnd);
        if (_hwndSource == null) return;

        _hwndSource.AddHook(WndProc);
        _hookInstalled = true;
    }

    /// <summary>
    /// Win32 message hook. Returns resize hit targets near left/right edges
    /// (when enabled), otherwise HTCAPTION so the whole overlay remains draggable.
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_NCHITTEST:
                if (_settingsManager.Settings.AllowManualResize
                    && !_settingsManager.Settings.ClickThrough
                    && TryGetResizeHitTest(lParam, out var resizeHit))
                {
                    handled = true;
                    return (IntPtr)resizeHit;
                }

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
        // Some systems do not yield a valid HwndSource during SourceInitialized.
        EnsureWindowHook();

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

        UpdateEdgeAnchors();
        UpdateClickThrough(settings.ClickThrough);
        _settingsManager.SettingsChanged += OnSettingsChanged;
    }

    private void OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Fallback if Win32 hook was not attached for any reason.
        if (_hookInstalled || _settingsManager.Settings.ClickThrough) return;
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;

        try
        {
            DragMove();
            e.Handled = true;
        }
        catch
        {
            // DragMove can throw when mouse state changes mid-drag.
        }
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
        if (_isInternalMove)
            return;

        if (_settingsManager.Settings.SnapToEdges)
        {
            var workArea = SystemParameters.WorkArea;
            var snapped = SnapHelper.SnapToEdges(
                Left, Top, ActualWidth, ActualHeight,
                workArea, _settingsManager.Settings.SnapDistance);

            if (Math.Abs(snapped.X - Left) > 0.5 || Math.Abs(snapped.Y - Top) > 0.5)
            {
                _isInternalMove = true;
                LocationChanged -= OnLocationChanged;
                Left = snapped.X;
                Top = snapped.Y;
                LocationChanged += OnLocationChanged;
                _isInternalMove = false;
            }
        }

        UpdateEdgeAnchors();
        _settingsManager.Settings.OverlayLeft = Left;
        _settingsManager.Settings.OverlayTop = Top;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isInternalMove)
            return;

        if (ActualWidth > 0)
            _settingsManager.Settings.OverlayWidth = ActualWidth;

        var workArea = SystemParameters.WorkArea;
        var targetLeft = Left;
        var targetTop = Top;

        if (_anchorToRightEdge)
            targetLeft = workArea.Right - _rightEdgeOffset - ActualWidth;

        if (_anchorToBottomEdge)
            targetTop = workArea.Bottom - _bottomEdgeOffset - ActualHeight;

        targetLeft = Math.Max(workArea.Left, Math.Min(targetLeft, workArea.Right - ActualWidth));
        targetTop = Math.Max(workArea.Top, Math.Min(targetTop, workArea.Bottom - ActualHeight));

        if (Math.Abs(targetLeft - Left) > 0.5 || Math.Abs(targetTop - Top) > 0.5)
        {
            _isInternalMove = true;
            Left = targetLeft;
            Top = targetTop;
            _isInternalMove = false;
        }

        _settingsManager.Settings.OverlayLeft = Left;
        _settingsManager.Settings.OverlayTop = Top;
    }

    private void UpdateEdgeAnchors()
    {
        var workArea = SystemParameters.WorkArea;
        var threshold = Math.Max(8, _settingsManager.Settings.SnapDistance + 4);

        var rightGap = workArea.Right - (Left + ActualWidth);
        var bottomGap = workArea.Bottom - (Top + ActualHeight);

        _anchorToRightEdge = rightGap <= threshold;
        _anchorToBottomEdge = bottomGap <= threshold;

        if (_anchorToRightEdge)
            _rightEdgeOffset = Math.Max(0, rightGap);

        if (_anchorToBottomEdge)
            _bottomEdgeOffset = Math.Max(0, bottomGap);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_hookInstalled && _hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hookInstalled = false;
        }

        _settingsManager.SettingsChanged -= OnSettingsChanged;
        _settingsManager.Save();
        base.OnClosed(e);
    }

    private bool TryGetResizeHitTest(IntPtr lParam, out int hitTest)
    {
        hitTest = HTCAPTION;

        if (ActualWidth <= 0 || ActualHeight <= 0)
            return false;

        var screenPoint = new System.Windows.Point(
            unchecked((short)(lParam.ToInt64() & 0xFFFF)),
            unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF)));
        var point = PointFromScreen(screenPoint);

        var onLeft = point.X >= 0 && point.X <= ResizeBorderThickness;
        var onRight = point.X <= ActualWidth && point.X >= ActualWidth - ResizeBorderThickness;

        if (onLeft)
        {
            hitTest = HTLEFT;
            return true;
        }

        if (onRight)
        {
            hitTest = HTRIGHT;
            return true;
        }

        return false;
    }
}
