using System.Collections.ObjectModel;
using System.Windows.Input;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.ViewModels;

public partial class SettingsViewModel
{
    private bool _showQuickTips = true;
    public bool ShowQuickTips
    {
        get => _showQuickTips;
        set
        {
            if (!SetProperty(ref _showQuickTips, value))
                return;

            if (!value)
                ShowFirstRunTip = false;

            QueueSave();
        }
    }

    private bool _secondaryOverlayEnabled;
    public bool SecondaryOverlayEnabled
    {
        get => _secondaryOverlayEnabled;
        set { if (SetProperty(ref _secondaryOverlayEnabled, value)) QueueSave(); }
    }

    private int _secondaryOverlayMonitorIndex;
    public int SecondaryOverlayMonitorIndex
    {
        get => _secondaryOverlayMonitorIndex;
        set { if (SetProperty(ref _secondaryOverlayMonitorIndex, Math.Max(0, value))) QueueSave(); }
    }

    private string _secondaryOverlayPositionPreset = "Top Left";
    public string SecondaryOverlayPositionPreset
    {
        get => _secondaryOverlayPositionPreset;
        set
        {
            var normalized = SecondaryOverlayPositionHelper.Normalize(value);
            if (SetProperty(ref _secondaryOverlayPositionPreset, normalized))
                QueueSave();
        }
    }

    private double _secondaryOverlayWidth = 340;
    public double SecondaryOverlayWidth
    {
        get => _secondaryOverlayWidth;
        set
        {
            var clamped = Math.Clamp(value, OverlayWidthMin, OverlayWidthMax);
            if (SetProperty(ref _secondaryOverlayWidth, clamped))
                QueueSave();
        }
    }

    private double _secondaryOverlayMaxHeight = 480;
    public double SecondaryOverlayMaxHeight
    {
        get => _secondaryOverlayMaxHeight;
        set
        {
            var clamped = Math.Clamp(value, OverlayMaxHeightMin, OverlayMaxHeightMax);
            if (SetProperty(ref _secondaryOverlayMaxHeight, clamped))
                QueueSave();
        }
    }

    private string _newNarrationKeyword = string.Empty;
    public string NewNarrationKeyword
    {
        get => _newNarrationKeyword;
        set => SetProperty(ref _newNarrationKeyword, value);
    }

    public ObservableCollection<NarrationRuleEntry> NarrationRuleEntries { get; } = new();
    public ObservableCollection<AppProfileEntry> AppProfileEntries { get; } = new();

    public List<string> AvailableNotificationMatchScopes { get; } = NotificationMatchScopeHelper.KnownScopes.ToList();
    public List<string> AvailableOverlayLanes { get; } = OverlayLaneHelper.KnownLanes.ToList();
    public List<string> AvailableNarrationRuleActions { get; } = NarrationRuleActionHelper.KnownActions.ToList();
    public List<string> AvailableNarrationRuleReadModes { get; } = new()
    {
        NarrationRuleReadModeHelper.UseGlobal,
        SpokenNotificationTextFormatter.ModeBodyOnly,
        SpokenNotificationTextFormatter.ModeTitleOnly,
        SpokenNotificationTextFormatter.ModeTitleBody,
        SpokenNotificationTextFormatter.ModeBodyTimestamp,
        SpokenNotificationTextFormatter.ModeTitleTimestamp,
        SpokenNotificationTextFormatter.ModeTitleBodyTimestamp
    };

    public List<string> AvailableSecondaryOverlayPositionPresets { get; } = new()
    {
        "Top Left",
        "Top Center",
        "Top Right",
        "Middle Left",
        "Middle Center",
        "Middle Right",
        "Bottom Left",
        "Bottom Center",
        "Bottom Right"
    };

    public ICommand AddNarrationRuleCommand { get; private set; } = null!;
    public ICommand RemoveNarrationRuleCommand { get; private set; } = null!;
    public ICommand BrowseAppBackgroundImageCommand { get; private set; } = null!;
    public ICommand ClearAppBackgroundImageCommand { get; private set; } = null!;

    private void InitializeAdvancedCustomization()
    {
        AddNarrationRuleCommand = new RelayCommand(_ => AddNarrationRule());
        RemoveNarrationRuleCommand = new RelayCommand(RemoveNarrationRule);
        BrowseAppBackgroundImageCommand = new RelayCommand(BrowseAppBackgroundImage);
        ClearAppBackgroundImageCommand = new RelayCommand(ClearAppBackgroundImage);
    }

