using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;
using NotificationsPro.ViewModels;
using NotificationsPro.Views;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace NotificationsPro;

public partial class App : Application
{
    private WinForms.NotifyIcon? _trayIcon;
    private OverlayWindow? _overlayWindow;
    private SettingsWindow? _settingsWindow;
    private QueueManager? _queueManager;
    private SettingsManager? _settingsManager;
    private OverlayViewModel? _overlayViewModel;
    private SettingsViewModel? _settingsViewModel;

    private NotificationListener? _notificationListener;
    private HotkeyManager? _hotkeyManager;

    private WinForms.ToolStripMenuItem? _showHideItem;
    private WinForms.ToolStripMenuItem? _pauseResumeItem;
    private WinForms.ToolStripMenuItem? _clickThroughItem;
    private WinForms.ToolStripMenuItem? _alwaysOnTopItem;
    private WinForms.ToolStripMenuItem? _statusItem;
    private WinForms.ToolStripMenuItem? _grantAccessItem;
    private WinForms.ToolStripMenuItem? _focusModeItem;
    private WinForms.ToolStripMenuItem? _quickMuteItem;
    private WinForms.ToolStripMenuItem? _themeSwitchItem;
    private DispatcherTimer? _focusTimer;
    private DateTime _focusEndTime;
    private ThemeManager? _themeManager;
    private DispatcherTimer? _presentationTimer;
    private bool _presentationDndActive;
    private System.ComponentModel.PropertyChangedEventHandler? _highContrastHandler;

