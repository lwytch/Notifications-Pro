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
using Windows.ApplicationModel;
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
    private SpokenNotificationService? _spokenNotificationService;

    private WinForms.ToolStripMenuItem? _showHideItem;
    private WinForms.ToolStripMenuItem? _pauseResumeItem;
    private WinForms.ToolStripMenuItem? _clickThroughItem;
    private WinForms.ToolStripMenuItem? _alwaysOnTopItem;
    private WinForms.ToolStripMenuItem? _statusItem;
    private WinForms.ToolStripMenuItem? _grantAccessItem;
    private WinForms.ToolStripMenuItem? _focusModeItem;
    private WinForms.ToolStripMenuItem? _quickMuteItem;
    private WinForms.ToolStripMenuItem? _themeSwitchItem;
    private WinForms.ToolStripMenuItem? _profileSwitchItem;
    private ProfileManager? _profileManager;
    private DispatcherTimer? _focusTimer;
    private DateTime _focusEndTime;
    private ThemeManager? _themeManager;
    private DispatcherTimer? _presentationTimer;
    private bool _presentationDndActive;
    private DispatcherTimer? _themeScheduleTimer;
    private string? _lastScheduledTheme;
    private System.ComponentModel.PropertyChangedEventHandler? _highContrastHandler;
    private bool _suppressSettingsWindowAutoRefresh;
    private bool _pendingSettingsWindowBulkRefresh;

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

    private static bool HasPackageIdentity()
    {
        try
        {
            _ = Package.Current.Id.FullName;
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers — show error details instead of silently closing
        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(
                $"Unhandled UI exception:\n\n{args.Exception}",
                "Notifications Pro — Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                System.Windows.MessageBox.Show(
                    $"Unhandled exception:\n\n{ex}",
                    "Notifications Pro — Fatal Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            args.SetObserved();
        };

        // Only unpackaged desktop runs need an explicit AUMID. In the MSIX build
        // Windows already provides package identity, and overriding it creates a
        // second notification identity that can break listener permissions.
        if (!HasPackageIdentity())
            SetCurrentProcessExplicitAppUserModelID("NotificationsPro.App");

        _settingsManager = new SettingsManager();
        _settingsManager.Load();
        _themeManager = new ThemeManager();
        _profileManager = new ProfileManager();

        // Sync the packaged StartupTask with the saved setting
        SyncStartupTaskState();

        _queueManager = new QueueManager(_settingsManager);
        _spokenNotificationService = new SpokenNotificationService(_queueManager, _settingsManager, Dispatcher);

        _notificationListener = new NotificationListener(_queueManager, Dispatcher, _settingsManager);
        _notificationListener.StatusChanged += UpdateStatusItem;

        _overlayViewModel = new OverlayViewModel(_queueManager, _settingsManager);
        _settingsViewModel = new SettingsViewModel(_settingsManager, _queueManager);
        _settingsViewModel.ConfigureRetryNotificationAccess(() =>
            _notificationListener?.RetryAccessAsync() ?? Task.CompletedTask);
        _settingsViewModel.ConfigureRefreshSettingsWindow(RefreshSettingsWindowAfterBulkApply);
        _settingsViewModel.ConfigureSettingsWindowBulkApplyState(SetSettingsWindowBulkApplyState);
        UpdateSettingsDiagnostics();

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
        _settingsManager.SettingsChanged += RefreshOpenSettingsWindowFromCurrentSettings;

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

        // Theme schedule: check every 60 seconds for time-based theme switching
        _themeScheduleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _themeScheduleTimer.Tick += OnThemeScheduleTimerTick;
        _themeScheduleTimer.Start();
        OnThemeScheduleTimerTick(null, EventArgs.Empty);

        // Show first-run balloon tip only when there was no existing settings file.
        if (!_settingsManager.HadExistingSettingsFile)
        {
            if (_trayIcon != null)
            {
                _trayIcon.ShowBalloonTip(
                    5000,
                    "Notifications Pro",
                    "Notifications Pro is running in the tray. Right-click for settings, quick theme switching, and focus mode.",
                    WinForms.ToolTipIcon.Info);
            }
        }

        // Initialize notification listener — will prompt for permission on first run
        await _notificationListener.InitializeAsync();

        // Process CLI arguments after full initialization
        ProcessCliArguments(e.Args);
    }

    private void ProcessCliArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();
            switch (arg)
            {
                case "--pause":
                    _queueManager?.Pause();
                    UpdateMenuLabels();
                    break;
                case "--resume":
                    _queueManager?.Resume();
                    UpdateMenuLabels();
                    break;
                case "--theme" when i + 1 < args.Length:
                    ApplyThemeByName(args[++i]);
                    break;
                case "--send-test":
                    _settingsViewModel?.PreviewNotificationCommand.Execute(null);
                    break;
                case "--hide":
                    HideOverlay();
                    break;
                case "--show":
                    ShowOverlay();
                    break;
            }
        }
    }

    private void ApplyThemeByName(string themeName)
    {
        var theme = ThemePreset.BuiltInThemes
            .FirstOrDefault(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
        if (theme == null && _themeManager != null)
            theme = _themeManager.LoadCustomThemes()
                .FirstOrDefault(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
        if (theme != null)
            ApplyThemeFromTray(theme);
    }

    private void OnThemeScheduleTimerTick(object? sender, EventArgs e)
    {
        if (_settingsManager == null || !_settingsManager.Settings.ThemeScheduleEnabled)
            return;

        var settings = _settingsManager.Settings;
        if (!TimeSpan.TryParse(settings.DayStartTime, out var dayStart) ||
            !TimeSpan.TryParse(settings.NightStartTime, out var nightStart))
            return;

        var now = DateTime.Now.TimeOfDay;
        var isDaytime = dayStart < nightStart
            ? now >= dayStart && now < nightStart
            : now >= dayStart || now < nightStart;

        var targetTheme = isDaytime ? settings.DayThemeName : settings.NightThemeName;
        if (string.Equals(targetTheme, _lastScheduledTheme, StringComparison.OrdinalIgnoreCase))
            return;

        _lastScheduledTheme = targetTheme;
        ApplyThemeByName(targetTheme);
    }

    private async void SyncStartupTaskState()
    {
        if (_settingsManager == null) return;
        var shouldStart = _settingsManager.Settings.StartWithWindows;
        var isRegistered = await StartupHelper.IsStartupEnabledAsync();

        if (shouldStart && !isRegistered)
            await StartupHelper.EnableStartupAsync();
        else if (!shouldStart && isRegistered)
            await StartupHelper.DisableStartupAsync();
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
        {
            UpdateSettingsDiagnostics();
            return;
        }

        // Need an HWND from the overlay window
        var hwnd = _overlayWindow != null && _overlayWindow.IsLoaded
            ? new System.Windows.Interop.WindowInteropHelper(_overlayWindow).Handle
            : IntPtr.Zero;

        if (hwnd == IntPtr.Zero)
        {
            UpdateSettingsDiagnostics();
            return;
        }

        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.ToggleOverlayRequested += () => Dispatcher.Invoke(ToggleOverlay);
        _hotkeyManager.DismissAllRequested += () => Dispatcher.Invoke(() => _queueManager?.ClearAll());
        _hotkeyManager.ToggleDndRequested += () => Dispatcher.Invoke(TogglePause);

        var s = _settingsManager.Settings;
        _hotkeyManager.Register(hwnd, s.HotkeyToggleOverlay, s.HotkeyDismissAll, s.HotkeyToggleDnd);
        UpdateSettingsDiagnostics();
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

        // Profile quick-switch submenu — populated dynamically when opened
        _profileSwitchItem = new WinForms.ToolStripMenuItem("Switch Profile");
        contextMenu.Items.Add(_profileSwitchItem);

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
        EnsureSettingsWindow();

        _settingsWindow!.Show();
        _settingsWindow.Activate();
    }

    private void EnsureSettingsWindow()
    {
        if (_settingsWindow != null && _settingsWindow.IsLoaded)
            return;

        _settingsViewModel ??= new SettingsViewModel(_settingsManager!, _queueManager!);
        _settingsViewModel.ConfigureRetryNotificationAccess(() =>
            _notificationListener?.RetryAccessAsync() ?? Task.CompletedTask);
        _settingsViewModel.ConfigureRefreshSettingsWindow(RefreshSettingsWindowAfterBulkApply);
        _settingsViewModel.ConfigureSettingsWindowBulkApplyState(SetSettingsWindowBulkApplyState);
        UpdateSettingsDiagnostics();
        _settingsWindow = new SettingsWindow(_settingsViewModel, _settingsManager);
        _settingsWindow.Closed += OnSettingsWindowClosed;
        InitializeSettingsWindowShell(_settingsWindow, _settingsManager!.Settings, repositionPopup: true);
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        if (sender is SettingsWindow window)
            window.Deactivated -= OnSettingsWindowDeactivated;

        if (ReferenceEquals(sender, _settingsWindow))
            _settingsWindow = null;
    }

    private void InitializeSettingsWindowShell(SettingsWindow window, AppSettings settings, bool repositionPopup)
    {
        var popupMode = string.Equals(settings.SettingsDisplayMode, "Popup", StringComparison.OrdinalIgnoreCase);
        if (popupMode)
        {
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.ShowInTaskbar = false;
            window.AllowsTransparency = true;
            window.Background = System.Windows.Media.Brushes.Transparent;
            window.WindowStartupLocation = WindowStartupLocation.Manual;

            if (repositionPopup)
            {
                var expectedWidth = double.IsNaN(window.Width)
                    ? (settings.CompactSettingsWindow ? 560.0 : 780.0)
                    : window.Width;
                var expectedHeight = double.IsNaN(window.Height) ? 560.0 : window.Height;
                var popupBounds = CalculateSettingsPopupBounds(expectedWidth, expectedHeight);
                window.Left = popupBounds.Left;
                window.Top = popupBounds.Top;
            }
        }
        else
        {
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.ResizeMode = ResizeMode.CanResize;
            window.ShowInTaskbar = true;
            window.SetResourceReference(Window.BackgroundProperty, "WindowBgBrush");
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        ApplySettingsWindowRuntimeState(window, settings, repositionPopup && popupMode);
    }

    private void ApplySettingsWindowRuntimeState(SettingsWindow window, AppSettings settings, bool repositionPopup)
    {
        var popupMode = string.Equals(settings.SettingsDisplayMode, "Popup", StringComparison.OrdinalIgnoreCase);

        if (popupMode && repositionPopup)
        {
            var expectedWidth = double.IsNaN(window.Width)
                ? (settings.CompactSettingsWindow ? 560.0 : 780.0)
                : window.Width;
            var expectedHeight = double.IsNaN(window.Height) ? 560.0 : window.Height;
            var popupBounds = CalculateSettingsPopupBounds(expectedWidth, expectedHeight);
            window.Left = popupBounds.Left;
            window.Top = popupBounds.Top;
        }

        window.Deactivated -= OnSettingsWindowDeactivated;
        if (popupMode && settings.PopupAutoClose)
            window.Deactivated += OnSettingsWindowDeactivated;

        window.RefreshChromeFromCurrentSettings();
    }

    private void RefreshOpenSettingsWindowFromCurrentSettings()
    {
        if (_suppressSettingsWindowAutoRefresh || _settingsWindow == null || _settingsManager == null)
            return;

        var settings = _settingsManager.Settings;
        if (_settingsWindow.IsLoaded)
        {
            var desiredPopup = string.Equals(settings.SettingsDisplayMode, "Popup", StringComparison.OrdinalIgnoreCase);
            if (_settingsWindow.IsPopupShellMode != desiredPopup)
            {
                RecreateSettingsWindow();
                return;
            }
        }

        ApplySettingsWindowRuntimeState(_settingsWindow, settings, repositionPopup: _settingsWindow.IsPopupShellMode);
    }

    private void RefreshSettingsWindowAfterBulkApply()
    {
        if (_settingsWindow == null || _settingsManager == null)
            return;

        if (_pendingSettingsWindowBulkRefresh)
            return;

        var settings = _settingsManager.Settings;
        if (string.Equals(settings.SettingsDisplayMode, "Popup", StringComparison.OrdinalIgnoreCase))
        {
            _pendingSettingsWindowBulkRefresh = true;
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                _pendingSettingsWindowBulkRefresh = false;
                if (_settingsWindow == null || _settingsManager == null)
                    return;

                if (string.Equals(_settingsManager.Settings.SettingsDisplayMode, "Popup", StringComparison.OrdinalIgnoreCase))
                    RecreateSettingsWindow();
                else
                    RefreshOpenSettingsWindowFromCurrentSettings();
            }));
            return;
        }

        RefreshOpenSettingsWindowFromCurrentSettings();
    }

    private void SetSettingsWindowBulkApplyState(bool isBulkApplying)
    {
        _suppressSettingsWindowAutoRefresh = isBulkApplying;
    }

    private void RecreateSettingsWindow()
    {
        if (_settingsWindow == null || _settingsManager == null || _settingsViewModel == null)
            return;

        var previousWindow = _settingsWindow;
        var selectedTabHeader = previousWindow.GetSelectedTabHeader();
        var wasVisible = previousWindow.IsVisible;

        previousWindow.Deactivated -= OnSettingsWindowDeactivated;
        previousWindow.Closed -= OnSettingsWindowClosed;
        _settingsWindow = null;
        previousWindow.Close();

        EnsureSettingsWindow();

        if (!string.IsNullOrWhiteSpace(selectedTabHeader))
            _settingsWindow?.NavigateToTab(selectedTabHeader);

        if (wasVisible)
        {
            _settingsWindow?.Show();
            _settingsWindow?.Activate();
        }
    }

    public static Rect CalculateSettingsPopupBounds(double requestedWidth, double requestedHeight)
    {
        var screens = WinForms.Screen.AllScreens;
        var screen = WinForms.Screen.PrimaryScreen ?? (screens.Length > 0 ? screens[0] : null);
        
        if (screen == null)
            return new Rect(0, 0, Math.Max(320, requestedWidth), Math.Max(280, requestedHeight));
            
        var workArea = screen.WorkingArea;

        var preferredWidth = requestedWidth > 0 ? requestedWidth : 640;
        var maxWidth = Math.Max(320, workArea.Width - 24);
        var minWidth = Math.Min(400, maxWidth);
        var width = Math.Clamp(preferredWidth, minWidth, maxWidth);

        var preferredHeight = Math.Max(requestedHeight, workArea.Height * 0.55);
        var maxHeight = Math.Max(280, workArea.Height - 24);
        var minHeight = Math.Min(380, maxHeight);
        var height = Math.Clamp(preferredHeight, minHeight, maxHeight);

        var left = workArea.Left + (workArea.Width - width) / 2.0;
        var top = workArea.Top + (workArea.Height - height) / 2.0;

        return new Rect(left, top, width, height);
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

            UpdateSettingsDiagnostics();
            UpdateMenuLabels();
        });
    }

    private void UpdateSettingsDiagnostics()
    {
        if (_settingsViewModel == null)
            return;

        if (_notificationListener != null)
        {
            _settingsViewModel.UpdateNotificationAccessStatus(
                _notificationListener.IsAccessGranted,
                _notificationListener.ListenerMode,
                _notificationListener.StatusMessage);
        }
        else
        {
            _settingsViewModel.UpdateNotificationAccessStatus(
                false,
                "Initializing",
                "Notification listener not initialized.");
        }

        _settingsViewModel.UpdateHotkeyRegistrationError(_hotkeyManager?.RegistrationError);
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
        PopulateProfileSwitchMenu();
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

    private void PopulateProfileSwitchMenu()
    {
        if (_profileSwitchItem == null || _profileManager == null || _settingsManager == null) return;

        _profileSwitchItem.DropDownItems.Clear();
        var profiles = _profileManager.GetProfileNames();
        if (profiles.Count == 0)
        {
            _profileSwitchItem.DropDownItems.Add(new WinForms.ToolStripMenuItem("(no saved profiles)") { Enabled = false });
            return;
        }
        foreach (var name in profiles)
        {
            var captured = name;
            _profileSwitchItem.DropDownItems.Add(name, null, (_, _) => ApplyProfileFromTray(captured));
        }
    }

    private void ApplyProfileFromTray(string profileName)
    {
        if (_profileManager == null || _settingsManager == null) return;
        var profile = _profileManager.LoadProfile(profileName);
        if (profile != null)
        {
            SetSettingsWindowBulkApplyState(true);
            try
            {
                profile.SettingsThemeMode = SettingsThemeService.ResolveThemeModeForLoadedSettings(profile);
                _settingsManager.Apply(profile);
                _settingsViewModel?.ReloadFromCurrentSettings();
                _settingsViewModel?.SyncResolvedSettingsThemeState();
            }
            finally
            {
                SetSettingsWindowBulkApplyState(false);
            }

            RefreshSettingsWindowAfterBulkApply();
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
        _settingsViewModel?.ReloadFromCurrentSettings();
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
        target.SettingsWindowOpacity = source.SettingsWindowOpacity;
        target.SettingsSurfaceOpacity = source.SettingsSurfaceOpacity;
        target.SettingsElementOpacity = source.SettingsElementOpacity;
        target.SettingsWindowCornerRadius = source.SettingsWindowCornerRadius;
    }

    public void ShowSettingsForApp(string appName)
    {
        ShowSettings();
        if (_settingsWindow != null)
        {
            _settingsWindow.NavigateToTab("Apps");
        }
    }

    private void ViewSessionArchive()
    {
        if (_queueManager == null) return;
        var archive = _queueManager.SessionArchive;
        if (archive.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "No archived notifications yet.\n\nEnable Session Archive in Settings > System to start recording notifications in memory.",
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
            $"{archive.Count} archived notification(s) copied to clipboard.\n\nNotifications Pro keeps the archive in RAM only and clears it when the app closes. Copying it here also hands the text to the Windows clipboard, which clipboard history or other clipboard tools may retain.",
            "Session Archive", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowAboutDialog()
    {
        var versionStr = GetCurrentVersionString();
        var packageIdentity = GetCurrentPackageIdentity();
        var installPath = GetCurrentInstallPath();
        var listenerMode = _notificationListener != null
            ? (_notificationListener.IsAccessGranted ? "WinRT" : "Accessibility")
            : "Unknown";
        var listenerStatus = _notificationListener?.StatusMessage ?? "No listener status available";

        System.Windows.MessageBox.Show(
            $"Notifications Pro v{versionStr}\n\n" +
            $"A Windows tray app that mirrors toast notifications\ninto a customizable always-on-top overlay.\n\n" +
            $"Package: {packageIdentity}\n" +
            $"Listener: {listenerMode}\n" +
            $"Status: {listenerStatus}\n" +
            $"Install Path: {installPath}\n" +
            $".NET {Environment.Version}\n\n" +
            $"License: GPL v3\n" +
            $"GitHub: github.com/lwytch/Notifications-Pro",
            "About Notifications Pro",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static string GetCurrentVersionString()
    {
        try
        {
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        catch
        {
            var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return assemblyVersion?.ToString(4) ?? "1.0.0.0";
        }
    }

    private static string GetCurrentPackageIdentity()
    {
        try
        {
            return Package.Current.Id.FullName;
        }
        catch
        {
            return "Unpackaged / developer run";
        }
    }

    private static string GetCurrentInstallPath()
    {
        try
        {
            return Package.Current.InstalledLocation.Path;
        }
        catch
        {
            return AppContext.BaseDirectory.TrimEnd(System.IO.Path.DirectorySeparatorChar);
        }
    }

    private void QuitApp()
    {
        _presentationTimer?.Stop();
        _focusTimer?.Stop();
        if (_highContrastHandler != null)
            SystemParameters.StaticPropertyChanged -= _highContrastHandler;
        _spokenNotificationService?.Dispose();
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
        _spokenNotificationService?.Dispose();
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