    private void LoadAdvancedCustomizationFromSettings(AppSettings s)
    {
        _showQuickTips = s.ShowQuickTips;
        _secondaryOverlayEnabled = s.SecondaryOverlayEnabled;
        _secondaryOverlayMonitorIndex = s.SecondaryOverlayMonitorIndex;
        _secondaryOverlayPositionPreset = SecondaryOverlayPositionHelper.Normalize(s.SecondaryOverlayPositionPreset);
        _secondaryOverlayWidth = Math.Clamp(s.SecondaryOverlayWidth, OverlayWidthMin, OverlayWidthMax);
        _secondaryOverlayMaxHeight = Math.Clamp(s.SecondaryOverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);

        NarrationRuleEntries.Clear();
        foreach (var rule in s.NarrationRules)
        {
            var entry = new NarrationRuleEntry(
                rule.Keyword,
                rule.IsRegex,
                rule.Scope,
                rule.AppFilter,
                rule.Action,
                rule.ReadMode);
            entry.PropertyChanged += (_, _) => QueueSave();
            NarrationRuleEntries.Add(entry);
        }

        RefreshAppProfileEntries(s.AppProfiles);
    }

    private void PopulateAdvancedCustomizationSettings(AppSettings s)
    {
        s.ShowQuickTips = ShowQuickTips;
        s.SecondaryOverlayEnabled = SecondaryOverlayEnabled;
        s.SecondaryOverlayMonitorIndex = SecondaryOverlayMonitorIndex;
        s.SecondaryOverlayPositionPreset = SecondaryOverlayPositionHelper.Normalize(SecondaryOverlayPositionPreset);
        s.SecondaryOverlayWidth = Math.Clamp(SecondaryOverlayWidth, OverlayWidthMin, OverlayWidthMax);
        s.SecondaryOverlayMaxHeight = Math.Clamp(SecondaryOverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
        s.NarrationRules = NarrationRuleEntries
            .Select(entry => new NarrationRuleDefinition
            {
                Keyword = entry.Keyword,
                IsRegex = entry.IsRegex,
                Scope = NotificationMatchScopeHelper.Normalize(entry.Scope),
                AppFilter = entry.AppFilter,
                Action = NarrationRuleActionHelper.Normalize(entry.Action),
                ReadMode = NarrationRuleReadModeHelper.Normalize(entry.ReadMode)
            })
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Keyword))
            .ToList();