    // Unpackaged desktop apps need an explicit AppUserModelID so the OS can
    // identify them in Privacy > Notifications and grant listener access.
    [DllImport("shell32.dll", SetLastError = true)]
    private static extern void SetCurrentProcessExplicitAppUserModelID(
        [MarshalAs(UnmanagedType.LPWStr)] string appId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private enum TaskbarEdge
    {
        Bottom,
        Top,
        Left,
        Right
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set AUMID before any notification API calls — this makes the app
        // appear in Windows Settings > Privacy > Notifications
        SetCurrentProcessExplicitAppUserModelID("NotificationsPro.App");

        _settingsManager = new SettingsManager();
        _settingsManager.Load();
        _themeManager = new ThemeManager();

        // Sync startup registry with saved setting
        SyncStartupRegistryState();

        _queueManager = new QueueManager(_settingsManager);

        _overlayViewModel = new OverlayViewModel(_queueManager, _settingsManager);
        _settingsViewModel = new SettingsViewModel(_settingsManager, _queueManager);

        _notificationListener = new NotificationListener(_queueManager, Dispatcher, _settingsManager);
        _notificationListener.StatusChanged += UpdateStatusItem;

        // Play notification sounds
        _queueManager.NotificationAdded += appName =>
            Services.SoundService.PlaySound(appName, _settingsManager.Settings);

        // Update tray icon badge when notification count changes
        ((System.Collections.Specialized.INotifyCollectionChanged)_queueManager.VisibleNotifications)
            .CollectionChanged += (_, _) => Dispatcher.InvokeAsync(UpdateTrayIcon);

        SetupTrayIcon();

        // Apply settings window theme preset/custom palette.
        Services.SettingsThemeService.ApplySettingsTheme(_settingsManager.Settings);
        _settingsManager.SettingsChanged += () =>
            Services.SettingsThemeService.ApplySettingsTheme(_settingsManager.Settings);

        // Apply High Contrast theme if active and respected
        ApplyHighContrastIfNeeded();
        _highContrastHandler = (_, e) =>
        {
            if (e.PropertyName == nameof(SystemParameters.HighContrast))
                Dispatcher.Invoke(ApplyHighContrastIfNeeded);
        };
        SystemParameters.StaticPropertyChanged += _highContrastHandler;

        if (_settingsManager.Settings.OverlayVisible)
            ShowOverlay();

        // Register global hotkeys when overlay window has an HWND
        _settingsManager.SettingsChanged += RefreshHotkeys;
        RefreshHotkeys();

        // Presentation mode: poll for fullscreen apps every 3 seconds
        _presentationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _presentationTimer.Tick += OnPresentationTimerTick;
        _presentationTimer.Start();

        // Show first-run balloon tip
        if (!_settingsManager.Settings.HasShownWelcome && _trayIcon != null)
        {
            _trayIcon.ShowBalloonTip(
                5000,
                "Notifications Pro",
                "Notifications Pro is running in the tray. Right-click for settings, quick theme switching, and focus mode.",
                WinForms.ToolTipIcon.Info);
        }

        // Initialize notification listener — will prompt for permission on first run
        await _notificationListener.InitializeAsync();
    }

    private void SyncStartupRegistryState()
    {
        if (_settingsManager == null) return;
        var shouldStart = _settingsManager.Settings.StartWithWindows;
        var isRegistered = StartupHelper.IsStartupEnabled();

        if (shouldStart && !isRegistered)
            StartupHelper.EnableStartup();
        else if (!shouldStart && isRegistered)
            StartupHelper.DisableStartup();
    }

    private void ApplyHighContrastIfNeeded()
    {
        if (_settingsManager == null) return;
        if (!_settingsManager.Settings.RespectHighContrast) return;
        if (!SystemParameters.HighContrast) return;

        // Auto-apply the High Contrast built-in theme
        var hcTheme = ThemePreset.BuiltInThemes
            .FirstOrDefault(t => t.Name == "High Contrast");
        if (hcTheme == null) return;

        var current = _settingsManager.Settings;
        var updated = current.Clone();
        hcTheme.ApplyOverlayTo(updated);
        if (updated.LinkOverlayThemeAndUiTheme)
        {
            hcTheme.ApplySettingsWindowTo(updated);
            updated.SettingsThemeMode = hcTheme.Name;
        }
        else
        {
            CopySettingsUiTheme(current, updated);
        }
        _settingsManager.Apply(updated);
    }

    private void RefreshHotkeys()
    {
        _hotkeyManager?.Dispose();
        _hotkeyManager = null;

        if (_settingsManager == null || !_settingsManager.Settings.GlobalHotkeysEnabled)
            return;

        // Need an HWND from the overlay window
        var hwnd = _overlayWindow != null && _overlayWindow.IsLoaded
            ? new System.Windows.Interop.WindowInteropHelper(_overlayWindow).Handle
            : IntPtr.Zero;

        if (hwnd == IntPtr.Zero) return;

        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.ToggleOverlayRequested += () => Dispatcher.Invoke(ToggleOverlay);
        _hotkeyManager.DismissAllRequested += () => Dispatcher.Invoke(() => _queueManager?.ClearAll());
        _hotkeyManager.ToggleDndRequested += () => Dispatcher.Invoke(TogglePause);

        var s = _settingsManager.Settings;
        _hotkeyManager.Register(hwnd, s.HotkeyToggleOverlay, s.HotkeyDismissAll, s.HotkeyToggleDnd);
    }

    private void SetupTrayIcon()
    {
        var icon = IconHelper.CreateTrayIcon();

        _showHideItem = new WinForms.ToolStripMenuItem("Hide Overlay", null, (_, _) => ToggleOverlay());
        _pauseResumeItem = new WinForms.ToolStripMenuItem("Pause Notifications", null, (_, _) => TogglePause());
        _clickThroughItem = new WinForms.ToolStripMenuItem("Enable Click-Through", null, (_, _) => ToggleClickThrough());
        _alwaysOnTopItem = new WinForms.ToolStripMenuItem("Disable Always on Top", null, (_, _) => ToggleAlwaysOnTop());

        var contextMenu = new WinForms.ContextMenuStrip();
        contextMenu.BackColor = Drawing.Color.FromArgb(28, 28, 28);
        contextMenu.ForeColor = Drawing.Color.FromArgb(243, 243, 243);
        contextMenu.Renderer = new DarkMenuRenderer();

        _statusItem = new WinForms.ToolStripMenuItem("Initializing...") { Enabled = false };
        _grantAccessItem = new WinForms.ToolStripMenuItem("Open Privacy > Notifications...", null, (_, _) => OpenNotificationSettings())
        {
            Visible = true
        };
        var retryAccessItem = new WinForms.ToolStripMenuItem("Retry Access Check", null, async (_, _) =>
        {
            if (_notificationListener != null)
                await _notificationListener.RetryAccessAsync();
        });

        contextMenu.Items.Add(_statusItem);
        contextMenu.Items.Add(_grantAccessItem);
        contextMenu.Items.Add(retryAccessItem);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add(_showHideItem);
        contextMenu.Items.Add(_pauseResumeItem);
        contextMenu.Items.Add(_alwaysOnTopItem);
        contextMenu.Items.Add(_clickThroughItem);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());

        // Focus mode submenu
        _focusModeItem = new WinForms.ToolStripMenuItem("Focus Mode");
        _focusModeItem.DropDownItems.Add("Focus for 15 min", null, (_, _) => StartFocusMode(15));
        _focusModeItem.DropDownItems.Add("Focus for 30 min", null, (_, _) => StartFocusMode(30));
        _focusModeItem.DropDownItems.Add("Focus for 60 min", null, (_, _) => StartFocusMode(60));
        _focusModeItem.DropDownItems.Add(new WinForms.ToolStripSeparator());
        _focusModeItem.DropDownItems.Add("Cancel Focus", null, (_, _) => CancelFocusMode());
        contextMenu.Items.Add(_focusModeItem);

        // Quick mute submenu — populated dynamically when opened
        _quickMuteItem = new WinForms.ToolStripMenuItem("Quick Mute App");
        contextMenu.Items.Add(_quickMuteItem);

        // Theme quick-switch submenu — populated dynamically when opened
        _themeSwitchItem = new WinForms.ToolStripMenuItem("Switch Theme");
        contextMenu.Items.Add(_themeSwitchItem);

        contextMenu.Opening += OnTrayMenuOpening;

        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add("Clear All Notifications", null, (_, _) => _queueManager?.ClearAll());
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add("View Session Archive", null, (_, _) => ViewSessionArchive());
        contextMenu.Items.Add("Settings...", null, (_, _) => ShowSettings());
        contextMenu.Items.Add("About Notifications Pro", null, (_, _) => ShowAboutDialog());
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add("Quit", null, (_, _) => QuitApp());

        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = icon,
            Text = "Notifications Pro",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) => ShowSettings();
        UpdateMenuLabels();
    }

    private void ShowOverlay()
    {
        if (_overlayWindow == null || !_overlayWindow.IsLoaded)
        {
            _overlayWindow = new OverlayWindow(_overlayViewModel!, _settingsManager!);
        }
        _overlayWindow.Show();
        _settingsManager!.Settings.OverlayVisible = true;
        UpdateMenuLabels();

        // Hotkeys need a valid HWND — try registering now that the window is shown
        if (_hotkeyManager == null && _settingsManager.Settings.GlobalHotkeysEnabled)
            RefreshHotkeys();
    }

    private void HideOverlay()
    {
        _overlayWindow?.Hide();
        _settingsManager!.Settings.OverlayVisible = false;
        UpdateMenuLabels();
    }

    private void ToggleOverlay()
    {
        if (_overlayWindow?.IsVisible == true)
            HideOverlay();
        else
            ShowOverlay();
    }

    private void TogglePause()
    {
        if (_queueManager!.IsPaused)
            _queueManager.Resume();
        else
            _queueManager.Pause();

        UpdateMenuLabels();
    }

    private void ToggleClickThrough()
    {
        if (_settingsManager == null) return;

        var updated = _settingsManager.Settings.Clone();
        updated.ClickThrough = !updated.ClickThrough;
        _settingsManager.Apply(updated);
        UpdateMenuLabels();
    }

    private void ToggleAlwaysOnTop()
    {
        if (_settingsManager == null) return;

        var updated = _settingsManager.Settings.Clone();
        updated.AlwaysOnTop = !updated.AlwaysOnTop;
        _settingsManager.Apply(updated);
        UpdateMenuLabels();
    }

    private void UpdateMenuLabels()
    {
        var hotkeysOn = _settingsManager?.Settings.GlobalHotkeysEnabled == true;
        var s = _settingsManager?.Settings;

        if (_showHideItem != null)
        {
            var label = _overlayWindow?.IsVisible == true ? "Hide Overlay" : "Show Overlay";
            if (hotkeysOn && s != null && !string.IsNullOrWhiteSpace(s.HotkeyToggleOverlay))
                label += $"    {s.HotkeyToggleOverlay}";
            _showHideItem.Text = label;
        }

        var isPaused = _queueManager?.IsPaused == true;
        if (_pauseResumeItem != null)
        {
            var label = isPaused ? "Resume Notifications" : "Pause Notifications";
            if (hotkeysOn && s != null && !string.IsNullOrWhiteSpace(s.HotkeyToggleDnd))
                label += $"    {s.HotkeyToggleDnd}";
            _pauseResumeItem.Text = label;
            _pauseResumeItem.Checked = isPaused;
        }

        if (_clickThroughItem != null)
        {
            var isClickThrough = _settingsManager?.Settings.ClickThrough == true;
            _clickThroughItem.Text = isClickThrough
                ? "Click-Through (Clicks Pass Through)"
                : "Click-Through";
            _clickThroughItem.Checked = isClickThrough;
        }

        if (_alwaysOnTopItem != null)
        {
            var isOnTop = _settingsManager?.Settings.AlwaysOnTop == true;
            _alwaysOnTopItem.Text = "Always on Top";
            _alwaysOnTopItem.Checked = isOnTop;
        }

        if (_focusModeItem != null && _focusTimer == null)
            _focusModeItem.Text = "Focus Mode";

        UpdateTrayIcon();
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon == null) return;

        var isPaused = _queueManager?.IsPaused == true;
        var visibleCount = _queueManager?.VisibleNotifications.Count ?? 0;

        Drawing.Icon newIcon;
        if (isPaused)
            newIcon = IconHelper.CreateDimmedTrayIcon();
        else if (visibleCount > 0)
            newIcon = IconHelper.CreateBadgedTrayIcon(visibleCount);
        else
            newIcon = IconHelper.CreateTrayIcon();

        var oldIcon = _trayIcon.Icon;
        _trayIcon.Icon = newIcon;
        oldIcon?.Dispose();
    }

    private void ShowSettings()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsViewModel = new SettingsViewModel(_settingsManager!, _queueManager!);
            _settingsWindow = new SettingsWindow(_settingsViewModel, _settingsManager);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;

            var settings = _settingsManager!.Settings;
            if (settings.SettingsDisplayMode == "Popup")
            {
                _settingsWindow.WindowStyle = WindowStyle.None;
                _settingsWindow.ResizeMode = ResizeMode.NoResize;
                _settingsWindow.ShowInTaskbar = false;
                _settingsWindow.AllowsTransparency = true;
                _settingsWindow.Background = System.Windows.Media.Brushes.Transparent;
                _settingsWindow.WindowStartupLocation = WindowStartupLocation.Manual;

                var popupBounds = CalculateSettingsPopupBounds(_settingsWindow.Width, _settingsWindow.Height);
                _settingsWindow.Width = popupBounds.Width;
                _settingsWindow.Height = popupBounds.Height;
                _settingsWindow.Left = popupBounds.Left;
                _settingsWindow.Top = popupBounds.Top;

                if (settings.PopupAutoClose)
                {
                    _settingsWindow.Deactivated += OnSettingsWindowDeactivated;
                }
            }
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    /// <summary>
    /// Finds the screen rectangle of the Windows system tray notification area.
    /// </summary>
    private static System.Drawing.Rectangle? GetTrayNotificationAreaRect()
    {
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero) return null;

        var trayNotify = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        if (trayNotify == IntPtr.Zero) return null;

        if (!GetWindowRect(trayNotify, out var rect)) return null;

        return new System.Drawing.Rectangle(
            rect.Left, rect.Top,
            rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    private static System.Drawing.Rectangle? GetTaskbarRect()
    {
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero) return null;
        if (!GetWindowRect(taskbar, out var rect)) return null;

        return new System.Drawing.Rectangle(
            rect.Left, rect.Top,
            rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    private static Rect CalculateSettingsPopupBounds(double requestedWidth, double requestedHeight)
    {
        const double margin = 12;

        var trayRect = GetTrayNotificationAreaRect();
        var taskbarRect = GetTaskbarRect();
        var anchorRect = trayRect ?? taskbarRect;

        var screens = WinForms.Screen.AllScreens;
        var screen = anchorRect.HasValue
            ? WinForms.Screen.FromRectangle(anchorRect.Value)
            : (WinForms.Screen.PrimaryScreen ?? (screens.Length > 0 ? screens[0] : null));
        if (screen == null)
            return new Rect(0, 0, Math.Max(320, requestedWidth), Math.Max(280, requestedHeight));
        var workArea = screen.WorkingArea;

        var preferredWidth = requestedWidth > 0 ? requestedWidth : 640;
        var maxWidth = Math.Max(320, workArea.Width - (margin * 2));
        var minWidth = Math.Min(400, maxWidth);
        var width = Math.Clamp(preferredWidth, minWidth, maxWidth);

        var preferredHeight = Math.Max(requestedHeight, workArea.Height * 0.55);
        var maxHeight = Math.Max(280, workArea.Height - (margin * 2));
        var minHeight = Math.Min(380, maxHeight);
        var height = Math.Clamp(preferredHeight, minHeight, maxHeight);

        var left = workArea.Right - width - margin;
        var top = workArea.Bottom - height - margin;

        if (taskbarRect.HasValue)
        {
            var edge = DetectTaskbarEdge(screen.Bounds, taskbarRect.Value);
            if (edge == TaskbarEdge.Top)
                top = workArea.Top + margin;
            else if (edge == TaskbarEdge.Left)
                left = workArea.Left + margin;
            else if (edge == TaskbarEdge.Right)
                left = workArea.Right - width - margin;
        }

        left = Math.Clamp(left, workArea.Left + 2, workArea.Right - width - 2);
        top = Math.Clamp(top, workArea.Top + 2, workArea.Bottom - height - 2);

        return new Rect(left, top, width, height);
    }

    private static TaskbarEdge DetectTaskbarEdge(System.Drawing.Rectangle screenBounds, System.Drawing.Rectangle taskbarRect)
    {
        var horizontalTaskbar = taskbarRect.Width >= taskbarRect.Height;
        if (horizontalTaskbar)
        {
            var distanceToTop = Math.Abs(taskbarRect.Top - screenBounds.Top);
            var distanceToBottom = Math.Abs(screenBounds.Bottom - taskbarRect.Bottom);
            return distanceToTop <= distanceToBottom ? TaskbarEdge.Top : TaskbarEdge.Bottom;
        }

        var distanceToLeft = Math.Abs(taskbarRect.Left - screenBounds.Left);
        var distanceToRight = Math.Abs(screenBounds.Right - taskbarRect.Right);
        return distanceToLeft <= distanceToRight ? TaskbarEdge.Left : TaskbarEdge.Right;
    }

    private void OnSettingsWindowDeactivated(object? sender, EventArgs e)
    {
        if (_settingsWindow != null && _settingsManager?.Settings.PopupAutoClose == true
            && _settingsManager.Settings.SettingsDisplayMode == "Popup")
        {
            _settingsWindow.Close();
        }
    }

    private void UpdateStatusItem()
    {
        Dispatcher.Invoke(() =>
        {
            if (_statusItem != null && _notificationListener != null)
            {
                _statusItem.Text = _notificationListener.StatusMessage;
            }

            if (_grantAccessItem != null)
            {
                // Always show — even when API says "Allowed", the user may need
                // to toggle notification access in Windows Settings for it to work
                _grantAccessItem.Visible = true;
            }

            if (_trayIcon != null && _notificationListener != null)
            {
                // Show diagnostic status in tooltip (visible on hover)
                // NotifyIcon.Text max is 127 chars
                var mode = _notificationListener.ListenerMode;
                var isPaused = _queueManager?.IsPaused == true;
                var pausedSuffix = isPaused ? " | Paused" : string.Empty;
                var clickThroughSuffix = _settingsManager?.Settings.ClickThrough == true
                    ? " | Click-through" : string.Empty;
                var tooltip = $"Notifications Pro — Listening via {mode}{pausedSuffix}{clickThroughSuffix}\n{_notificationListener.StatusMessage}";
                if (tooltip.Length > 127) tooltip = tooltip[..127];
                _trayIcon.Text = tooltip;
            }

            UpdateMenuLabels();
        });
    }

    private void OpenNotificationSettings()
    {
        try
        {
            // Privacy > Notifications — where apps get ACCESS to read notifications.
            // NOT ms-settings:notifications, which is System > Notifications (configuration).
            Process.Start(new ProcessStartInfo("ms-settings:privacy-notifications") { UseShellExecute = true });
        }
        catch { }
    }

    private void StartFocusMode(int minutes)
    {
        if (_queueManager == null) return;
        _queueManager.Pause();
        _focusEndTime = DateTime.Now.AddMinutes(minutes);

        _focusTimer?.Stop();
        _focusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _focusTimer.Tick += OnFocusTimerTick;
        _focusTimer.Start();
        UpdateMenuLabels();
    }

    private void CancelFocusMode()
    {
        _focusTimer?.Stop();
        _focusTimer = null;
        _queueManager?.Resume();
        UpdateMenuLabels();
    }

    private void OnFocusTimerTick(object? sender, EventArgs e)
    {
        var remaining = _focusEndTime - DateTime.Now;
        if (remaining <= TimeSpan.Zero)
        {
            CancelFocusMode();
            return;
        }

        if (_focusModeItem != null)
            _focusModeItem.Text = $"Focus Mode ({remaining.Minutes}:{remaining.Seconds:D2} left)";
    }

    private void OnPresentationTimerTick(object? sender, EventArgs e)
    {
        if (_settingsManager == null || _queueManager == null) return;
        if (!_settingsManager.Settings.PresentationModeEnabled) return;

        var isFullscreen = FullscreenHelper.IsPresentationAppFullscreen(
            _settingsManager.Settings.PresentationApps);

        if (isFullscreen && !_presentationDndActive)
        {
            _presentationDndActive = true;
            if (!_queueManager.IsPaused)
            {
                _queueManager.Pause();
                UpdateMenuLabels();
            }
        }
        else if (!isFullscreen && _presentationDndActive)
        {
            _presentationDndActive = false;
            if (_queueManager.IsPaused)
            {
                _queueManager.Resume();
                UpdateMenuLabels();
            }
        }
    }

    private void OnTrayMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        PopulateQuickMuteMenu();
        PopulateThemeSwitchMenu();
    }

    private void PopulateQuickMuteMenu()
    {
        if (_quickMuteItem == null || _queueManager == null) return;

        _quickMuteItem.DropDownItems.Clear();
        var apps = _queueManager.SeenAppNames.OrderBy(a => a, StringComparer.OrdinalIgnoreCase).ToList();
        if (apps.Count == 0)
        {
            _quickMuteItem.DropDownItems.Add(new WinForms.ToolStripMenuItem("(no apps seen yet)") { Enabled = false });
            return;
        }

        foreach (var app in apps)
        {
            var isMuted = _queueManager.IsAppMuted(app);
            var label = isMuted ? $"Unmute: {app}" : $"Mute: {app}";
            var capturedApp = app;
            _quickMuteItem.DropDownItems.Add(label, null, (_, _) =>
            {
                if (_queueManager.IsAppMuted(capturedApp))
                    _queueManager.UnmuteApp(capturedApp);
                else
                    _queueManager.MuteApp(capturedApp);
            });
        }
    }

    private void PopulateThemeSwitchMenu()
    {
        if (_themeSwitchItem == null || _settingsManager == null || _themeManager == null) return;

        _themeSwitchItem.DropDownItems.Clear();

        foreach (var theme in ThemePreset.BuiltInThemes)
        {
            var captured = theme;
            _themeSwitchItem.DropDownItems.Add(theme.Name, null, (_, _) => ApplyThemeFromTray(captured));
        }

        var customThemes = _themeManager.LoadCustomThemes();
        if (customThemes.Count > 0)
        {
            _themeSwitchItem.DropDownItems.Add(new WinForms.ToolStripSeparator());
            foreach (var theme in customThemes)
            {
                var captured = theme;
                _themeSwitchItem.DropDownItems.Add(theme.Name, null, (_, _) => ApplyThemeFromTray(captured));
            }
        }
    }

    private void ApplyThemeFromTray(ThemePreset theme)
    {
        if (_settingsManager == null) return;
        var current = _settingsManager.Settings;
        var updated = current.Clone();
        theme.ApplyOverlayTo(updated);
        if (updated.LinkOverlayThemeAndUiTheme)
        {
            theme.ApplySettingsWindowTo(updated);
            updated.SettingsThemeMode = theme.Name;
        }
        else
        {
            // Keep settings-window palette unchanged when themes are unlinked.
            CopySettingsUiTheme(current, updated);
        }
        _settingsManager.Apply(updated);
    }

    private static void CopySettingsUiTheme(AppSettings source, AppSettings target)
    {
        target.SettingsThemeMode = source.SettingsThemeMode;
        target.SettingsWindowBg = source.SettingsWindowBg;
        target.SettingsWindowSurface = source.SettingsWindowSurface;
        target.SettingsWindowSurfaceLight = source.SettingsWindowSurfaceLight;
        target.SettingsWindowSurfaceHover = source.SettingsWindowSurfaceHover;
        target.SettingsWindowText = source.SettingsWindowText;
        target.SettingsWindowTextSecondary = source.SettingsWindowTextSecondary;
        target.SettingsWindowTextMuted = source.SettingsWindowTextMuted;
        target.SettingsWindowAccent = source.SettingsWindowAccent;
        target.SettingsWindowBorder = source.SettingsWindowBorder;
    }

    public void ShowSettingsForApp(string appName)
    {
        ShowSettings();
        if (_settingsWindow != null)
        {
            _settingsWindow.NavigateToTab("Filtering");
        }
    }

    private void ViewSessionArchive()
    {
        if (_queueManager == null) return;
        var archive = _queueManager.SessionArchive;
        if (archive.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "No archived notifications yet.\n\nEnable Session Archive in Settings > Behavior to start recording notifications in memory.",
                "Session Archive", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var lines = archive
            .AsEnumerable().Reverse()
            .Select(a =>
            {
                var elapsed = DateTime.Now - a.ReceivedAt;
                var timeStr = elapsed.TotalMinutes < 1 ? "just now"
                    : elapsed.TotalMinutes < 60 ? $"{(int)elapsed.TotalMinutes}m ago"
                    : $"{(int)elapsed.TotalHours}h ago";
                var parts = new[] { a.AppName, a.Title, a.Body }
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                return $"[{timeStr}] {string.Join(" — ", parts)}";
            });
        var text = string.Join("\n", lines);
        System.Windows.Clipboard.SetText(text);
        System.Windows.MessageBox.Show(
            $"{archive.Count} archived notification(s) copied to clipboard.\n\nThis data exists only in RAM and will be cleared when the app closes.",
            "Session Archive", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowAboutDialog()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        var listenerMode = _notificationListener != null
            ? (_notificationListener.IsAccessGranted ? "WinRT" : "Accessibility")
            : "Unknown";

        System.Windows.MessageBox.Show(
            $"Notifications Pro v{versionStr}\n\n" +
            $"A Windows tray app that mirrors toast notifications\ninto a customizable always-on-top overlay.\n\n" +
            $"Listener: {listenerMode}\n" +
            $".NET {Environment.Version}\n\n" +
            $"License: MIT\n" +
            $"GitHub: github.com/lwytch/Notifications-Pro",
            "About Notifications Pro",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void QuitApp()
    {
        _presentationTimer?.Stop();
        _focusTimer?.Stop();
        if (_highContrastHandler != null)
            SystemParameters.StaticPropertyChanged -= _highContrastHandler;
        _hotkeyManager?.Dispose();
        _notificationListener?.Stop();
        _settingsManager?.Save();
        _trayIcon?.Dispose();
        _overlayWindow?.Close();
        _settingsWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}

/// <summary>
/// Custom renderer for dark-themed tray context menu.
/// </summary>
internal class DarkMenuRenderer : WinForms.ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkMenuColors()) { }

    protected override void OnRenderItemText(WinForms.ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Drawing.Color.FromArgb(243, 243, 243);
        base.OnRenderItemText(e);
    }
}

internal class DarkMenuColors : WinForms.ProfessionalColorTable
{
    public override Drawing.Color MenuItemSelected => Drawing.Color.FromArgb(45, 45, 45);
    public override Drawing.Color MenuItemBorder => Drawing.Color.FromArgb(65, 65, 65);
    public override Drawing.Color MenuBorder => Drawing.Color.FromArgb(65, 65, 65);
    public override Drawing.Color MenuItemSelectedGradientBegin => Drawing.Color.FromArgb(45, 45, 45);
    public override Drawing.Color MenuItemSelectedGradientEnd => Drawing.Color.FromArgb(45, 45, 45);
    public override Drawing.Color MenuItemPressedGradientBegin => Drawing.Color.FromArgb(38, 38, 38);
    public override Drawing.Color MenuItemPressedGradientEnd => Drawing.Color.FromArgb(38, 38, 38);
    public override Drawing.Color ToolStripDropDownBackground => Drawing.Color.FromArgb(28, 28, 28);
    public override Drawing.Color ImageMarginGradientBegin => Drawing.Color.FromArgb(28, 28, 28);
    public override Drawing.Color ImageMarginGradientMiddle => Drawing.Color.FromArgb(28, 28, 28);
    public override Drawing.Color ImageMarginGradientEnd => Drawing.Color.FromArgb(28, 28, 28);
    public override Drawing.Color SeparatorDark => Drawing.Color.FromArgb(65, 65, 65);
    public override Drawing.Color SeparatorLight => Drawing.Color.FromArgb(65, 65, 65);
}
