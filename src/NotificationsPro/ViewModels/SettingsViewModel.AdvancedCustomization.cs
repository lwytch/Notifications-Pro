using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.ViewModels;

public partial class SettingsViewModel
{
    private readonly OverlayLaneEntry _mainOverlayLaneEntry = new(OverlayLaneHelper.Main, OverlayLaneHelper.MainDisplayName);

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

    private string _selectedOverlayLaneId = OverlayLaneHelper.Main;
    public string SelectedOverlayLaneId
    {
        get => _selectedOverlayLaneId;
        set
        {
            var normalized = NormalizeSelectedLaneId(value);
            if (!SetProperty(ref _selectedOverlayLaneId, normalized))
                return;

            LoadSelectedLaneEditor();
            NotifySelectedOverlayLaneChanged();
        }
    }

    public OverlayLaneEntry? SelectedOverlayLaneEntry => GetSelectedOverlayLaneEntry();

    public bool IsMainOverlayLaneSelected =>
        string.Equals(_selectedOverlayLaneId, OverlayLaneHelper.Main, StringComparison.OrdinalIgnoreCase);

    public bool CanRemoveSelectedOverlayLane =>
        !IsMainOverlayLaneSelected && SelectedOverlayLaneEntry != null;

    public string SelectedOverlayLaneName
    {
        get => GetSelectedOverlayLaneEntry()?.Name ?? OverlayLaneHelper.MainDisplayName;
        set
        {
            if (IsMainOverlayLaneSelected)
                return;

            var entry = GetSelectedOverlayLaneEntry();
            if (entry == null)
                return;

            var normalized = string.IsNullOrWhiteSpace(value)
                ? BuildUniqueLaneName(GetNextLaneBaseName())
                : value.Trim();

            if (string.Equals(entry.Name, normalized, StringComparison.Ordinal))
                return;

            entry.Name = normalized;
            RebuildAvailableOverlayLaneChoices();
            OnPropertyChanged(nameof(SelectedOverlayLaneName));
            QueueSave();
        }
    }

    private string _overlayBackgroundImagePath = string.Empty;
    public string OverlayBackgroundImagePath
    {
        get => _overlayBackgroundImagePath;
        set
        {
            if (!SetProperty(ref _overlayBackgroundImagePath, value?.Trim() ?? string.Empty))
                return;

            SyncSelectedOverlayLaneFromEditor();
            QueueSave();
        }
    }

    private double _overlayBackgroundImageOpacity = 0.45;
    public double OverlayBackgroundImageOpacity
    {
        get => _overlayBackgroundImageOpacity;
        set
        {
            if (!SetProperty(ref _overlayBackgroundImageOpacity, Math.Clamp(value, 0.0, 1.0)))
                return;

            SyncSelectedOverlayLaneFromEditor();
            QueueSave();
        }
    }

    private double _overlayBackgroundImageHueDegrees;
    public double OverlayBackgroundImageHueDegrees
    {
        get => _overlayBackgroundImageHueDegrees;
        set
        {
            if (!SetProperty(ref _overlayBackgroundImageHueDegrees, Math.Clamp(value, -180.0, 180.0)))
                return;

            SyncSelectedOverlayLaneFromEditor();
            QueueSave();
        }
    }

    private double _overlayBackgroundImageBrightness = 1.0;
    public double OverlayBackgroundImageBrightness
    {
        get => _overlayBackgroundImageBrightness;
        set
        {
            if (!SetProperty(ref _overlayBackgroundImageBrightness, Math.Clamp(value, 0.2, 2.0)))
                return;

            SyncSelectedOverlayLaneFromEditor();
            QueueSave();
        }
    }

    public ObservableCollection<NarrationRuleEntry> NarrationRuleEntries { get; } = new();
    public ObservableCollection<AppProfileEntry> AppProfileEntries { get; } = new();
    public ObservableCollection<OverlayLaneEntry> OverlayLaneEntries { get; } = new();
    public ObservableCollection<OverlayLaneChoice> AvailableOverlayLaneChoices { get; } = new();

    public List<string> AvailableNotificationMatchScopes { get; } = NotificationMatchScopeHelper.KnownScopes.ToList();
    public List<string> AvailableNarrationRuleActions { get; } = NarrationRuleActionHelper.KnownActions.ToList();
    public List<string> AvailableNarrationRuleReadModes { get; } =
    [
        NarrationRuleReadModeHelper.UseGlobal,
        SpokenNotificationTextFormatter.ModeBodyOnly,
        SpokenNotificationTextFormatter.ModeTitleOnly,
        SpokenNotificationTextFormatter.ModeTitleBody,
        SpokenNotificationTextFormatter.ModeBodyTimestamp,
        SpokenNotificationTextFormatter.ModeTitleTimestamp,
        SpokenNotificationTextFormatter.ModeTitleBodyTimestamp
    ];