        s.AppProfiles = AppProfileEntries
            .Select(entry => new AppProfile
            {
                AppName = entry.AppName,
                IsReadAloudEnabled = entry.IsReadAloudEnabled,
                OverlayLane = OverlayLaneHelper.Normalize(entry.OverlayLane),
                Sound = string.IsNullOrWhiteSpace(entry.Sound) ? "Default" : entry.Sound,
                Icon = string.IsNullOrWhiteSpace(entry.Icon) ? "Default" : entry.Icon,
                AccentColor = entry.AccentColor,
                BackgroundColor = entry.BackgroundColor,
                TitleColor = entry.TitleColor,
                TextColor = entry.TextColor,
                AppNameColor = entry.AppNameColor,
                BackgroundImagePath = entry.BackgroundImagePath,
                BackgroundImageOpacity = Math.Clamp(entry.BackgroundImageOpacity, 0.0, 1.0),
                BackgroundImageHueDegrees = Math.Clamp(entry.BackgroundImageHueDegrees, -180.0, 180.0),
                BackgroundImageBrightness = Math.Clamp(entry.BackgroundImageBrightness, 0.2, 2.0)
            })
            .Where(profile => !string.IsNullOrWhiteSpace(profile.AppName))
            .OrderBy(profile => profile.AppName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        s.PerAppSounds = s.AppProfiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Sound) && !string.Equals(profile.Sound, "Default", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(profile => profile.AppName, profile => profile.Sound, StringComparer.OrdinalIgnoreCase);

        s.PerAppIcons = s.AppProfiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Icon) && !string.Equals(profile.Icon, "Default", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(profile => profile.AppName, profile => profile.Icon, StringComparer.OrdinalIgnoreCase);

        s.SpokenMutedApps = s.AppProfiles
            .Where(profile => !profile.IsReadAloudEnabled)
            .Select(profile => profile.AppName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(app => app, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void RefreshAppProfileEntries()
    {
        var currentProfiles = AppProfileEntries.Select(ToProfile).ToList();
        RefreshAppProfileEntries(currentProfiles.Count > 0 ? currentProfiles : _settingsManager.Settings.AppProfiles);
    }

    private void RefreshAppProfileEntries(IEnumerable<AppProfile> sourceProfiles)
    {
        var profileMap = sourceProfiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.AppName))
            .GroupBy(profile => profile.AppName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Clone(), StringComparer.OrdinalIgnoreCase);

        foreach (var appName in _queueManager.SeenAppNames)
        {
            if (!profileMap.ContainsKey(appName))
                profileMap[appName] = new AppProfile { AppName = appName };
        }

        var orderedProfiles = profileMap.Values
            .OrderBy(profile => profile.AppName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AppProfileEntries.Clear();
        foreach (var profile in orderedProfiles)
        {
            var entry = new AppProfileEntry(profile.AppName)
            {
                IsReadAloudEnabled = profile.IsReadAloudEnabled,
                OverlayLane = OverlayLaneHelper.Normalize(profile.OverlayLane),
                Sound = string.IsNullOrWhiteSpace(profile.Sound) ? "Default" : profile.Sound,
                Icon = string.IsNullOrWhiteSpace(profile.Icon) ? "Default" : profile.Icon,
                AccentColor = profile.AccentColor,
                BackgroundColor = profile.BackgroundColor,
                TitleColor = profile.TitleColor,
                TextColor = profile.TextColor,
                AppNameColor = profile.AppNameColor,
                BackgroundImagePath = profile.BackgroundImagePath,
                BackgroundImageOpacity = Math.Clamp(profile.BackgroundImageOpacity, 0.0, 1.0),
                BackgroundImageHueDegrees = Math.Clamp(profile.BackgroundImageHueDegrees, -180.0, 180.0),
                BackgroundImageBrightness = Math.Clamp(profile.BackgroundImageBrightness, 0.2, 2.0)
            };
            entry.PropertyChanged += (_, _) => QueueSave();
            AppProfileEntries.Add(entry);
        }
    }

    private static AppProfile ToProfile(AppProfileEntry entry)
    {
        return new AppProfile
        {
            AppName = entry.AppName,
            IsReadAloudEnabled = entry.IsReadAloudEnabled,
            OverlayLane = OverlayLaneHelper.Normalize(entry.OverlayLane),
            Sound = string.IsNullOrWhiteSpace(entry.Sound) ? "Default" : entry.Sound,
            Icon = string.IsNullOrWhiteSpace(entry.Icon) ? "Default" : entry.Icon,
            AccentColor = entry.AccentColor,
            BackgroundColor = entry.BackgroundColor,
            TitleColor = entry.TitleColor,
            TextColor = entry.TextColor,
            AppNameColor = entry.AppNameColor,
            BackgroundImagePath = entry.BackgroundImagePath,
            BackgroundImageOpacity = Math.Clamp(entry.BackgroundImageOpacity, 0.0, 1.0),
            BackgroundImageHueDegrees = Math.Clamp(entry.BackgroundImageHueDegrees, -180.0, 180.0),
            BackgroundImageBrightness = Math.Clamp(entry.BackgroundImageBrightness, 0.2, 2.0)
        };
    }

    private void AddNarrationRule()
    {
        var keyword = NewNarrationKeyword?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
            return;

        if (NarrationRuleEntries.Any(entry => string.Equals(entry.Keyword, keyword, StringComparison.OrdinalIgnoreCase)))
            return;

        var entry = new NarrationRuleEntry(keyword);
        entry.PropertyChanged += (_, _) => QueueSave();
        NarrationRuleEntries.Add(entry);
        NewNarrationKeyword = string.Empty;
        QueueSave();
    }

    private void RemoveNarrationRule(object? parameter)
    {
        NarrationRuleEntry? entry = parameter switch
        {
            NarrationRuleEntry ruleEntry => ruleEntry,
            string keyword => NarrationRuleEntries.FirstOrDefault(item => string.Equals(item.Keyword, keyword, StringComparison.OrdinalIgnoreCase)),
            _ => null
        };

        if (entry == null)
            return;

        NarrationRuleEntries.Remove(entry);
        QueueSave();
    }

    private void BrowseAppBackgroundImage(object? parameter)
    {
        if (parameter is not AppProfileEntry entry)
            return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            Title = $"Choose a background image for {entry.AppName}"
        };

        if (dialog.ShowDialog() != true)
            return;

        BackgroundImageService.EnsureBackgroundsDirExists();
        var destinationDirectory = BackgroundImageService.GetCustomBackgroundsDir();
        var fileName = System.IO.Path.GetFileName(dialog.FileName);
        var destinationPath = System.IO.Path.Combine(destinationDirectory, fileName);

        try
        {
            System.IO.File.Copy(dialog.FileName, destinationPath, overwrite: true);
            entry.BackgroundImagePath = destinationPath;
        }
        catch
        {
            return;
        }
    }

    private void ClearAppBackgroundImage(object? parameter)
    {
        if (parameter is not AppProfileEntry entry)
            return;

        entry.BackgroundImagePath = string.Empty;
        entry.BackgroundImageHueDegrees = 0;
        entry.BackgroundImageBrightness = 1.0;
        entry.BackgroundImageOpacity = 0.45;
    }
}
