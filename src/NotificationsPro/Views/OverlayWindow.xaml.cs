using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;
using NotificationsPro.ViewModels;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

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
    private const int HTTRANSPARENT = -1;
    private const int HTCAPTION = 2;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const double ResizeBorderThickness = 8;
    private const double OverlayMargin = 16;
    private const double OuterContentMargin = 8;

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
                if (_settingsManager.Settings.ClickThrough)
                {
                    handled = true;
                    return (IntPtr)HTTRANSPARENT;
                }

                if (_settingsManager.Settings.AllowManualResize
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
            var workArea = GetWorkAreaForMonitorIndex(settings.MonitorIndex);
            var pos = SnapHelper.GetDefaultPosition(Width, ActualHeight, workArea);
            Left = pos.X;
            Top = pos.Y;
        }
        else
        {
            Left = settings.OverlayLeft.Value;
            Top = settings.OverlayTop.Value;
        }

        ApplyEffectiveMaxHeight(settings);
        TryApplySingleLineAutoFullWidth(settings);
        ClampToCurrentWorkArea();
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
            var settings = _settingsManager.Settings;
            UpdateClickThrough(settings.ClickThrough);
            ApplyEffectiveMaxHeight(settings);
            TryApplySingleLineAutoFullWidth(settings);
            TryApplyStoredPosition(settings);
            ClampToCurrentWorkArea();
            UpdateEdgeAnchors();
            InvalidateMeasure();
            InvalidateArrange();
            UpdateLayout();
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

        var previousMonitor = _settingsManager.Settings.MonitorIndex;

        if (_settingsManager.Settings.SnapToEdges)
        {
            var workArea = GetCurrentWorkArea();
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
        var currentMonitor = GetCurrentMonitorIndex();
        _settingsManager.Settings.MonitorIndex = currentMonitor;

        if (currentMonitor != previousMonitor)
        {
            ApplyEffectiveMaxHeight(_settingsManager.Settings);
            TryApplySingleLineAutoFullWidth(_settingsManager.Settings);
            ClampToCurrentWorkArea();
            UpdateEdgeAnchors();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isInternalMove)
            return;

        if (ActualWidth > 0)
        {
            _settingsManager.Settings.OverlayWidth = ActualWidth;
            if (!(_settingsManager.Settings.SingleLineMode && _settingsManager.Settings.SingleLineAutoFullWidth))
                _settingsManager.Settings.LastManualOverlayWidth = ActualWidth;
        }

        var workArea = GetCurrentWorkArea();
        var targetLeft = Left;
        var targetTop = Top;
        var threshold = Math.Max(8, _settingsManager.Settings.SnapDistance + 4);
        var previousWidth = ResolveDimension(e.PreviousSize.Width, ActualWidth, MinWidth, 380);
        var previousHeight = ResolveDimension(e.PreviousSize.Height, ActualHeight, MinHeight, 120);
        var rightGapBefore = workArea.Right - (Left + previousWidth);
        var bottomGapBefore = workArea.Bottom - (Top + previousHeight);
        var shouldAnchorRight = _anchorToRightEdge || Math.Abs(rightGapBefore) <= threshold;
        var shouldAnchorBottom = _anchorToBottomEdge || Math.Abs(bottomGapBefore) <= threshold;

        if (shouldAnchorRight)
        {
            var rightOffset = Math.Max(0, rightGapBefore);
            targetLeft = workArea.Right - rightOffset - ActualWidth;
        }

        if (shouldAnchorBottom)
        {
            var bottomOffset = Math.Max(0, bottomGapBefore);
            targetTop = workArea.Bottom - bottomOffset - ActualHeight;
        }

        targetLeft = ClampToWorkAreaX(targetLeft, workArea);
        targetTop = ClampToWorkAreaY(targetTop, workArea);

        if (_settingsManager.Settings.SnapToEdges)
        {
            var snapped = SnapHelper.SnapToEdges(
                targetLeft, targetTop, ActualWidth, ActualHeight,
                workArea, _settingsManager.Settings.SnapDistance);
            targetLeft = snapped.X;
            targetTop = snapped.Y;
        }

        if (Math.Abs(targetLeft - Left) > 0.5 || Math.Abs(targetTop - Top) > 0.5)
        {
            _isInternalMove = true;
            Left = targetLeft;
            Top = targetTop;
            _isInternalMove = false;
        }

        _settingsManager.Settings.OverlayLeft = Left;
        _settingsManager.Settings.OverlayTop = Top;
        _settingsManager.Settings.MonitorIndex = GetCurrentMonitorIndex();
        UpdateEdgeAnchors();
    }

    private void UpdateEdgeAnchors()
    {
        var workArea = GetCurrentWorkArea();
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

    private Rect GetCurrentWorkArea()
    {
        var width = ResolveDimension(ActualWidth, Width, MinWidth, 380);
        var height = ResolveDimension(ActualHeight, Height, MinHeight, 120);
        return GetWorkAreaForBounds(Left, Top, width, height);
    }

    private static Rect GetWorkAreaForMonitorIndex(int index)
    {
        var screens = WinForms.Screen.AllScreens;
        if (screens.Length == 0)
            return SystemParameters.WorkArea;

        if (index < 0 || index >= screens.Length)
            return ToRect(WinForms.Screen.PrimaryScreen?.WorkingArea ?? screens[0].WorkingArea);

        return ToRect(screens[index].WorkingArea);
    }

    private static Rect GetWorkAreaForBounds(double left, double top, double width, double height)
    {
        var safeWidth = Math.Max(1, (int)Math.Round(width));
        var safeHeight = Math.Max(1, (int)Math.Round(height));
        var rect = new Drawing.Rectangle(
            SafeRoundToInt(left),
            SafeRoundToInt(top),
            safeWidth,
            safeHeight);
        var screen = WinForms.Screen.FromRectangle(rect);
        return ToRect(screen.WorkingArea);
    }

    private int GetCurrentMonitorIndex()
    {
        var width = ResolveDimension(ActualWidth, Width, MinWidth, 380);
        var height = ResolveDimension(ActualHeight, Height, MinHeight, 120);
        var safeWidth = Math.Max(1, (int)Math.Round(width));
        var safeHeight = Math.Max(1, (int)Math.Round(height));
        var rect = new Drawing.Rectangle(
            SafeRoundToInt(Left),
            SafeRoundToInt(Top),
            safeWidth,
            safeHeight);

        var current = WinForms.Screen.FromRectangle(rect);
        var screens = WinForms.Screen.AllScreens;
        for (var i = 0; i < screens.Length; i++)
        {
            if (screens[i].DeviceName == current.DeviceName)
                return i;
        }

        return 0;
    }

    private void ClampToCurrentWorkArea()
    {
        var workArea = GetCurrentWorkArea();
        var targetLeft = ClampToWorkAreaX(Left, workArea);
        var targetTop = ClampToWorkAreaY(Top, workArea);

        if (Math.Abs(targetLeft - Left) <= 0.5 && Math.Abs(targetTop - Top) <= 0.5)
            return;

        _isInternalMove = true;
        Left = targetLeft;
        Top = targetTop;
        _isInternalMove = false;
    }

    private double ClampToWorkAreaX(double left, Rect workArea)
    {
        var maxLeft = workArea.Right - ActualWidth;
        if (maxLeft < workArea.Left)
            maxLeft = workArea.Left;
        return Math.Max(workArea.Left, Math.Min(left, maxLeft));
    }

    private double ClampToWorkAreaY(double top, Rect workArea)
    {
        var maxTop = workArea.Bottom - ActualHeight;
        if (maxTop < workArea.Top)
            maxTop = workArea.Top;
        return Math.Max(workArea.Top, Math.Min(top, maxTop));
    }

    private static Rect ToRect(Drawing.Rectangle rect)
    {
        return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    private static double ResolveDimension(double actual, double configured, double minimum, double fallback)
    {
        if (actual > 0 && !double.IsNaN(actual))
            return actual;

        if (!double.IsNaN(configured) && configured > 0)
            return Math.Max(configured, minimum > 0 ? minimum : 1);

        if (!double.IsNaN(minimum) && minimum > 0)
            return minimum;

        return fallback;
    }

    private static int SafeRoundToInt(double value, int fallback = 0)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return fallback;

        return (int)Math.Round(value);
    }

    private void ApplyEffectiveMaxHeight(AppSettings settings)
    {
        var workArea = GetCurrentWorkArea();
        var userMax = Math.Max(120, settings.OverlayMaxHeight);
        var effectiveMax = Math.Min(userMax, Math.Max(120, workArea.Height - (OverlayMargin * 2)));
        var scrollViewerMax = Math.Max(120, effectiveMax - OuterContentMargin);

        if (Math.Abs(MaxHeight - effectiveMax) > 0.5)
            MaxHeight = effectiveMax;

        if (Math.Abs(NotificationScrollViewer.MaxHeight - scrollViewerMax) > 0.5)
            NotificationScrollViewer.MaxHeight = scrollViewerMax;
    }

    private void TryApplyStoredPosition(AppSettings settings)
    {
        if (settings.OverlayLeft is not double targetLeft || settings.OverlayTop is not double targetTop)
            return;

        if (Math.Abs(targetLeft - Left) <= 0.5 && Math.Abs(targetTop - Top) <= 0.5)
            return;

        _isInternalMove = true;
        Left = targetLeft;
        Top = targetTop;
        _isInternalMove = false;
    }

    private void TryApplySingleLineAutoFullWidth(AppSettings settings)
    {
        if (!settings.SingleLineMode || !settings.SingleLineAutoFullWidth)
            return;

        if (settings.LastManualOverlayWidth <= 0)
            settings.LastManualOverlayWidth = Math.Max(MinWidth, settings.OverlayWidth);

        var workArea = GetCurrentWorkArea();
        var targetWidth = Math.Max(MinWidth, workArea.Width - (OverlayMargin * 2));
        var targetLeft = workArea.Left + OverlayMargin;

        var widthChanged = Math.Abs(targetWidth - Width) > 0.5;
        var leftChanged = Math.Abs(targetLeft - Left) > 0.5;
        if (!widthChanged && !leftChanged)
            return;

        _isInternalMove = true;
        Width = targetWidth;
        Left = targetLeft;
        _isInternalMove = false;

        settings.OverlayWidth = targetWidth;
        settings.OverlayLeft = targetLeft;
    }
}
