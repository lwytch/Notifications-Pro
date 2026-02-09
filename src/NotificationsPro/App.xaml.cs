using System.Windows;
using Application = System.Windows.Application;
using NotificationsPro.Helpers;
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

    private WinForms.ToolStripMenuItem? _showHideItem;
    private WinForms.ToolStripMenuItem? _pauseResumeItem;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsManager = new SettingsManager();
        _settingsManager.Load();

        _queueManager = new QueueManager(_settingsManager);

        _overlayViewModel = new OverlayViewModel(_queueManager, _settingsManager);
        _settingsViewModel = new SettingsViewModel(_settingsManager, _queueManager);

        SetupTrayIcon();

        if (_settingsManager.Settings.OverlayVisible)
            ShowOverlay();
    }

    private void SetupTrayIcon()
    {
        var icon = IconHelper.CreateTrayIcon();

        _showHideItem = new WinForms.ToolStripMenuItem("Hide Overlay", null, (_, _) => ToggleOverlay());
        _pauseResumeItem = new WinForms.ToolStripMenuItem("Pause Notifications", null, (_, _) => TogglePause());

        var contextMenu = new WinForms.ContextMenuStrip();
        contextMenu.BackColor = Drawing.Color.FromArgb(30, 30, 46);
        contextMenu.ForeColor = Drawing.Color.FromArgb(228, 228, 239);
        contextMenu.Renderer = new DarkMenuRenderer();

        contextMenu.Items.Add(_showHideItem);
        contextMenu.Items.Add(_pauseResumeItem);
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

    private void UpdateMenuLabels()
    {
        if (_showHideItem != null)
            _showHideItem.Text = _overlayWindow?.IsVisible == true ? "Hide Overlay" : "Show Overlay";

        if (_pauseResumeItem != null)
            _pauseResumeItem.Text = _queueManager?.IsPaused == true ? "Resume Notifications" : "Pause Notifications";
    }

    private void ShowSettings()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsViewModel = new SettingsViewModel(_settingsManager!, _queueManager!);
            _settingsWindow = new SettingsWindow(_settingsViewModel);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void QuitApp()
    {
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
