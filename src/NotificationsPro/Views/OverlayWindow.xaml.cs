using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
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
    private readonly string _overlayLane;
    private HwndSource? _hwndSource;
    private bool _hookInstalled;
    private bool _isInternalMove;
    private bool _anchorToRightEdge;
    private bool _anchorToBottomEdge;
    private double _rightEdgeOffset = 16;
    private double _bottomEdgeOffset = 16;
    private bool _ncMouseDownTracked;
    private bool _dragOccurredSinceMouseDown;
    private bool _isHoveringOverlay;
    private DispatcherTimer? _hoverCheckTimer;

    // Win32 messages
    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCLBUTTONUP = 0x00A2;
    private const int WM_NCLBUTTONDBLCLK = 0x00A3;
    private const int WM_NCRBUTTONUP = 0x00A5;
    private const int WM_NCMOUSEMOVE = 0x00A0;
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

    public OverlayWindow(OverlayViewModel viewModel, SettingsManager settingsManager, string overlayLane = OverlayLaneHelper.Main)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsManager = settingsManager;
        _overlayLane = OverlayLaneHelper.Normalize(overlayLane);

        // SourceInitialized fires when the HWND is created — the EARLIEST point
        // we can install the Win32 hook. Loaded fires AFTER the window is shown.
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
        LocationChanged += OnLocationChanged;
        SizeChanged += OnSizeChanged;
        PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _settingsManager.SettingsChanged -= OnSettingsChanged;
        _hoverCheckTimer?.Stop();
        if (DataContext is OverlayViewModel vm)
            vm.Cleanup();
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

            case WM_NCLBUTTONDOWN:
                _ncMouseDownTracked = true;
                _dragOccurredSinceMouseDown = false;
                break;

            case WM_NCLBUTTONUP:
                if (_ncMouseDownTracked && !_dragOccurredSinceMouseDown)
                {
                    if (IsOverflowBadgeHit(lParam))
                    {
                        HandleOverflowBadgeClick();
                        handled = true;
                        _ncMouseDownTracked = false;
                        return IntPtr.Zero;
                    }

                    var clickItem = FindNotificationAtScreenPoint(lParam);
                    if (clickItem != null && DataContext is OverlayViewModel clickVm)
                    {
                        clickVm.Queue.DismissNotification(clickItem);
                        handled = true;
                        _ncMouseDownTracked = false;
                        return IntPtr.Zero;
                    }
                }
                _ncMouseDownTracked = false;
                break;

            case WM_NCRBUTTONUP:
                ShowCardContextMenu(lParam);
                handled = true;
                return IntPtr.Zero;

            case WM_NCMOUSEMOVE:
                if (!_isHoveringOverlay)
                {
                    _isHoveringOverlay = true;
                    if (DataContext is OverlayViewModel hoverVm)
                        hoverVm.Queue.PauseAllTimers();
                }
                ResetHoverCheckTimer();
                break;
        }

        return IntPtr.Zero;
    }

    private void HandleOverflowBadgeClick()
    {
        if (DataContext is not OverlayViewModel vm || !vm.Queue.HasOverflow)
            return;

        var currentLimit = Math.Max(1, _settingsManager.Settings.MaxVisibleNotifications);
        var suggestedLimit = Math.Min(40, currentLimit + vm.Queue.OverflowCount);

        if (suggestedLimit > currentLimit)
        {
            var result = System.Windows.MessageBox.Show(
                $"{vm.Queue.OverflowCount} notification(s) were not shown because the visible limit is currently {currentLimit}.\n\n" +
                "Overflow content is discarded immediately for privacy, so it cannot be expanded later.\n\n" +
                $"Increase the visible limit to {suggestedLimit} for future notifications?",
                "Overflow Summary",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
                return;

            var updated = _settingsManager.Settings.Clone();
            updated.MaxVisibleNotifications = suggestedLimit;
            _settingsManager.Apply(updated);
            return;
        }

        System.Windows.MessageBox.Show(
            $"{vm.Queue.OverflowCount} notification(s) were not shown while the overlay was already at its maximum visible limit of 40.\n\n" +
            "Overflow content is discarded immediately for privacy, so it cannot be expanded later.",
            "Overflow Summary",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Some systems do not yield a valid HwndSource during SourceInitialized.
        EnsureWindowHook();

        var settings = _settingsManager.Settings;

        var storedLeft = GetStoredLeft(settings);
        var storedTop = GetStoredTop(settings);
        if (storedLeft == null || storedTop == null)
        {
            if (IsSecondaryOverlay)
            {
                ApplySecondaryDefaultPlacement(settings);
            }
            else
            {
                var workArea = GetWorkAreaForMonitorIndex(settings.MonitorIndex);
                var pos = SnapHelper.GetDefaultPosition(Width, ActualHeight, workArea);
                Left = pos.X;
                Top = pos.Y;
            }
        }
        else
        {
            Left = storedLeft.Value;
            Top = storedTop.Value;
        }

        ApplyObsFixedWindowMode(settings);
        ApplyFullscreenOverlayMode(settings);

        if (!IsFullscreenOverlayActive(settings))
        {
            ApplyEffectiveMaxHeight(settings);
            TryApplySingleLineAutoFullWidth(settings);
            ClampToCurrentWorkArea();
            UpdateEdgeAnchors();
        }
        else
        {
            ApplyEffectiveMaxHeight(settings);
        }

        UpdateClickThrough(settings.ClickThrough);

        // Force a synchronous layout pass so fullscreen/height settings applied above
        // take effect immediately rather than waiting for the next render frame.
        InvalidateMeasure();
        InvalidateArrange();
        UpdateLayout();

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
            ApplyObsFixedWindowMode(settings);
            ApplyFullscreenOverlayMode(settings);

            if (!IsFullscreenOverlayActive(settings))
            {
                ApplyEffectiveMaxHeight(settings);
                TryApplySingleLineAutoFullWidth(settings);
                TryApplyStoredPosition(settings);
                ClampToCurrentWorkArea();
                UpdateEdgeAnchors();
            }
            else
            {
                ApplyEffectiveMaxHeight(settings);
            }

            InvalidateMeasure();
            InvalidateArrange();
            UpdateLayout();
        });
    }

    private void ApplyObsFixedWindowMode(AppSettings settings)
    {
        if (settings.ObsFixedWindowMode)
        {
            SizeToContent = SizeToContent.Manual;
            Width = Math.Clamp(settings.ObsFixedWidth, 200, 7680);
            Height = Math.Clamp(settings.ObsFixedHeight, 200, 4320);
        }
        else
        {
            SizeToContent = SizeToContent.Height;
            ClearValue(HeightProperty);
        }
    }

    private bool _wasFullscreen;

    private double _preFullscreenLeft;
    private double _preFullscreenTop;
    private double _preFullscreenWidth;

    private void ApplyFullscreenOverlayMode(AppSettings settings)
    {
        if (IsFullscreenOverlayActive(settings))
        {
            // Save previous position for restore if entering fullscreen for the first time
            if (!_wasFullscreen)
            {
                _preFullscreenLeft = Left;
                _preFullscreenTop = Top;
                _preFullscreenWidth = ActualWidth > 0 ? ActualWidth : settings.OverlayWidth;
            }

            // Use the selected monitor index, not the current window position
            var screens = WinForms.Screen.AllScreens;
            if (screens.Length == 0)
                return;
            var idx = Math.Clamp(settings.SelectedMonitorIndex, 0, screens.Length - 1);
            var workArea = screens[idx].Bounds;

            SizeToContent = SizeToContent.Manual;
            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            MaxHeight = workArea.Height; // Must be set before Height to remove any prior constraint
            Height = workArea.Height;
            _wasFullscreen = true;

            // Apply fullscreen background color with opacity
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.FullscreenOverlayColor);
                color.A = (byte)(settings.FullscreenOverlayOpacity * 255);
                Background = new SolidColorBrush(color);
            }
            catch
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                    (byte)(settings.FullscreenOverlayOpacity * 255), 0, 0, 0));
            }
        }
        else if (_wasFullscreen)
        {
            _wasFullscreen = false;
            Background = System.Windows.Media.Brushes.Transparent;
            // Restore previous size and position
            _isInternalMove = true;
            Width = _preFullscreenWidth;
            Left = _preFullscreenLeft;
            Top = _preFullscreenTop;
            _isInternalMove = false;
            if (!settings.ObsFixedWindowMode)
            {
                SizeToContent = SizeToContent.Height;
                ClearValue(HeightProperty);
            }
        }
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

        if (IsFullscreenOverlayActive(_settingsManager.Settings))
            return;

        if (_ncMouseDownTracked)
            _dragOccurredSinceMouseDown = true;

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
        SetStoredPosition(_settingsManager.Settings, Left, Top);
        var currentMonitor = GetCurrentMonitorIndex();
        SetConfiguredMonitorIndex(_settingsManager.Settings, currentMonitor);

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

        if (IsFullscreenOverlayActive(_settingsManager.Settings))
            return;

        if (ActualWidth > 0)
        {
            SetConfiguredWidth(_settingsManager.Settings, ActualWidth);
            if (!(_settingsManager.Settings.SingleLineMode && _settingsManager.Settings.SingleLineAutoFullWidth) && !IsSecondaryOverlay)
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

        SetStoredPosition(_settingsManager.Settings, Left, Top);
        SetConfiguredMonitorIndex(_settingsManager.Settings, GetCurrentMonitorIndex());
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

    private void OnCardLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Border card) return;
        var transform = EnsureMutableCardTransform(card);
        if (DataContext is not OverlayViewModel vm) return;
        if (!vm.AnimationsEnabled) return;

        var durationMs = vm.AnimationDurationMs;
        var fadeOnly = vm.FadeOnlyAnimation;
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        // Fade in
        var fadeDuration = new Duration(TimeSpan.FromMilliseconds(durationMs * 0.75));
        card.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, fadeDuration));

        if (fadeOnly) return;

        // Slide in from configured direction
        var motionDuration = new Duration(TimeSpan.FromMilliseconds(durationMs));
        var direction = vm.SlideInDirection;

        switch (direction)
        {
            case "Right":
                transform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(vm.OverlayWidth + 40, 0, motionDuration) { EasingFunction = easing });
                break;
            case "Top":
                transform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(-200, 0, motionDuration) { EasingFunction = easing });
                break;
            case "Bottom":
                transform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(200, 0, motionDuration) { EasingFunction = easing });
                break;
            default: // "Left"
                transform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(-(vm.OverlayWidth + 40), 0, motionDuration) { EasingFunction = easing });
                break;
        }
    }

    private static TranslateTransform EnsureMutableCardTransform(Border card)
    {
        if (card.RenderTransform is TranslateTransform existingTransform)
        {
            if (!existingTransform.IsFrozen)
                return existingTransform;

            var clone = existingTransform.CloneCurrentValue();
            card.RenderTransform = clone;
            return clone;
        }

        var newTransform = new TranslateTransform();
        card.RenderTransform = newTransform;
        return newTransform;
    }

    private NotificationItem? FindNotificationAtScreenPoint(IntPtr lParam)
    {
        var point = PointFromScreen(GetScreenPointFromLParam(lParam));

        var result = VisualTreeHelper.HitTest(this, point);
        if (result?.VisualHit == null) return null;

        DependencyObject? current = result.VisualHit;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is NotificationItem item)
                return item;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private bool IsOverflowBadgeHit(IntPtr lParam)
    {
        var point = PointFromScreen(GetScreenPointFromLParam(lParam));
        var result = VisualTreeHelper.HitTest(this, point);
        if (result?.VisualHit == null)
            return false;

        DependencyObject? current = result.VisualHit;
        while (current != null)
        {
            if (current is FrameworkElement fe && (ReferenceEquals(fe, OverflowBadge) || fe.Name == "OverflowBadge"))
                return true;

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void ShowCardContextMenu(IntPtr lParam)
    {
        var item = FindNotificationAtScreenPoint(lParam);
        var screenX = unchecked((short)(lParam.ToInt64() & 0xFFFF));
        var screenY = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));
        var point = PointFromScreen(new System.Windows.Point(screenX, screenY));

        var menu = new ContextMenu();

        if (DataContext is OverlayViewModel themeVm)
        {
            try
            {
                menu.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(themeVm.BackgroundColor));
                menu.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(themeVm.TextColor));
                menu.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(themeVm.BorderColor));
            }
            catch { /* fall back to default styling */ }
        }

        if (item != null)
        {
            var dismissMenuItem = new MenuItem { Header = "Dismiss" };
            dismissMenuItem.Click += (_, _) =>
            {
                if (DataContext is OverlayViewModel dismissVm)
                    dismissVm.Queue.DismissNotification(item);
            };
            menu.Items.Add(dismissMenuItem);

            var copyMenuItem = new MenuItem { Header = "Copy Text" };
            copyMenuItem.Click += (_, _) =>
            {
                var parts = new[] { item.AppName, item.Title, item.Body }
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var text = string.Join("\n", parts);
                if (!string.IsNullOrEmpty(text))
                    System.Windows.Clipboard.SetText(text);
            };
            menu.Items.Add(copyMenuItem);

            if (!string.IsNullOrWhiteSpace(item.AppName))
            {
                var isMuted = DataContext is OverlayViewModel muteCheckVm && muteCheckVm.Queue.IsAppMuted(item.AppName);
                var muteMenuItem = new MenuItem { Header = isMuted ? $"Unmute {item.AppName}" : $"Mute {item.AppName}" };
                muteMenuItem.Click += (_, _) =>
                {
                    if (DataContext is not OverlayViewModel muteVm) return;
                    if (isMuted)
                        muteVm.Queue.UnmuteApp(item.AppName);
                    else
                        muteVm.Queue.MuteApp(item.AppName);
                };
                menu.Items.Add(muteMenuItem);

                var settingsForAppMenuItem = new MenuItem { Header = $"Settings for {item.AppName}..." };
                settingsForAppMenuItem.Click += (_, _) =>
                {
                    if (System.Windows.Application.Current is App app)
                        app.ShowSettingsForApp(item.AppName);
                };
                menu.Items.Add(settingsForAppMenuItem);
            }

            menu.Items.Add(new Separator());
        }

        var copyAllMenuItem = new MenuItem { Header = "Copy All to Clipboard" };
        copyAllMenuItem.Click += (_, _) =>
        {
            if (DataContext is OverlayViewModel copyAllVm)
            {
                var notifications = copyAllVm.Queue.VisibleNotifications.ToList();
                if (notifications.Count == 0) return;
                var lines = notifications.Select(n =>
                {
                    var parts = new[] { n.AppName, n.Title, n.Body }
                        .Where(s => !string.IsNullOrWhiteSpace(s));
                    return string.Join(" — ", parts);
                });
                System.Windows.Clipboard.SetText(string.Join("\n", lines));
            }
        };
        menu.Items.Add(copyAllMenuItem);

        // Search toggle
        if (DataContext is OverlayViewModel searchVm)
        {
            var searchMenuItem = new MenuItem { Header = searchVm.IsSearchVisible ? "Hide Search" : "Search..." };
            searchMenuItem.Click += (_, _) =>
            {
                if (DataContext is OverlayViewModel svm)
                {
                    svm.IsSearchVisible = !svm.IsSearchVisible;
                    if (svm.IsSearchVisible)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            SearchBox?.Focus();
                        }, System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            };
            menu.Items.Add(searchMenuItem);
        }

        var clearMenuItem = new MenuItem { Header = "Clear All" };
        clearMenuItem.Click += (_, _) =>
        {
            if (DataContext is OverlayViewModel clearVm)
                clearVm.Queue.ClearAll();
        };
        menu.Items.Add(clearMenuItem);

        menu.PlacementTarget = this;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint;
        menu.HorizontalOffset = point.X;
        menu.VerticalOffset = point.Y;
        menu.IsOpen = true;
    }

    private void ResetHoverCheckTimer()
    {
        if (_hoverCheckTimer != null) return;
        _hoverCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _hoverCheckTimer.Tick += OnHoverCheckTick;
        _hoverCheckTimer.Start();
    }

    private void OnHoverCheckTick(object? sender, EventArgs e)
    {
        var cursorPos = WinForms.Cursor.Position;
        var windowRect = new Rect(Left, Top, ActualWidth, ActualHeight);
        if (!windowRect.Contains(new System.Windows.Point(cursorPos.X, cursorPos.Y)))
        {
            _hoverCheckTimer?.Stop();
            _hoverCheckTimer = null;
            _isHoveringOverlay = false;
            if (DataContext is OverlayViewModel vm)
                vm.Queue.ResumeAllTimers();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _hoverCheckTimer?.Stop();
        _hoverCheckTimer = null;

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
        if (IsFullscreenOverlayActive(_settingsManager.Settings))
            return GetMonitorBoundsForIndex(_settingsManager.Settings.SelectedMonitorIndex);

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

    private static Rect GetMonitorBoundsForIndex(int index)
    {
        var screens = WinForms.Screen.AllScreens;
        if (screens.Length == 0)
            return SystemParameters.WorkArea;

        if (index < 0 || index >= screens.Length)
            return ToRect(WinForms.Screen.PrimaryScreen?.Bounds ?? screens[0].Bounds);

        return ToRect(screens[index].Bounds);
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
        if (IsFullscreenOverlayActive(_settingsManager.Settings))
            return;

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
        var maxLeft = workArea.Right - ActualWidth + 8;
        var minLeft = workArea.Left - 8;
        if (maxLeft < minLeft)
            maxLeft = minLeft;
        return Math.Max(minLeft, Math.Min(left, maxLeft));
    }

    private double ClampToWorkAreaY(double top, Rect workArea)
    {
        var maxTop = workArea.Bottom - ActualHeight + 24;
        var minTop = workArea.Top - 8;
        if (maxTop < minTop)
            maxTop = minTop;
        return Math.Max(minTop, Math.Min(top, maxTop));
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

    private static System.Windows.Point GetScreenPointFromLParam(IntPtr lParam)
    {
        var screenX = unchecked((short)(lParam.ToInt64() & 0xFFFF));
        var screenY = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));
        return new System.Windows.Point(screenX, screenY);
    }

    private void ApplyEffectiveMaxHeight(AppSettings settings)
    {
        if (IsFullscreenOverlayActive(settings))
        {
            var monitorBounds = GetMonitorBoundsForIndex(settings.SelectedMonitorIndex);
            var maxHeight = Math.Max(120, monitorBounds.Height);
            if (Math.Abs(MaxHeight - maxHeight) > 0.5)
                MaxHeight = maxHeight;

            if (Math.Abs(NotificationScrollViewer.MaxHeight - maxHeight) > 0.5)
                NotificationScrollViewer.MaxHeight = maxHeight;
            return;
        }

        var workArea = GetCurrentWorkArea();
        var userMax = Math.Max(120, GetConfiguredMaxHeight(settings));
        var effectiveMax = Math.Min(userMax, Math.Max(120, workArea.Height - (OverlayMargin * 2)));
        var scrollViewerMax = Math.Max(120, effectiveMax - OuterContentMargin);

        if (Math.Abs(MaxHeight - effectiveMax) > 0.5)
            MaxHeight = effectiveMax;

        if (Math.Abs(NotificationScrollViewer.MaxHeight - scrollViewerMax) > 0.5)
            NotificationScrollViewer.MaxHeight = scrollViewerMax;
    }

    private void TryApplyStoredPosition(AppSettings settings)
    {
        var storedLeft = GetStoredLeft(settings);
        var storedTop = GetStoredTop(settings);
        if (storedLeft is not double targetLeft || storedTop is not double targetTop)
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

        if (!IsSecondaryOverlay && settings.LastManualOverlayWidth <= 0)
            settings.LastManualOverlayWidth = Math.Max(MinWidth, settings.OverlayWidth);

        var workArea = GetCurrentWorkArea();
        var targetWidth = Math.Max(MinWidth, workArea.Width + 16);
        var targetLeft = workArea.Left - 8;

        var widthChanged = Math.Abs(targetWidth - Width) > 0.5;
        var leftChanged = Math.Abs(targetLeft - Left) > 0.5;
        if (!widthChanged && !leftChanged)
            return;

        _isInternalMove = true;
        Width = targetWidth;
        Left = targetLeft;
        _isInternalMove = false;

        SetConfiguredWidth(settings, targetWidth);
        SetStoredPosition(settings, targetLeft, Top);
    }

    private bool IsSecondaryOverlay =>
        string.Equals(_overlayLane, OverlayLaneHelper.Secondary, StringComparison.OrdinalIgnoreCase);

    private bool IsFullscreenOverlayActive(AppSettings settings) =>
        settings.FullscreenOverlayMode && !IsSecondaryOverlay;

    private double? GetStoredLeft(AppSettings settings) =>
        IsSecondaryOverlay ? settings.SecondaryOverlayLeft : settings.OverlayLeft;

    private double? GetStoredTop(AppSettings settings) =>
        IsSecondaryOverlay ? settings.SecondaryOverlayTop : settings.OverlayTop;

    private void SetStoredPosition(AppSettings settings, double left, double top)
    {
        if (IsSecondaryOverlay)
        {
            settings.SecondaryOverlayLeft = left;
            settings.SecondaryOverlayTop = top;
            return;
        }

        settings.OverlayLeft = left;
        settings.OverlayTop = top;
    }

    private int GetConfiguredMonitorIndex(AppSettings settings) =>
        IsSecondaryOverlay ? settings.SecondaryOverlayMonitorIndex : settings.MonitorIndex;

    private void SetConfiguredMonitorIndex(AppSettings settings, int index)
    {
        if (IsSecondaryOverlay)
        {
            settings.SecondaryOverlayMonitorIndex = index;
            return;
        }

        settings.MonitorIndex = index;
    }

    private double GetConfiguredMaxHeight(AppSettings settings) =>
        IsSecondaryOverlay ? settings.SecondaryOverlayMaxHeight : settings.OverlayMaxHeight;

    private void SetConfiguredWidth(AppSettings settings, double width)
    {
        if (IsSecondaryOverlay)
        {
            settings.SecondaryOverlayWidth = width;
            return;
        }

        settings.OverlayWidth = width;
    }

    private void ApplySecondaryDefaultPlacement(AppSettings settings)
    {
        var workArea = GetWorkAreaForMonitorIndex(GetConfiguredMonitorIndex(settings));
        var width = ActualWidth > 0 ? ActualWidth : Math.Max(MinWidth, settings.SecondaryOverlayWidth);
        var height = ActualHeight > 0 ? ActualHeight : Math.Min(360, workArea.Height - 16);
        var (left, top) = GetPresetPosition(
            settings.SecondaryOverlayPositionPreset,
            workArea,
            width,
            height);

        Left = left;
        Top = top;
        SetStoredPosition(settings, left, top);
    }

    private static (double Left, double Top) GetPresetPosition(string? preset, Rect workArea, double width, double height)
    {
        var targetLeft = workArea.Left - 8;
        var targetTop = workArea.Top - 8;

        switch (SecondaryOverlayPositionHelper.Normalize(preset))
        {
            case SecondaryOverlayPositionHelper.TopCenter:
                targetLeft = workArea.Left + ((workArea.Width - width) / 2);
                break;
            case SecondaryOverlayPositionHelper.TopRight:
                targetLeft = workArea.Right - width + 8;
                break;
            case SecondaryOverlayPositionHelper.MiddleLeft:
                targetTop = workArea.Top + ((workArea.Height - height) / 2);
                break;
            case SecondaryOverlayPositionHelper.MiddleCenter:
                targetLeft = workArea.Left + ((workArea.Width - width) / 2);
                targetTop = workArea.Top + ((workArea.Height - height) / 2);
                break;
            case SecondaryOverlayPositionHelper.MiddleRight:
                targetLeft = workArea.Right - width + 8;
                targetTop = workArea.Top + ((workArea.Height - height) / 2);
                break;
            case SecondaryOverlayPositionHelper.BottomLeft:
                targetTop = workArea.Bottom - height + 24;
                break;
            case SecondaryOverlayPositionHelper.BottomCenter:
                targetLeft = workArea.Left + ((workArea.Width - width) / 2);
                targetTop = workArea.Bottom - height + 24;
                break;
            case SecondaryOverlayPositionHelper.BottomRight:
                targetLeft = workArea.Right - width + 8;
                targetTop = workArea.Bottom - height + 24;
                break;
            default:
                break;
        }

        var minLeft = workArea.Left - 8;
        var maxLeft = Math.Max(minLeft, workArea.Right - width + 8);
        var minTop = workArea.Top - 8;
        var maxTop = Math.Max(minTop, workArea.Bottom - height + 24);

        return (
            Math.Max(minLeft, Math.Min(targetLeft, maxLeft)),
            Math.Max(minTop, Math.Min(targetTop, maxTop)));
    }
}