    public List<string> AvailableSecondaryOverlayPositionPresets { get; } =
    [
        "Top Left",
        "Top Center",
        "Top Right",
        "Middle Left",
        "Middle Center",
        "Middle Right",
        "Bottom Left",
        "Bottom Center",
        "Bottom Right"
    ];

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
        RemoveOverlayLaneCommand = new RelayCommand(_ => RemoveSelectedOverlayLane(), _ => CanRemoveSelectedOverlayLane);
        BrowseOverlayLaneBackgroundImageCommand = new RelayCommand(_ => BrowseOverlayLaneBackgroundImage(), _ => SelectedOverlayLaneEntry != null);
        ClearOverlayLaneBackgroundImageCommand = new RelayCommand(_ => ClearOverlayLaneBackgroundImage(), _ => SelectedOverlayLaneEntry != null);
    }

    private void LoadAdvancedCustomizationFromSettings(AppSettings settings)
    {
        var desiredLaneId = NormalizeSelectedLaneId(_selectedOverlayLaneId, settings.OverlayLanes);

        _showQuickTips = settings.ShowQuickTips;
        _secondaryOverlayEnabled = settings.SecondaryOverlayEnabled;
        _secondaryOverlayMonitorIndex = settings.SecondaryOverlayMonitorIndex;
        _secondaryOverlayPositionPreset = SecondaryOverlayPositionHelper.Normalize(settings.SecondaryOverlayPositionPreset);
        _secondaryOverlayWidth = Math.Clamp(settings.SecondaryOverlayWidth, OverlayWidthMin, OverlayWidthMax);
        _secondaryOverlayMaxHeight = Math.Clamp(settings.SecondaryOverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);

        NarrationRuleEntries.Clear();
        foreach (var rule in settings.NarrationRules)
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

        PopulateMainOverlayLaneEntry(settings);
        RefreshOverlayLaneEntries(settings.OverlayLanes);
        _selectedOverlayLaneId = desiredLaneId;
        LoadSelectedLaneEditor();
        RebuildAvailableOverlayLaneChoices();
        RefreshAppProfileEntries(settings.AppProfiles);
        NotifySelectedOverlayLaneChanged();
    }

    private void PopulateAdvancedCustomizationSettings(AppSettings settings)
    {
        SyncSelectedOverlayLaneFromEditor();
        ApplyMainOverlayLaneEntryToSettings(settings);

        settings.ShowQuickTips = ShowQuickTips;
        settings.NarrationRules = NarrationRuleEntries
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

        settings.OverlayLanes = OverlayLaneEntries
            .Select(ToOverlayLaneDefinition)
            .OrderBy(lane => lane.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var secondaryLane = settings.OverlayLanes.FirstOrDefault(lane =>
            string.Equals(OverlayLaneHelper.Normalize(lane.Id), OverlayLaneHelper.Secondary, StringComparison.OrdinalIgnoreCase));
        if (secondaryLane != null)
        {
            settings.SecondaryOverlayEnabled = secondaryLane.IsEnabled;
            settings.SecondaryOverlayMonitorIndex = secondaryLane.MonitorIndex;
            settings.SecondaryOverlayPositionPreset = SecondaryOverlayPositionHelper.Normalize(secondaryLane.PositionPreset);
            settings.SecondaryOverlayLeft = secondaryLane.Left;
            settings.SecondaryOverlayTop = secondaryLane.Top;
            settings.SecondaryOverlayWidth = Math.Clamp(secondaryLane.Width, OverlayWidthMin, OverlayWidthMax);
            settings.SecondaryOverlayMaxHeight = Math.Clamp(secondaryLane.MaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
        }
        else
        {
            settings.SecondaryOverlayEnabled = false;
            settings.SecondaryOverlayMonitorIndex = 0;
            settings.SecondaryOverlayPositionPreset = "Top Left";
            settings.SecondaryOverlayLeft = null;
            settings.SecondaryOverlayTop = null;
            settings.SecondaryOverlayWidth = 340;
            settings.SecondaryOverlayMaxHeight = 480;
        }

        settings.AppProfiles = AppProfileEntries
            .Select(entry => new AppProfile
            {
                AppName = entry.AppName,
                IsReadAloudEnabled = entry.IsReadAloudEnabled,
                OverlayLane = NormalizeSelectedLaneId(entry.OverlayLane),
                Sound = string.IsNullOrWhiteSpace(entry.Sound) ? "Default" : entry.Sound,
                Icon = string.IsNullOrWhiteSpace(entry.Icon) ? "Default" : entry.Icon
            })
            .Where(profile => !string.IsNullOrWhiteSpace(profile.AppName))
            .OrderBy(profile => profile.AppName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        settings.PerAppSounds = settings.AppProfiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Sound) && !string.Equals(profile.Sound, "Default", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(profile => profile.AppName, profile => profile.Sound, StringComparer.OrdinalIgnoreCase);

        settings.PerAppIcons = settings.AppProfiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Icon) && !string.Equals(profile.Icon, "Default", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(profile => profile.AppName, profile => profile.Icon, StringComparer.OrdinalIgnoreCase);

        settings.SpokenMutedApps = settings.AppProfiles
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
                Icon = string.IsNullOrWhiteSpace(profile.Icon) ? "Default" : profile.Icon
            };
            entry.PropertyChanged += (_, _) => QueueSave();
            AppProfileEntries.Add(entry);
        }
    }

    private void PopulateMainOverlayLaneEntry(AppSettings settings)
    {
        _mainOverlayLaneEntry.Name = OverlayLaneHelper.MainDisplayName;
        _mainOverlayLaneEntry.IsEnabled = true;
        _mainOverlayLaneEntry.FontFamily = settings.FontFamily;
        _mainOverlayLaneEntry.FontSize = settings.FontSize;
        _mainOverlayLaneEntry.FontWeight = settings.FontWeight;
        _mainOverlayLaneEntry.AppNameFontSize = settings.AppNameFontSize;
        _mainOverlayLaneEntry.AppNameFontWeight = settings.AppNameFontWeight;
        _mainOverlayLaneEntry.TitleFontSize = settings.TitleFontSize;
        _mainOverlayLaneEntry.TitleFontWeight = settings.TitleFontWeight;
        _mainOverlayLaneEntry.LineSpacing = settings.LineSpacing;
        _mainOverlayLaneEntry.TextAlignment = string.IsNullOrWhiteSpace(settings.TextAlignment) ? "Left" : settings.TextAlignment;
        _mainOverlayLaneEntry.MonitorIndex = settings.SelectedMonitorIndex;
        _mainOverlayLaneEntry.PositionPreset = "Top Right";
        _mainOverlayLaneEntry.Left = settings.OverlayLeft;
        _mainOverlayLaneEntry.Top = settings.OverlayTop;
        _mainOverlayLaneEntry.Width = Math.Clamp(settings.OverlayWidth, OverlayWidthMin, OverlayWidthMax);
        _mainOverlayLaneEntry.MaxHeight = Math.Clamp(settings.OverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
        _mainOverlayLaneEntry.AccentColor = settings.AccentColor;
        _mainOverlayLaneEntry.BackgroundColor = settings.BackgroundColor;
        _mainOverlayLaneEntry.BackgroundOpacity = settings.BackgroundOpacity;
        _mainOverlayLaneEntry.TitleColor = settings.TitleColor;
        _mainOverlayLaneEntry.TextColor = settings.TextColor;
        _mainOverlayLaneEntry.AppNameColor = settings.AppNameColor;
        _mainOverlayLaneEntry.CornerRadius = settings.CornerRadius;
        _mainOverlayLaneEntry.Padding = settings.Padding;
        _mainOverlayLaneEntry.CardGap = settings.CardGap;
        _mainOverlayLaneEntry.OuterMargin = settings.OuterMargin;
        _mainOverlayLaneEntry.ShowAccent = settings.ShowAccent;
        _mainOverlayLaneEntry.AccentThickness = settings.AccentThickness;
        _mainOverlayLaneEntry.ShowBorder = settings.ShowBorder;
        _mainOverlayLaneEntry.BorderColor = settings.BorderColor;
        _mainOverlayLaneEntry.BorderThickness = settings.BorderThickness;
        _mainOverlayLaneEntry.ShowAppName = settings.ShowAppName;
        _mainOverlayLaneEntry.ShowNotificationTitle = settings.ShowNotificationTitle;
        _mainOverlayLaneEntry.ShowNotificationBody = settings.ShowNotificationBody;
        _mainOverlayLaneEntry.LimitTextLines = settings.LimitTextLines;
        _mainOverlayLaneEntry.MaxAppNameLines = settings.MaxAppNameLines;
        _mainOverlayLaneEntry.MaxTitleLines = settings.MaxTitleLines;
        _mainOverlayLaneEntry.MaxBodyLines = settings.MaxBodyLines;
        _mainOverlayLaneEntry.SingleLineMode = settings.SingleLineMode;
        _mainOverlayLaneEntry.SingleLineWrapText = settings.SingleLineWrapText;
        _mainOverlayLaneEntry.SingleLineMaxLines = settings.SingleLineMaxLines;
        _mainOverlayLaneEntry.SingleLineAutoFullWidth = settings.SingleLineAutoFullWidth;
        _mainOverlayLaneEntry.ShowTimestamp = settings.ShowTimestamp;
        _mainOverlayLaneEntry.TimestampFontSize = settings.TimestampFontSize;
        _mainOverlayLaneEntry.TimestampDisplayMode = settings.TimestampDisplayMode;
        _mainOverlayLaneEntry.TimestampFontWeight = settings.TimestampFontWeight;
        _mainOverlayLaneEntry.TimestampColor = settings.TimestampColor;
        _mainOverlayLaneEntry.DensityPreset = settings.DensityPreset;
        _mainOverlayLaneEntry.BackgroundImagePath = settings.OverlayBackgroundImagePath;
        _mainOverlayLaneEntry.BackgroundImageOpacity = Math.Clamp(settings.OverlayBackgroundImageOpacity, 0.0, 1.0);
        _mainOverlayLaneEntry.BackgroundImageHueDegrees = settings.OverlayBackgroundImageHueDegrees;
        _mainOverlayLaneEntry.BackgroundImageBrightness = Math.Clamp(settings.OverlayBackgroundImageBrightness, 0.2, 2.0);
    }

    private void ApplyMainOverlayLaneEntryToSettings(AppSettings settings)
    {
        NormalizeLaneEntry(_mainOverlayLaneEntry);

        settings.FontFamily = _mainOverlayLaneEntry.FontFamily;
        settings.FontSize = _mainOverlayLaneEntry.FontSize;
        settings.FontWeight = _mainOverlayLaneEntry.FontWeight;
        settings.AppNameFontSize = _mainOverlayLaneEntry.AppNameFontSize;
        settings.AppNameFontWeight = _mainOverlayLaneEntry.AppNameFontWeight;
        settings.TitleFontSize = _mainOverlayLaneEntry.TitleFontSize;
        settings.TitleFontWeight = _mainOverlayLaneEntry.TitleFontWeight;
        settings.LineSpacing = _mainOverlayLaneEntry.LineSpacing;
        settings.TextAlignment = _mainOverlayLaneEntry.TextAlignment;
        settings.TextColor = _mainOverlayLaneEntry.TextColor;
        settings.TitleColor = _mainOverlayLaneEntry.TitleColor;
        settings.AppNameColor = _mainOverlayLaneEntry.AppNameColor;
        settings.BackgroundColor = _mainOverlayLaneEntry.BackgroundColor;
        settings.BackgroundOpacity = _mainOverlayLaneEntry.BackgroundOpacity;
        settings.AccentColor = _mainOverlayLaneEntry.AccentColor;
        settings.OverlayBackgroundImagePath = _mainOverlayLaneEntry.BackgroundImagePath;
        settings.OverlayBackgroundImageOpacity = Math.Clamp(_mainOverlayLaneEntry.BackgroundImageOpacity, 0.0, 1.0);
        settings.OverlayBackgroundImageHueDegrees = _mainOverlayLaneEntry.BackgroundImageHueDegrees;
        settings.OverlayBackgroundImageBrightness = Math.Clamp(_mainOverlayLaneEntry.BackgroundImageBrightness, 0.2, 2.0);
        settings.CornerRadius = _mainOverlayLaneEntry.CornerRadius;
        settings.Padding = _mainOverlayLaneEntry.Padding;
        settings.CardGap = _mainOverlayLaneEntry.CardGap;
        settings.OuterMargin = _mainOverlayLaneEntry.OuterMargin;
        settings.ShowAccent = _mainOverlayLaneEntry.ShowAccent;
        settings.AccentThickness = _mainOverlayLaneEntry.AccentThickness;
        settings.ShowBorder = _mainOverlayLaneEntry.ShowBorder;
        settings.BorderColor = _mainOverlayLaneEntry.BorderColor;
        settings.BorderThickness = _mainOverlayLaneEntry.BorderThickness;
        settings.ShowAppName = _mainOverlayLaneEntry.ShowAppName;
        settings.ShowNotificationTitle = _mainOverlayLaneEntry.ShowNotificationTitle;
        settings.ShowNotificationBody = _mainOverlayLaneEntry.ShowNotificationBody;
        settings.LimitTextLines = _mainOverlayLaneEntry.LimitTextLines;
        settings.MaxAppNameLines = _mainOverlayLaneEntry.MaxAppNameLines;
        settings.MaxTitleLines = _mainOverlayLaneEntry.MaxTitleLines;
        settings.MaxBodyLines = _mainOverlayLaneEntry.MaxBodyLines;
        settings.SingleLineMode = _mainOverlayLaneEntry.SingleLineMode;
        settings.SingleLineWrapText = _mainOverlayLaneEntry.SingleLineWrapText;
        settings.SingleLineMaxLines = _mainOverlayLaneEntry.SingleLineMaxLines;
        settings.SingleLineAutoFullWidth = _mainOverlayLaneEntry.SingleLineAutoFullWidth;
        settings.ShowTimestamp = _mainOverlayLaneEntry.ShowTimestamp;
        settings.TimestampFontSize = _mainOverlayLaneEntry.TimestampFontSize;
        settings.TimestampDisplayMode = _mainOverlayLaneEntry.TimestampDisplayMode;
        settings.TimestampFontWeight = _mainOverlayLaneEntry.TimestampFontWeight;
        settings.TimestampColor = _mainOverlayLaneEntry.TimestampColor;
        settings.DensityPreset = _mainOverlayLaneEntry.DensityPreset;
        settings.SelectedMonitorIndex = _mainOverlayLaneEntry.MonitorIndex;
        settings.MonitorIndex = _mainOverlayLaneEntry.MonitorIndex;
        settings.OverlayLeft = _mainOverlayLaneEntry.Left;
        settings.OverlayTop = _mainOverlayLaneEntry.Top;
        settings.OverlayWidth = Math.Clamp(_mainOverlayLaneEntry.Width, OverlayWidthMin, OverlayWidthMax);
        settings.OverlayMaxHeight = Math.Clamp(_mainOverlayLaneEntry.MaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
    }

    private void RefreshOverlayLaneEntries(IEnumerable<OverlayLaneDefinition> sourceLanes)
    {
        OverlayLaneEntries.Clear();

        foreach (var lane in sourceLanes
                     .Where(lane => !string.Equals(OverlayLaneHelper.Normalize(lane.Id), OverlayLaneHelper.Main, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(lane => lane.Name, StringComparer.OrdinalIgnoreCase))
        {
            var entry = CreateOverlayLaneEntry(lane);
            AttachOverlayLaneEntry(entry);
            OverlayLaneEntries.Add(entry);
        }
    }

    private OverlayLaneEntry CreateOverlayLaneEntry(OverlayLaneDefinition lane)
    {
        return new OverlayLaneEntry(lane.Id, lane.Name)
        {
            IsEnabled = lane.IsEnabled,
            FontFamily = lane.FontFamily,
            FontSize = lane.FontSize,
            FontWeight = lane.FontWeight,
            AppNameFontSize = lane.AppNameFontSize,
            AppNameFontWeight = lane.AppNameFontWeight,
            TitleFontSize = lane.TitleFontSize,
            TitleFontWeight = lane.TitleFontWeight,
            LineSpacing = lane.LineSpacing,
            TextAlignment = lane.TextAlignment,
            MonitorIndex = lane.MonitorIndex,
            PositionPreset = SecondaryOverlayPositionHelper.Normalize(lane.PositionPreset),
            Left = lane.Left,
            Top = lane.Top,
            Width = lane.Width,
            MaxHeight = lane.MaxHeight,
            AccentColor = lane.AccentColor,
            BackgroundColor = lane.BackgroundColor,
            BackgroundOpacity = lane.BackgroundOpacity,
            TitleColor = lane.TitleColor,
            TextColor = lane.TextColor,
            AppNameColor = lane.AppNameColor,
            CornerRadius = lane.CornerRadius,
            Padding = lane.Padding,
            CardGap = lane.CardGap,
            OuterMargin = lane.OuterMargin,
            ShowAccent = lane.ShowAccent,
            AccentThickness = lane.AccentThickness,
            ShowBorder = lane.ShowBorder,
            BorderColor = lane.BorderColor,
            BorderThickness = lane.BorderThickness,
            ShowAppName = lane.ShowAppName,
            ShowNotificationTitle = lane.ShowNotificationTitle,
            ShowNotificationBody = lane.ShowNotificationBody,
            LimitTextLines = lane.LimitTextLines,
            MaxAppNameLines = lane.MaxAppNameLines,
            MaxTitleLines = lane.MaxTitleLines,
            MaxBodyLines = lane.MaxBodyLines,
            SingleLineMode = lane.SingleLineMode,
            SingleLineWrapText = lane.SingleLineWrapText,
            SingleLineMaxLines = lane.SingleLineMaxLines,
            SingleLineAutoFullWidth = lane.SingleLineAutoFullWidth,
            ShowTimestamp = lane.ShowTimestamp,
            TimestampFontSize = lane.TimestampFontSize,
            TimestampDisplayMode = lane.TimestampDisplayMode,
            TimestampFontWeight = lane.TimestampFontWeight,
            TimestampColor = lane.TimestampColor,
            DensityPreset = lane.DensityPreset,
            BackgroundImagePath = lane.BackgroundImagePath,
            BackgroundImageOpacity = lane.BackgroundImageOpacity,
            BackgroundImageHueDegrees = lane.BackgroundImageHueDegrees,
            BackgroundImageBrightness = lane.BackgroundImageBrightness
        };
    }

    private void AttachOverlayLaneEntry(OverlayLaneEntry entry)
    {
        entry.PropertyChanged += (_, args) =>
        {
            if (string.Equals(args.PropertyName, nameof(OverlayLaneEntry.Name), StringComparison.Ordinal))
            {
                RebuildAvailableOverlayLaneChoices();
                OnPropertyChanged(nameof(SelectedOverlayLaneName));
            }

            QueueSave();
        };
    }

    private void RebuildAvailableOverlayLaneChoices()
    {
        AvailableOverlayLaneChoices.Clear();
        AvailableOverlayLaneChoices.Add(new OverlayLaneChoice(OverlayLaneHelper.Main, OverlayLaneHelper.MainDisplayName));

        foreach (var lane in OverlayLaneEntries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
            AvailableOverlayLaneChoices.Add(new OverlayLaneChoice(lane.Id, lane.Name));

        foreach (var profile in AppProfileEntries)
            profile.OverlayLane = NormalizeSelectedLaneId(profile.OverlayLane);
    }

    private static AppProfile ToProfile(AppProfileEntry entry)
    {
        return new AppProfile
        {
            AppName = entry.AppName,
            IsReadAloudEnabled = entry.IsReadAloudEnabled,
            OverlayLane = OverlayLaneHelper.Normalize(entry.OverlayLane),
            Sound = string.IsNullOrWhiteSpace(entry.Sound) ? "Default" : entry.Sound,
            Icon = string.IsNullOrWhiteSpace(entry.Icon) ? "Default" : entry.Icon
        };
    }

    private static OverlayLaneDefinition ToOverlayLaneDefinition(OverlayLaneEntry entry)
    {
        NormalizeLaneEntry(entry);

        return new OverlayLaneDefinition
        {
            Id = OverlayLaneHelper.Normalize(entry.Id),
            Name = string.IsNullOrWhiteSpace(entry.Name) ? "Overlay Lane" : entry.Name.Trim(),
            IsEnabled = entry.IsEnabled,
            FontFamily = entry.FontFamily,
            FontSize = entry.FontSize,
            FontWeight = entry.FontWeight,
            AppNameFontSize = entry.AppNameFontSize,
            AppNameFontWeight = entry.AppNameFontWeight,
            TitleFontSize = entry.TitleFontSize,
            TitleFontWeight = entry.TitleFontWeight,
            LineSpacing = entry.LineSpacing,
            TextAlignment = entry.TextAlignment,
            MonitorIndex = Math.Max(0, entry.MonitorIndex),
            PositionPreset = SecondaryOverlayPositionHelper.Normalize(entry.PositionPreset),
            Left = entry.Left,
            Top = entry.Top,
            Width = Math.Clamp(entry.Width, OverlayWidthMin, OverlayWidthMax),
            MaxHeight = Math.Clamp(entry.MaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax),
            AccentColor = entry.AccentColor,
            BackgroundColor = entry.BackgroundColor,
            BackgroundOpacity = Math.Clamp(entry.BackgroundOpacity, 0.0, 1.0),
            TitleColor = entry.TitleColor,
            TextColor = entry.TextColor,
            AppNameColor = entry.AppNameColor,
            CornerRadius = entry.CornerRadius,
            Padding = entry.Padding,
            CardGap = entry.CardGap,
            OuterMargin = entry.OuterMargin,
            ShowAccent = entry.ShowAccent,
            AccentThickness = entry.AccentThickness,
            ShowBorder = entry.ShowBorder,
            BorderColor = entry.BorderColor,
            BorderThickness = entry.BorderThickness,
            ShowAppName = entry.ShowAppName,
            ShowNotificationTitle = entry.ShowNotificationTitle,
            ShowNotificationBody = entry.ShowNotificationBody,
            LimitTextLines = entry.LimitTextLines,
            MaxAppNameLines = entry.MaxAppNameLines,
            MaxTitleLines = entry.MaxTitleLines,
            MaxBodyLines = entry.MaxBodyLines,
            SingleLineMode = entry.SingleLineMode,
            SingleLineWrapText = entry.SingleLineWrapText,
            SingleLineMaxLines = entry.SingleLineMaxLines,
            SingleLineAutoFullWidth = entry.SingleLineAutoFullWidth,
            ShowTimestamp = entry.ShowTimestamp,
            TimestampFontSize = entry.TimestampFontSize,
            TimestampDisplayMode = entry.TimestampDisplayMode,
            TimestampFontWeight = entry.TimestampFontWeight,
            TimestampColor = entry.TimestampColor,
            DensityPreset = entry.DensityPreset,
            BackgroundImagePath = entry.BackgroundImagePath,
            BackgroundImageOpacity = Math.Clamp(entry.BackgroundImageOpacity, 0.0, 1.0),
            BackgroundImageHueDegrees = Math.Clamp(entry.BackgroundImageHueDegrees, -180.0, 180.0),
            BackgroundImageBrightness = Math.Clamp(entry.BackgroundImageBrightness, 0.2, 2.0)
        };
    }

    private OverlayLaneEntry? GetSelectedOverlayLaneEntry()
    {
        if (string.Equals(_selectedOverlayLaneId, OverlayLaneHelper.Main, StringComparison.OrdinalIgnoreCase))
            return _mainOverlayLaneEntry;

        return OverlayLaneEntries.FirstOrDefault(entry =>
            string.Equals(entry.Id, _selectedOverlayLaneId, StringComparison.OrdinalIgnoreCase));
    }

    private void LoadSelectedLaneEditor()
    {
        var entry = GetSelectedOverlayLaneEntry() ?? _mainOverlayLaneEntry;
        NormalizeLaneEntry(entry);

        _fontFamily = entry.FontFamily;
        _fontSize = entry.FontSize;
        _fontWeight = entry.FontWeight;
        _appNameFontSize = entry.AppNameFontSize;
        _appNameFontWeight = entry.AppNameFontWeight;
        _titleFontSize = entry.TitleFontSize;
        _titleFontWeight = entry.TitleFontWeight;
        _lineSpacing = entry.LineSpacing;
        _textAlignment = entry.TextAlignment;
        _textColor = entry.TextColor;
        _titleColor = entry.TitleColor;
        _appNameColor = entry.AppNameColor;
        _backgroundColor = entry.BackgroundColor;
        _backgroundOpacity = entry.BackgroundOpacity;
        _accentColor = entry.AccentColor;
        _cornerRadius = entry.CornerRadius;
        _padding = entry.Padding;
        _cardGap = entry.CardGap;
        _outerMargin = entry.OuterMargin;
        _showAccent = entry.ShowAccent;
        _accentThickness = entry.AccentThickness;
        _showBorder = entry.ShowBorder;
        _borderColor = entry.BorderColor;
        _borderThickness = entry.BorderThickness;
        _showAppName = entry.ShowAppName;
        _showNotificationTitle = entry.ShowNotificationTitle;
        _showNotificationBody = entry.ShowNotificationBody;
        _limitTextLines = entry.LimitTextLines;
        _maxAppNameLines = entry.MaxAppNameLines;
        _maxTitleLines = entry.MaxTitleLines;
        _maxBodyLines = entry.MaxBodyLines;
        _singleLineMode = entry.SingleLineMode;
        _singleLineWrapText = entry.SingleLineWrapText;
        _singleLineMaxLines = entry.SingleLineMaxLines;
        _singleLineAutoFullWidth = entry.SingleLineAutoFullWidth;
        _showTimestamp = entry.ShowTimestamp;
        _timestampFontSize = entry.TimestampFontSize;
        _timestampDisplayMode = entry.TimestampDisplayMode;
        _timestampFontWeight = entry.TimestampFontWeight;
        _timestampColor = entry.TimestampColor;
        _densityPreset = entry.DensityPreset;
        _selectedMonitorIndex = Math.Max(0, entry.MonitorIndex);
        _overlayWidth = Math.Clamp(entry.Width, OverlayWidthMin, OverlayWidthMax);
        _overlayMaxHeight = Math.Clamp(entry.MaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
        _overlayBackgroundImagePath = entry.BackgroundImagePath;
        _overlayBackgroundImageOpacity = Math.Clamp(entry.BackgroundImageOpacity, 0.0, 1.0);
        _overlayBackgroundImageHueDegrees = Math.Clamp(entry.BackgroundImageHueDegrees, -180.0, 180.0);
        _overlayBackgroundImageBrightness = Math.Clamp(entry.BackgroundImageBrightness, 0.2, 2.0);
        _overlayWidthDirty = false;

        OnPropertyChanged(nameof(FontFamily));
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(FontWeight));
        OnPropertyChanged(nameof(AppNameFontSize));
        OnPropertyChanged(nameof(AppNameFontWeight));
        OnPropertyChanged(nameof(TitleFontSize));
        OnPropertyChanged(nameof(TitleFontWeight));
        OnPropertyChanged(nameof(LineSpacing));
        OnPropertyChanged(nameof(TextAlignment));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(TitleColor));
        OnPropertyChanged(nameof(AppNameColor));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(BackgroundOpacity));
        OnPropertyChanged(nameof(AccentColor));
        OnPropertyChanged(nameof(CornerRadius));
        OnPropertyChanged(nameof(Padding));
        OnPropertyChanged(nameof(CardGap));
        OnPropertyChanged(nameof(OuterMargin));
        OnPropertyChanged(nameof(ShowAccent));
        OnPropertyChanged(nameof(AccentThickness));
        OnPropertyChanged(nameof(ShowBorder));
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(BorderThickness));
        OnPropertyChanged(nameof(ShowAppName));
        OnPropertyChanged(nameof(ShowNotificationTitle));
        OnPropertyChanged(nameof(ShowNotificationBody));
        OnPropertyChanged(nameof(LimitTextLines));
        OnPropertyChanged(nameof(MaxAppNameLines));
        OnPropertyChanged(nameof(MaxTitleLines));
        OnPropertyChanged(nameof(MaxBodyLines));
        OnPropertyChanged(nameof(SingleLineMode));
        OnPropertyChanged(nameof(SingleLineWrapText));
        OnPropertyChanged(nameof(SingleLineMaxLines));
        OnPropertyChanged(nameof(SingleLineAutoFullWidth));
        OnPropertyChanged(nameof(ShowTimestamp));
        OnPropertyChanged(nameof(TimestampFontSize));
        OnPropertyChanged(nameof(TimestampDisplayMode));
        OnPropertyChanged(nameof(TimestampFontWeight));
        OnPropertyChanged(nameof(TimestampColor));
        OnPropertyChanged(nameof(DensityPreset));
        OnPropertyChanged(nameof(SelectedMonitorIndex));
        OnPropertyChanged(nameof(OverlayWidth));
        OnPropertyChanged(nameof(OverlayMaxHeight));
        OnPropertyChanged(nameof(OverlayBackgroundImagePath));
        OnPropertyChanged(nameof(OverlayBackgroundImageOpacity));
        OnPropertyChanged(nameof(OverlayBackgroundImageHueDegrees));
        OnPropertyChanged(nameof(OverlayBackgroundImageBrightness));
        OnPropertyChanged(nameof(IsStackedLayout));
    }

    private void SyncSelectedOverlayLaneFromEditor()
    {
        var entry = GetSelectedOverlayLaneEntry();
        if (entry == null)
            return;

        entry.FontFamily = _fontFamily;
        entry.FontSize = _fontSize;
        entry.FontWeight = _fontWeight;
        entry.AppNameFontSize = _appNameFontSize;
        entry.AppNameFontWeight = _appNameFontWeight;
        entry.TitleFontSize = _titleFontSize;
        entry.TitleFontWeight = _titleFontWeight;
        entry.LineSpacing = _lineSpacing;
        entry.TextAlignment = _textAlignment;
        entry.MonitorIndex = Math.Max(0, _selectedMonitorIndex);
        entry.Width = Math.Clamp(_overlayWidth, OverlayWidthMin, OverlayWidthMax);
        entry.MaxHeight = Math.Clamp(_overlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
        entry.AccentColor = _accentColor;
        entry.BackgroundColor = _backgroundColor;
        entry.BackgroundOpacity = Math.Clamp(_backgroundOpacity, 0.0, 1.0);
        entry.TitleColor = _titleColor;
        entry.TextColor = _textColor;
        entry.AppNameColor = _appNameColor;
        entry.CornerRadius = _cornerRadius;
        entry.Padding = _padding;
        entry.CardGap = _cardGap;
        entry.OuterMargin = _outerMargin;
        entry.ShowAccent = _showAccent;
        entry.AccentThickness = _accentThickness;
        entry.ShowBorder = _showBorder;
        entry.BorderColor = _borderColor;
        entry.BorderThickness = _borderThickness;
        entry.ShowAppName = _showAppName;
        entry.ShowNotificationTitle = _showNotificationTitle;
        entry.ShowNotificationBody = _showNotificationBody;
        entry.LimitTextLines = _limitTextLines;
        entry.MaxAppNameLines = _maxAppNameLines;
        entry.MaxTitleLines = _maxTitleLines;
        entry.MaxBodyLines = _maxBodyLines;
        entry.SingleLineMode = _singleLineMode;
        entry.SingleLineWrapText = _singleLineWrapText;
        entry.SingleLineMaxLines = _singleLineMaxLines;
        entry.SingleLineAutoFullWidth = _singleLineAutoFullWidth;
        entry.ShowTimestamp = _showTimestamp;
        entry.TimestampFontSize = _timestampFontSize;
        entry.TimestampDisplayMode = _timestampDisplayMode;
        entry.TimestampFontWeight = _timestampFontWeight;
        entry.TimestampColor = _timestampColor;
        entry.DensityPreset = _densityPreset;
        entry.BackgroundImagePath = _overlayBackgroundImagePath;
        entry.BackgroundImageOpacity = Math.Clamp(_overlayBackgroundImageOpacity, 0.0, 1.0);
        entry.BackgroundImageHueDegrees = Math.Clamp(_overlayBackgroundImageHueDegrees, -180.0, 180.0);
        entry.BackgroundImageBrightness = Math.Clamp(_overlayBackgroundImageBrightness, 0.2, 2.0);

        NormalizeLaneEntry(entry);
    }

    private void NotifySelectedOverlayLaneChanged()
    {
        OnPropertyChanged(nameof(SelectedOverlayLaneEntry));
        OnPropertyChanged(nameof(SelectedOverlayLaneId));
        OnPropertyChanged(nameof(SelectedOverlayLaneName));
        OnPropertyChanged(nameof(IsMainOverlayLaneSelected));
        OnPropertyChanged(nameof(CanRemoveSelectedOverlayLane));
        CommandManager.InvalidateRequerySuggested();
    }

    private string NormalizeSelectedLaneId(string? laneId)
    {
        var lanes = OverlayLaneEntries.Select(ToOverlayLaneDefinition).ToList();
        return NormalizeSelectedLaneId(laneId, lanes);
    }

    private static string NormalizeSelectedLaneId(string? laneId, IEnumerable<OverlayLaneDefinition> lanes)
    {
        return OverlayLaneHelper.NormalizeOrMain(laneId, lanes);
    }

    private void AddOverlayLane()
    {
        SyncSelectedOverlayLaneFromEditor();

        var laneName = BuildUniqueLaneName(GetNextLaneBaseName());
        var laneId = BuildUniqueLaneId(laneName);
        var defaultPreset = AvailableSecondaryOverlayPositionPresets[
            OverlayLaneEntries.Count % AvailableSecondaryOverlayPositionPresets.Count];

        var seed = _mainOverlayLaneEntry;
        var entry = new OverlayLaneEntry(laneId, laneName)
        {
            IsEnabled = true,
            FontFamily = seed.FontFamily,
            FontSize = seed.FontSize,
            FontWeight = seed.FontWeight,
            AppNameFontSize = seed.AppNameFontSize,
            AppNameFontWeight = seed.AppNameFontWeight,
            TitleFontSize = seed.TitleFontSize,
            TitleFontWeight = seed.TitleFontWeight,
            LineSpacing = seed.LineSpacing,
            TextAlignment = seed.TextAlignment,
            MonitorIndex = seed.MonitorIndex,
            PositionPreset = defaultPreset,
            Left = null,
            Top = null,
            Width = Math.Clamp(seed.Width, OverlayWidthMin, OverlayWidthMax),
            MaxHeight = Math.Clamp(seed.MaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax),
            AccentColor = seed.AccentColor,
            BackgroundColor = seed.BackgroundColor,
            BackgroundOpacity = seed.BackgroundOpacity,
            TitleColor = seed.TitleColor,
            TextColor = seed.TextColor,
            AppNameColor = seed.AppNameColor,
            CornerRadius = seed.CornerRadius,
            Padding = seed.Padding,
            CardGap = seed.CardGap,
            OuterMargin = seed.OuterMargin,
            ShowAccent = seed.ShowAccent,
            AccentThickness = seed.AccentThickness,
            ShowBorder = seed.ShowBorder,
            BorderColor = seed.BorderColor,
            BorderThickness = seed.BorderThickness,
            ShowAppName = seed.ShowAppName,
            ShowNotificationTitle = seed.ShowNotificationTitle,
            ShowNotificationBody = seed.ShowNotificationBody,
            LimitTextLines = seed.LimitTextLines,
            MaxAppNameLines = seed.MaxAppNameLines,
            MaxTitleLines = seed.MaxTitleLines,
            MaxBodyLines = seed.MaxBodyLines,
            SingleLineMode = seed.SingleLineMode,
            SingleLineWrapText = seed.SingleLineWrapText,
            SingleLineMaxLines = seed.SingleLineMaxLines,
            SingleLineAutoFullWidth = seed.SingleLineAutoFullWidth,
            ShowTimestamp = seed.ShowTimestamp,
            TimestampFontSize = seed.TimestampFontSize,
            TimestampDisplayMode = seed.TimestampDisplayMode,
            TimestampFontWeight = seed.TimestampFontWeight,
            TimestampColor = seed.TimestampColor,
            DensityPreset = seed.DensityPreset,
            BackgroundImagePath = seed.BackgroundImagePath,
            BackgroundImageOpacity = seed.BackgroundImageOpacity,
            BackgroundImageHueDegrees = seed.BackgroundImageHueDegrees,
            BackgroundImageBrightness = seed.BackgroundImageBrightness
        };

        AttachOverlayLaneEntry(entry);
        OverlayLaneEntries.Add(entry);
        RebuildAvailableOverlayLaneChoices();
        SelectedOverlayLaneId = entry.Id;
        QueueSave();
    }

    private void DuplicateSelectedOverlayLane()
    {
        SyncSelectedOverlayLaneFromEditor();

        var source = GetSelectedOverlayLaneEntry();
        if (source == null)
            return;

        var duplicateName = BuildUniqueLaneName(IsMainOverlayLaneSelected ? GetNextLaneBaseName() : $"{source.Name} Copy");
        var duplicate = new OverlayLaneEntry(BuildUniqueLaneId(duplicateName), duplicateName)
        {
            IsEnabled = true,
            FontFamily = source.FontFamily,
            FontSize = source.FontSize,
            FontWeight = source.FontWeight,
            AppNameFontSize = source.AppNameFontSize,
            AppNameFontWeight = source.AppNameFontWeight,
            TitleFontSize = source.TitleFontSize,
            TitleFontWeight = source.TitleFontWeight,
            LineSpacing = source.LineSpacing,
            TextAlignment = source.TextAlignment,
            MonitorIndex = source.MonitorIndex,
            PositionPreset = GetNextPositionPreset(source.PositionPreset),
            Left = null,
            Top = null,
            Width = source.Width,
            MaxHeight = source.MaxHeight,
            AccentColor = source.AccentColor,
            BackgroundColor = source.BackgroundColor,
            BackgroundOpacity = source.BackgroundOpacity,
            TitleColor = source.TitleColor,
            TextColor = source.TextColor,
            AppNameColor = source.AppNameColor,
            CornerRadius = source.CornerRadius,
            Padding = source.Padding,
            CardGap = source.CardGap,
            OuterMargin = source.OuterMargin,
            ShowAccent = source.ShowAccent,
            AccentThickness = source.AccentThickness,
            ShowBorder = source.ShowBorder,
            BorderColor = source.BorderColor,
            BorderThickness = source.BorderThickness,
            ShowAppName = source.ShowAppName,
            ShowNotificationTitle = source.ShowNotificationTitle,
            ShowNotificationBody = source.ShowNotificationBody,
            LimitTextLines = source.LimitTextLines,
            MaxAppNameLines = source.MaxAppNameLines,
            MaxTitleLines = source.MaxTitleLines,
            MaxBodyLines = source.MaxBodyLines,
            SingleLineMode = source.SingleLineMode,
            SingleLineWrapText = source.SingleLineWrapText,
            SingleLineMaxLines = source.SingleLineMaxLines,
            SingleLineAutoFullWidth = source.SingleLineAutoFullWidth,
            ShowTimestamp = source.ShowTimestamp,
            TimestampFontSize = source.TimestampFontSize,
            TimestampDisplayMode = source.TimestampDisplayMode,
            TimestampFontWeight = source.TimestampFontWeight,
            TimestampColor = source.TimestampColor,
            DensityPreset = source.DensityPreset,
            BackgroundImagePath = source.BackgroundImagePath,
            BackgroundImageOpacity = source.BackgroundImageOpacity,
            BackgroundImageHueDegrees = source.BackgroundImageHueDegrees,
            BackgroundImageBrightness = source.BackgroundImageBrightness
        };

        AttachOverlayLaneEntry(duplicate);
        OverlayLaneEntries.Add(duplicate);
        RebuildAvailableOverlayLaneChoices();
        SelectedOverlayLaneId = duplicate.Id;
        QueueSave();
    }

    private void RemoveSelectedOverlayLane()
    {
        if (!CanRemoveSelectedOverlayLane || SelectedOverlayLaneEntry == null)
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
        SelectedOverlayLaneId = OverlayLaneHelper.Main;
        QueueSave();
    }

    private string BuildUniqueLaneName(string baseName)
    {
        var names = OverlayLaneEntries.Select(entry => entry.Name)
            .Append(OverlayLaneHelper.MainDisplayName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
        var ids = OverlayLaneEntries.Select(entry => entry.Id)
            .Append(OverlayLaneHelper.Main)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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

    private string GetNextLaneBaseName()
    {
        var laneNumber = OverlayLaneEntries.Count + 2;
        return $"Lane {laneNumber}";
    }

    private string GetNextPositionPreset(string currentPreset)
    {
        var index = AvailableSecondaryOverlayPositionPresets.FindIndex(preset =>
            string.Equals(preset, SecondaryOverlayPositionHelper.Normalize(currentPreset), StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return AvailableSecondaryOverlayPositionPresets[0];

        return AvailableSecondaryOverlayPositionPresets[(index + 1) % AvailableSecondaryOverlayPositionPresets.Count];
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

    private void BrowseOverlayLaneBackgroundImage()
    {
        var entry = GetSelectedOverlayLaneEntry();
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
            OverlayBackgroundImagePath = destinationPath;
        }
        catch
        {
            return;
        }
    }

    private void ClearOverlayLaneBackgroundImage()
    {
        OverlayBackgroundImagePath = string.Empty;
        OverlayBackgroundImageHueDegrees = 0;
        OverlayBackgroundImageBrightness = 1.0;
        OverlayBackgroundImageOpacity = 0.45;
    }

    private static void NormalizeLaneEntry(OverlayLaneEntry entry)
    {
        entry.FontFamily = string.IsNullOrWhiteSpace(entry.FontFamily) ? "Segoe UI" : entry.FontFamily.Trim();
        entry.FontSize = entry.FontSize <= 0 ? 14 : entry.FontSize;
        entry.FontWeight = string.IsNullOrWhiteSpace(entry.FontWeight) ? "Normal" : entry.FontWeight.Trim();
        entry.AppNameFontSize = entry.AppNameFontSize <= 0 ? 14 : entry.AppNameFontSize;
        entry.AppNameFontWeight = string.IsNullOrWhiteSpace(entry.AppNameFontWeight) ? "SemiBold" : entry.AppNameFontWeight.Trim();
        entry.TitleFontSize = entry.TitleFontSize <= 0 ? 16 : entry.TitleFontSize;
        entry.TitleFontWeight = string.IsNullOrWhiteSpace(entry.TitleFontWeight) ? "SemiBold" : entry.TitleFontWeight.Trim();
        entry.LineSpacing = entry.LineSpacing <= 0 ? 1.5 : entry.LineSpacing;
        entry.TextAlignment = string.IsNullOrWhiteSpace(entry.TextAlignment) ? "Left" : entry.TextAlignment.Trim();
        entry.Width = Math.Clamp(entry.Width <= 0 ? 340 : entry.Width, OverlayWidthMin, OverlayWidthMax);
        entry.MaxHeight = Math.Clamp(entry.MaxHeight <= 0 ? 480 : entry.MaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
        entry.BackgroundOpacity = entry.BackgroundOpacity <= 0 ? 0.94 : Math.Clamp(entry.BackgroundOpacity, 0.0, 1.0);
        entry.CornerRadius = entry.CornerRadius < 0 ? 0 : entry.CornerRadius;
        entry.Padding = entry.Padding <= 0 ? 16 : entry.Padding;
        entry.CardGap = entry.CardGap < 0 ? 0 : entry.CardGap;
        entry.OuterMargin = entry.OuterMargin < 0 ? 0 : entry.OuterMargin;
        entry.AccentThickness = entry.AccentThickness <= 0 ? 3 : entry.AccentThickness;
        entry.BorderColor = string.IsNullOrWhiteSpace(entry.BorderColor) ? "#3A3A3A" : entry.BorderColor.Trim();
        entry.BorderThickness = entry.BorderThickness <= 0 ? 1 : entry.BorderThickness;
        entry.MaxAppNameLines = entry.MaxAppNameLines <= 0 ? 2 : entry.MaxAppNameLines;
        entry.MaxTitleLines = entry.MaxTitleLines <= 0 ? 2 : entry.MaxTitleLines;
        entry.MaxBodyLines = entry.MaxBodyLines <= 0 ? 4 : entry.MaxBodyLines;
        entry.SingleLineMaxLines = entry.SingleLineMaxLines <= 0 ? 3 : entry.SingleLineMaxLines;
        entry.TimestampFontSize = entry.TimestampFontSize <= 0 ? 11 : entry.TimestampFontSize;
        entry.TimestampDisplayMode = string.IsNullOrWhiteSpace(entry.TimestampDisplayMode) ? "Relative" : entry.TimestampDisplayMode.Trim();
        entry.TimestampFontWeight = string.IsNullOrWhiteSpace(entry.TimestampFontWeight) ? "Normal" : entry.TimestampFontWeight.Trim();
        entry.TimestampColor = string.IsNullOrWhiteSpace(entry.TimestampColor) ? "#C8C8C8" : entry.TimestampColor.Trim();
        entry.DensityPreset = string.IsNullOrWhiteSpace(entry.DensityPreset) ? "Comfortable" : entry.DensityPreset.Trim();
        entry.PositionPreset = SecondaryOverlayPositionHelper.Normalize(entry.PositionPreset);
        entry.BackgroundImageOpacity = Math.Clamp(entry.BackgroundImageOpacity, 0.0, 1.0);
        entry.BackgroundImageBrightness = Math.Clamp(entry.BackgroundImageBrightness, 0.2, 2.0);

        if (!entry.ShowAppName && !entry.ShowNotificationTitle && !entry.ShowNotificationBody)
            entry.ShowNotificationBody = true;
    }
}
