using System.Collections.ObjectModel;
using System.Linq;
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

    private OverlayLaneEntry? _selectedOverlayLaneEntry;
    public OverlayLaneEntry? SelectedOverlayLaneEntry
    {
        get => _selectedOverlayLaneEntry;
        set
        {
            if (SetProperty(ref _selectedOverlayLaneEntry, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public ObservableCollection<NarrationRuleEntry> NarrationRuleEntries { get; } = new();
    public ObservableCollection<AppProfileEntry> AppProfileEntries { get; } = new();
    public ObservableCollection<OverlayLaneEntry> OverlayLaneEntries { get; } = new();
    public ObservableCollection<OverlayLaneChoice> AvailableOverlayLaneChoices { get; } = new();

    public List<string> AvailableNotificationMatchScopes { get; } = NotificationMatchScopeHelper.KnownScopes.ToList();
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
    public ICommand AddOverlayLaneCommand { get; private set; } = null!;
    public ICommand DuplicateOverlayLaneCommand { get; private set; } = null!;
    public ICommand RemoveOverlayLaneCommand { get; private set; } = null!;
    public ICommand BrowseOverlayLaneBackgroundImageCommand { get; private set; } = null!;
    public ICommand ClearOverlayLaneBackgroundImageCommand { get; private set; } = null!;

    private void InitializeAdvancedCustomization()
    {
        AddNarrationRuleCommand = new RelayCommand(_ => AddNarrationRule());
        RemoveNarrationRuleCommand = new RelayCommand(RemoveNarrationRule);
        AddOverlayLaneCommand = new RelayCommand(_ => AddOverlayLane());
        DuplicateOverlayLaneCommand = new RelayCommand(_ => DuplicateSelectedOverlayLane(), _ => SelectedOverlayLaneEntry != null);
        RemoveOverlayLaneCommand = new RelayCommand(_ => RemoveSelectedOverlayLane(), _ => SelectedOverlayLaneEntry != null);
        BrowseOverlayLaneBackgroundImageCommand = new RelayCommand(BrowseOverlayLaneBackgroundImage, _ => SelectedOverlayLaneEntry != null);
        ClearOverlayLaneBackgroundImageCommand = new RelayCommand(ClearOverlayLaneBackgroundImage, _ => SelectedOverlayLaneEntry != null);
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

        RefreshOverlayLaneEntries(s.OverlayLanes);
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

        s.OverlayLanes = OverlayLaneEntries
            .Select(ToOverlayLaneDefinition)
            .OrderBy(lane => lane.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        s.AppProfiles = AppProfileEntries
            .Select(entry => new AppProfile
            {
                AppName = entry.AppName,
                IsReadAloudEnabled = entry.IsReadAloudEnabled,
                OverlayLane = NormalizeSelectedLaneId(entry.OverlayLane),
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
                OverlayLane = NormalizeSelectedLaneId(profile.OverlayLane),
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

    private void RefreshOverlayLaneEntries(IEnumerable<OverlayLaneDefinition> sourceLanes)
    {
        var currentSelectionId = SelectedOverlayLaneEntry?.Id;
        OverlayLaneEntries.Clear();

        foreach (var lane in sourceLanes
                     .Where(lane => !string.Equals(OverlayLaneHelper.Normalize(lane.Id), OverlayLaneHelper.Main, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(lane => lane.Name, StringComparer.OrdinalIgnoreCase))
        {
            var entry = new OverlayLaneEntry(lane.Id, lane.Name)
            {
                IsEnabled = lane.IsEnabled,
                MonitorIndex = lane.MonitorIndex,
                PositionPreset = SecondaryOverlayPositionHelper.Normalize(lane.PositionPreset),
                Width = lane.Width,
                MaxHeight = lane.MaxHeight,
                AccentColor = lane.AccentColor,
                BackgroundColor = lane.BackgroundColor,
                TitleColor = lane.TitleColor,
                TextColor = lane.TextColor,
                AppNameColor = lane.AppNameColor,
                BackgroundImagePath = lane.BackgroundImagePath,
                BackgroundImageOpacity = lane.BackgroundImageOpacity,
                BackgroundImageHueDegrees = lane.BackgroundImageHueDegrees,
                BackgroundImageBrightness = lane.BackgroundImageBrightness
            };

            AttachOverlayLaneEntry(entry);
            OverlayLaneEntries.Add(entry);
        }

        RebuildAvailableOverlayLaneChoices();
        SelectedOverlayLaneEntry = OverlayLaneEntries.FirstOrDefault(entry =>
                                       string.Equals(entry.Id, currentSelectionId, StringComparison.OrdinalIgnoreCase))
                                   ?? OverlayLaneEntries.FirstOrDefault();
    }

    private void AttachOverlayLaneEntry(OverlayLaneEntry entry)
    {
        entry.PropertyChanged += (_, args) =>
        {
            if (string.Equals(args.PropertyName, nameof(OverlayLaneEntry.Name), StringComparison.Ordinal))
                RebuildAvailableOverlayLaneChoices();

            QueueSave();
        };
    }

    private void RebuildAvailableOverlayLaneChoices()
    {
        var currentSelectionId = SelectedOverlayLaneEntry?.Id;
        AvailableOverlayLaneChoices.Clear();
        AvailableOverlayLaneChoices.Add(new OverlayLaneChoice(OverlayLaneHelper.Main, OverlayLaneHelper.MainDisplayName));

        foreach (var lane in OverlayLaneEntries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
            AvailableOverlayLaneChoices.Add(new OverlayLaneChoice(lane.Id, lane.Name));

        foreach (var profile in AppProfileEntries)
            profile.OverlayLane = NormalizeSelectedLaneId(profile.OverlayLane);

        if (!string.IsNullOrWhiteSpace(currentSelectionId))
        {
            SelectedOverlayLaneEntry = OverlayLaneEntries.FirstOrDefault(entry =>
                                           string.Equals(entry.Id, currentSelectionId, StringComparison.OrdinalIgnoreCase))
                                       ?? OverlayLaneEntries.FirstOrDefault();
        }

        OnPropertyChanged(nameof(SelectedOverlayLaneEntry));
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

    private static OverlayLaneDefinition ToOverlayLaneDefinition(OverlayLaneEntry entry)
    {
        return new OverlayLaneDefinition
        {
            Id = OverlayLaneHelper.Normalize(entry.Id),
            Name = string.IsNullOrWhiteSpace(entry.Name) ? "Overlay Lane" : entry.Name.Trim(),
            IsEnabled = entry.IsEnabled,
            MonitorIndex = Math.Max(0, entry.MonitorIndex),
            PositionPreset = SecondaryOverlayPositionHelper.Normalize(entry.PositionPreset),
            Width = Math.Clamp(entry.Width, OverlayWidthMin, OverlayWidthMax),
            MaxHeight = Math.Clamp(entry.MaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax),
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

    private string NormalizeSelectedLaneId(string? laneId)
    {
        var lanes = OverlayLaneEntries.Select(ToOverlayLaneDefinition).ToList();
        return OverlayLaneHelper.NormalizeOrMain(laneId, lanes);
    }

    private void AddOverlayLane()
    {
        var laneName = BuildUniqueLaneName("New Lane");
        var laneId = BuildUniqueLaneId(laneName);
        var defaultPreset = AvailableSecondaryOverlayPositionPresets[
            OverlayLaneEntries.Count % AvailableSecondaryOverlayPositionPresets.Count];
        var entry = new OverlayLaneEntry(laneId, laneName)
        {
            IsEnabled = true,
            MonitorIndex = SelectedMonitorIndex,
            PositionPreset = defaultPreset,
            Width = Math.Clamp(OverlayWidth, OverlayWidthMin, OverlayWidthMax),
            MaxHeight = Math.Clamp(OverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax),
            AccentColor = AccentColor,
            BackgroundColor = BackgroundColor,
            TitleColor = TitleColor,
            TextColor = TextColor,
            AppNameColor = AppNameColor
        };

        AttachOverlayLaneEntry(entry);
        OverlayLaneEntries.Add(entry);
        RebuildAvailableOverlayLaneChoices();
        SelectedOverlayLaneEntry = entry;
        QueueSave();
    }

    private void DuplicateSelectedOverlayLane()
    {
        if (SelectedOverlayLaneEntry == null)
            return;

        var duplicateName = BuildUniqueLaneName($"{SelectedOverlayLaneEntry.Name} Copy");
        var duplicate = new OverlayLaneEntry(BuildUniqueLaneId(duplicateName), duplicateName)
        {
            IsEnabled = SelectedOverlayLaneEntry.IsEnabled,
            MonitorIndex = SelectedOverlayLaneEntry.MonitorIndex,
            PositionPreset = SelectedOverlayLaneEntry.PositionPreset,
            Width = SelectedOverlayLaneEntry.Width,
            MaxHeight = SelectedOverlayLaneEntry.MaxHeight,
            AccentColor = SelectedOverlayLaneEntry.AccentColor,
            BackgroundColor = SelectedOverlayLaneEntry.BackgroundColor,
            TitleColor = SelectedOverlayLaneEntry.TitleColor,
            TextColor = SelectedOverlayLaneEntry.TextColor,
            AppNameColor = SelectedOverlayLaneEntry.AppNameColor,
            BackgroundImagePath = SelectedOverlayLaneEntry.BackgroundImagePath,
            BackgroundImageOpacity = SelectedOverlayLaneEntry.BackgroundImageOpacity,
            BackgroundImageHueDegrees = SelectedOverlayLaneEntry.BackgroundImageHueDegrees,
            BackgroundImageBrightness = SelectedOverlayLaneEntry.BackgroundImageBrightness
        };

        AttachOverlayLaneEntry(duplicate);
        OverlayLaneEntries.Add(duplicate);
        RebuildAvailableOverlayLaneChoices();
        SelectedOverlayLaneEntry = duplicate;
        QueueSave();
    }

    private void RemoveSelectedOverlayLane()
    {
        if (SelectedOverlayLaneEntry == null)
            return;

        var removedLaneId = SelectedOverlayLaneEntry.Id;
        OverlayLaneEntries.Remove(SelectedOverlayLaneEntry);

        foreach (var profile in AppProfileEntries.Where(profile =>
                     string.Equals(
                         OverlayLaneHelper.Normalize(profile.OverlayLane),
                         OverlayLaneHelper.Normalize(removedLaneId),
                         StringComparison.OrdinalIgnoreCase)))
        {
            profile.OverlayLane = OverlayLaneHelper.Main;
        }

        RebuildAvailableOverlayLaneChoices();
        SelectedOverlayLaneEntry = OverlayLaneEntries.FirstOrDefault();
        QueueSave();
    }

    private string BuildUniqueLaneName(string baseName)
    {
        var names = OverlayLaneEntries.Select(entry => entry.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(baseName))
            return baseName;

        for (var index = 2; index < 1000; index++)
        {
            var candidate = $"{baseName} {index}";
            if (!names.Contains(candidate))
                return candidate;
        }

        return $"{baseName} {Guid.NewGuid():N}";
    }

    private string BuildUniqueLaneId(string baseName)
    {
        var slug = OverlayLaneHelper.Slugify(baseName);
        var ids = OverlayLaneEntries.Select(entry => entry.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!ids.Contains(slug))
            return slug;

        for (var index = 2; index < 1000; index++)
        {
            var candidate = $"{slug}{index}";
            if (!ids.Contains(candidate))
                return candidate;
        }

        return $"{slug}{Guid.NewGuid():N}";
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

    private void BrowseOverlayLaneBackgroundImage(object? parameter)
    {
        var entry = parameter as OverlayLaneEntry ?? SelectedOverlayLaneEntry;
        if (entry == null)
            return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            Title = $"Choose a background image for {entry.Name}"
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

    private void ClearOverlayLaneBackgroundImage(object? parameter)
    {
        var entry = parameter as OverlayLaneEntry ?? SelectedOverlayLaneEntry;
        if (entry == null)
            return;

        entry.BackgroundImagePath = string.Empty;
        entry.BackgroundImageHueDegrees = 0;
        entry.BackgroundImageBrightness = 1.0;
        entry.BackgroundImageOpacity = 0.45;
    }
}
