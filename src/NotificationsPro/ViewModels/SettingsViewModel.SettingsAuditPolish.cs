using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace NotificationsPro.ViewModels;

public partial class SettingsViewModel
{
    private ICollectionView? _perAppConfigEntriesView;
    public ICollectionView PerAppConfigEntriesView => _perAppConfigEntriesView ??= CollectionViewSource.GetDefaultView(PerAppConfigEntries);

    private ICollectionView? _mutedAppEntriesView;
    public ICollectionView MutedAppEntriesView => _mutedAppEntriesView ??= CollectionViewSource.GetDefaultView(MutedAppEntries);

    private string _appOverridesFilterText = string.Empty;
    public string AppOverridesFilterText
    {
        get => _appOverridesFilterText;
        set
        {
            if (!SetProperty(ref _appOverridesFilterText, value))
                return;

            RefreshPerAppConfigFilters();
        }
    }

    private bool _showOnlyModifiedAppOverrides;
    public bool ShowOnlyModifiedAppOverrides
    {
        get => _showOnlyModifiedAppOverrides;
        set
        {
            if (!SetProperty(ref _showOnlyModifiedAppOverrides, value))
                return;

            RefreshPerAppConfigFilters();
        }
    }

    private string _mutedAppsFilterText = string.Empty;
    public string MutedAppsFilterText
    {
        get => _mutedAppsFilterText;
        set
        {
            if (!SetProperty(ref _mutedAppsFilterText, value))
                return;

            RefreshMutedAppFilters();
        }
    }

    private bool _showOnlyMutedApps;
    public bool ShowOnlyMutedApps
    {
        get => _showOnlyMutedApps;
        set
        {
            if (!SetProperty(ref _showOnlyMutedApps, value))
                return;

            RefreshMutedAppFilters();
        }
    }

    public ICommand ClearPerAppOverridesCommand { get; private set; } = null!;
    public ICommand RunCaptureDiagnosticCommand { get; private set; } = null!;

    private void InitializeSettingsAuditPolishCommands()
    {
        ClearPerAppOverridesCommand = new RelayCommand(ClearPerAppOverrides);
        RunCaptureDiagnosticCommand = new RelayCommand(_ => RunCaptureDiagnostic());
    }

    private void InitializeSettingsAuditPolishViews()
    {
        _perAppConfigEntriesView = CollectionViewSource.GetDefaultView(PerAppConfigEntries);
        _perAppConfigEntriesView.Filter = FilterPerAppConfigEntry;

        _mutedAppEntriesView = CollectionViewSource.GetDefaultView(MutedAppEntries);
        _mutedAppEntriesView.Filter = FilterMutedAppEntry;

        RefreshPerAppConfigFilters();
        RefreshMutedAppFilters();
    }

    private bool FilterPerAppConfigEntry(object item)
    {
        if (item is not PerAppConfigEntry entry)
            return false;

        if (ShowOnlyModifiedAppOverrides && !entry.HasOverrides)
            return false;

        if (string.IsNullOrWhiteSpace(AppOverridesFilterText))
            return true;

        return entry.AppName.Contains(AppOverridesFilterText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterMutedAppEntry(object item)
    {
        if (item is not MutedAppEntry entry)
            return false;

        if (ShowOnlyMutedApps && !entry.IsMuted)
            return false;

        if (string.IsNullOrWhiteSpace(MutedAppsFilterText))
            return true;

        return entry.AppName.Contains(MutedAppsFilterText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshPerAppConfigFilters()
    {
        _perAppConfigEntriesView?.Refresh();
    }

    private void RefreshMutedAppFilters()
    {
        _mutedAppEntriesView?.Refresh();
    }

    private void ClearPerAppOverrides(object? parameter)
    {
        if (parameter is not PerAppConfigEntry entry)
            return;

        var updated = _settingsManager.Settings.Clone();
        updated.PerAppSounds.Remove(entry.AppName);
        updated.PerAppIcons.Remove(entry.AppName);
        updated.PerAppBackgroundImages.Remove(entry.AppName);
        updated.SpokenMutedApps.RemoveAll(existing => string.Equals(existing, entry.AppName, StringComparison.OrdinalIgnoreCase));
        _settingsManager.Apply(updated);

        entry.ApplyDefaults();
        RefreshPerAppConfig();
    }

    private void RunCaptureDiagnostic()
    {
        var settings = _settingsManager.Settings;
        var lines = new[]
        {
            $"Status: {NotificationAccessStatusSummary}",
            $"Detail: {NotificationAccessStatusDetail}",
            $"Configured capture mode: {settings.NotificationCaptureMode}",
            $"Live listener mode: {_listenerModeForDiagnostic ?? "Unknown"}",
            $"Listener status: {_listenerStatusForDiagnostic ?? "Unknown"}",
            $"Seen apps this session: {_queueManager.SeenAppNames.Count}",
            $"Visible notifications: {_queueManager.VisibleNotifications.Count}",
            $"Overflow count: {_queueManager.OverflowCount}"
        };

        System.Windows.Clipboard.SetText(string.Join(Environment.NewLine, lines));

        System.Windows.MessageBox.Show(
            string.Join(Environment.NewLine + Environment.NewLine, lines),
            "Capture Diagnostic",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private string? _listenerModeForDiagnostic;
    private string? _listenerStatusForDiagnostic;

    public void UpdateCaptureDiagnosticSnapshot(string? listenerMode, string? statusMessage)
    {
        _listenerModeForDiagnostic = string.IsNullOrWhiteSpace(listenerMode) ? "Unknown" : listenerMode.Trim();
        _listenerStatusForDiagnostic = string.IsNullOrWhiteSpace(statusMessage) ? "Unknown" : statusMessage.Trim();
    }
}
