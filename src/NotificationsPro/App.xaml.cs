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

        // Apply settings window theme (Dark/Light/System)
        Services.SettingsThemeService.ApplySettingsTheme(_settingsManager.Settings);
        _settingsManager.SettingsChanged += () =>
            Services.SettingsThemeService.ApplySettingsTheme(_settingsManager.Settings);

        // Apply High Contrast theme if active and respected
        ApplyHighContrastIfNeeded();
        SystemParameters.StaticPropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SystemParameters.HighContrast))
                Dispatcher.Invoke(ApplyHighContrastIfNeeded);
        };

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
                "Notifications Pro is running. Right-click the tray icon for settings.",
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

        var updated = _settingsManager.Settings.Clone();
        hcTheme.ApplyTo(updated);
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
        contextMenu.BackColor = Drawing.Color.FromArgb(30, 30, 46);
        contextMenu.ForeColor = Drawing.Color.FromArgb(228, 228, 239);
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
        contextMenu.Items.Add("Settings...", null, (_, _) => ShowSettings());
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
        if (_showHideItem != null)
            _showHideItem.Text = _overlayWindow?.IsVisible == true ? "Hide Overlay" : "Show Overlay";

        var isPaused = _queueManager?.IsPaused == true;
        if (_pauseResumeItem != null)
        {
            _pauseResumeItem.Text = isPaused ? "Resume Notifications" : "Pause Notifications";
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
                _settingsWindow.WindowStartupLocation = WindowStartupLocation.Manual;

                var windowWidth = _settingsWindow.Width;

                // Use 70% of primary screen height with padding
                var primaryScreen = WinForms.Screen.PrimaryScreen ?? WinForms.Screen.AllScreens[0];
                var workArea = primaryScreen.WorkingArea;
                var windowHeight = workArea.Height * 0.7;
                _settingsWindow.Height = windowHeight;

                // Position fixed above the system tray notification area
                var trayRect = GetTrayNotificationAreaRect();
                if (trayRect.HasValue)
                {
                    var r = trayRect.Value;
                    _settingsWindow.Left = Math.Max(workArea.Left, r.Right - windowWidth);
                    _settingsWindow.Top = r.Top - windowHeight - 8;
                }
                else
                {
                    _settingsWindow.Left = workArea.Right - windowWidth - 8;
                    _settingsWindow.Top = workArea.Bottom - windowHeight - 8;
                }

                // Clamp to work area bounds
                if (_settingsWindow.Top < workArea.Top)
                    _settingsWindow.Top = workArea.Top;

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
    /// Finds the screen rectangle of the Windows system tray notification area
    /// using Win32 window hierarchy: Shell_TrayWnd > TrayNotifyWnd.
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
                var clickThroughSuffix = _settingsManager?.Settings.ClickThrough == true
                    ? " | click-through ON"
                    : string.Empty;
                var tooltip = $"Notifications Pro\n{_notificationListener.StatusMessage}{clickThroughSuffix}";
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
        var updated = _settingsManager.Settings.Clone();
        theme.ApplyTo(updated);
        _settingsManager.Apply(updated);
    }

    private void QuitApp()
    {
        _presentationTimer?.Stop();
        _focusTimer?.Stop();
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
        e.TextColor = Drawing.Color.FromArgb(228, 228, 239);
        base.OnRenderItemText(e);
    }
}

internal class DarkMenuColors : WinForms.ProfessionalColorTable
{
    public override Drawing.Color MenuItemSelected => Drawing.Color.FromArgb(52, 52, 80);
    public override Drawing.Color MenuItemBorder => Drawing.Color.FromArgb(54, 54, 80);
    public override Drawing.Color MenuBorder => Drawing.Color.FromArgb(54, 54, 80);
    public override Drawing.Color MenuItemSelectedGradientBegin => Drawing.Color.FromArgb(52, 52, 80);
    public override Drawing.Color MenuItemSelectedGradientEnd => Drawing.Color.FromArgb(52, 52, 80);
    public override Drawing.Color MenuItemPressedGradientBegin => Drawing.Color.FromArgb(40, 40, 64);
    public override Drawing.Color MenuItemPressedGradientEnd => Drawing.Color.FromArgb(40, 40, 64);
    public override Drawing.Color ToolStripDropDownBackground => Drawing.Color.FromArgb(30, 30, 46);
    public override Drawing.Color ImageMarginGradientBegin => Drawing.Color.FromArgb(30, 30, 46);
    public override Drawing.Color ImageMarginGradientMiddle => Drawing.Color.FromArgb(30, 30, 46);
    public override Drawing.Color ImageMarginGradientEnd => Drawing.Color.FromArgb(30, 30, 46);
    public override Drawing.Color SeparatorDark => Drawing.Color.FromArgb(54, 54, 80);
    public override Drawing.Color SeparatorLight => Drawing.Color.FromArgb(54, 54, 80);
}
