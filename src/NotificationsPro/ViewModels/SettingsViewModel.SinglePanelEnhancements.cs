using System.Collections.ObjectModel;
using System.IO;
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

    private string _cardBackgroundImagePath = string.Empty;
    public string CardBackgroundImagePath
    {
        get => _cardBackgroundImagePath;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (!SetProperty(ref _cardBackgroundImagePath, normalized))
                return;

            OnPropertyChanged(nameof(HasCardBackgroundImage));
            QueueSave();
        }
    }

    public bool HasCardBackgroundImage => !string.IsNullOrWhiteSpace(CardBackgroundImagePath);

    private double _cardBackgroundImageOpacity = 0.45;
    public double CardBackgroundImageOpacity
    {
        get => _cardBackgroundImageOpacity;
        set
        {
            if (SetProperty(ref _cardBackgroundImageOpacity, Math.Clamp(value, 0.0, 1.0)))
                QueueSave();
        }
    }

    private double _cardBackgroundImageHueDegrees;
    public double CardBackgroundImageHueDegrees
    {
        get => _cardBackgroundImageHueDegrees;
        set
        {
            if (SetProperty(ref _cardBackgroundImageHueDegrees, Math.Clamp(value, -180, 180)))
                QueueSave();
        }
    }

    private double _cardBackgroundImageBrightness = 1.0;
    public double CardBackgroundImageBrightness
    {
        get => _cardBackgroundImageBrightness;
        set
        {
            if (SetProperty(ref _cardBackgroundImageBrightness, Math.Clamp(value, 0.2, 2.0)))
                QueueSave();
        }
    }

    private string _cardBackgroundImageFitMode = CardBackgroundImageFitModeHelper.FillCard;
    public string CardBackgroundImageFitMode
    {
        get => _cardBackgroundImageFitMode;
        set
        {
            var normalized = CardBackgroundImageFitModeHelper.Normalize(value);
            if (SetProperty(ref _cardBackgroundImageFitMode, normalized))
                QueueSave();
        }
    }

    private string _cardBackgroundImagePlacement = CardBackgroundImagePlacementHelper.InsidePadding;
    public string CardBackgroundImagePlacement
    {
        get => _cardBackgroundImagePlacement;
        set
        {
            var normalized = CardBackgroundImagePlacementHelper.Normalize(value);
            if (SetProperty(ref _cardBackgroundImagePlacement, normalized))
                QueueSave();
        }
    }

    private string _fullscreenOverlayImagePath = string.Empty;
    public string FullscreenOverlayImagePath
    {
        get => _fullscreenOverlayImagePath;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (!SetProperty(ref _fullscreenOverlayImagePath, normalized))
                return;

            QueueSave();
        }
    }

    private string _fullscreenOverlayImageFitMode = CardBackgroundImageFitModeHelper.FillCard;
    public string FullscreenOverlayImageFitMode
    {
        get => _fullscreenOverlayImageFitMode;
        set
        {
            var normalized = CardBackgroundImageFitModeHelper.Normalize(value);
            if (SetProperty(ref _fullscreenOverlayImageFitMode, normalized))
                QueueSave();
        }
    }

    private string _newNarrationKeyword = string.Empty;
    public string NewNarrationKeyword { get => _newNarrationKeyword; set => SetProperty(ref _newNarrationKeyword, value); }

    public ObservableCollection<NarrationRuleEntry> NarrationRuleEntries { get; } = new();

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
    public List<string> AvailableCardBackgroundImageFitModes { get; } = CardBackgroundImageFitModeHelper.KnownModes.ToList();
    public List<string> AvailableCardBackgroundImagePlacements { get; } = CardBackgroundImagePlacementHelper.KnownPlacements.ToList();

    public ICommand AddNarrationRuleCommand { get; private set; } = null!;
    public ICommand RemoveNarrationRuleCommand { get; private set; } = null!;
    public ICommand BrowseCardBackgroundImageCommand { get; private set; } = null!;
    public ICommand ClearCardBackgroundImageCommand { get; private set; } = null!;
    public ICommand BrowseFullscreenOverlayImageCommand { get; private set; } = null!;
    public ICommand ClearFullscreenOverlayImageCommand { get; private set; } = null!;

    private void InitializeSinglePanelEnhancementCommands()
    {
        AddNarrationRuleCommand = new RelayCommand(_ => AddNarrationRule());
        RemoveNarrationRuleCommand = new RelayCommand(RemoveNarrationRule);
        BrowseCardBackgroundImageCommand = new RelayCommand(_ => BrowseCardBackgroundImage());
        ClearCardBackgroundImageCommand = new RelayCommand(_ => ClearCardBackgroundImage());
        BrowseFullscreenOverlayImageCommand = new RelayCommand(_ => BrowseFullscreenOverlayImage());
        ClearFullscreenOverlayImageCommand = new RelayCommand(_ => ClearFullscreenOverlayImage());
    }

    private void LoadSinglePanelEnhancements(AppSettings settings)
    {
        _showQuickTips = settings.ShowQuickTips;
        _cardBackgroundImagePath = settings.CardBackgroundImagePath?.Trim() ?? string.Empty;
        _cardBackgroundImageOpacity = Math.Clamp(settings.CardBackgroundImageOpacity, 0.0, 1.0);
        _cardBackgroundImageHueDegrees = Math.Clamp(settings.CardBackgroundImageHueDegrees, -180, 180);
        _cardBackgroundImageBrightness = Math.Clamp(settings.CardBackgroundImageBrightness, 0.2, 2.0);
        _cardBackgroundImageFitMode = CardBackgroundImageFitModeHelper.Normalize(settings.CardBackgroundImageFitMode);
        _cardBackgroundImagePlacement = CardBackgroundImagePlacementHelper.Normalize(settings.CardBackgroundImagePlacement);
        _fullscreenOverlayImagePath = settings.FullscreenOverlayImagePath?.Trim() ?? string.Empty;
        _fullscreenOverlayImageFitMode = CardBackgroundImageFitModeHelper.Normalize(settings.FullscreenOverlayImageFitMode);
        _newNarrationKeyword = string.Empty;

        LoadHighlightEntries(settings);
        LoadMuteEntries(settings);
        LoadNarrationEntries(settings);

        OnPropertyChanged(nameof(ShowQuickTips));
        OnPropertyChanged(nameof(CardBackgroundImagePath));
        OnPropertyChanged(nameof(HasCardBackgroundImage));
        OnPropertyChanged(nameof(CardBackgroundImageOpacity));
        OnPropertyChanged(nameof(CardBackgroundImageHueDegrees));
        OnPropertyChanged(nameof(CardBackgroundImageBrightness));
        OnPropertyChanged(nameof(CardBackgroundImageFitMode));
        OnPropertyChanged(nameof(CardBackgroundImagePlacement));
        OnPropertyChanged(nameof(FullscreenOverlayImagePath));
        OnPropertyChanged(nameof(FullscreenOverlayImageFitMode));
        OnPropertyChanged(nameof(NewNarrationKeyword));
    }

    private void LoadHighlightEntries(AppSettings settings)
    {
        HighlightKeywordEntries.Clear();

        if (settings.HighlightRules.Count > 0)
        {
            foreach (var rule in settings.HighlightRules)
            {
                var entry = new KeywordHighlightEntry(
                    rule.Keyword,
                    string.IsNullOrWhiteSpace(rule.Color) ? settings.HighlightColor : rule.Color,
                    rule.IsRegex,
                    rule.Scope,
                    rule.AppFilter);
                entry.PropertyChanged += (_, _) => QueueSave();
                HighlightKeywordEntries.Add(entry);
            }

            return;
        }

        foreach (var keyword in settings.HighlightKeywords)
        {
            var color = settings.PerKeywordColors.TryGetValue(keyword, out var keywordColor)
                ? keywordColor
                : settings.HighlightColor;
            var isRegex = settings.HighlightKeywordRegexFlags.TryGetValue(keyword, out var flag) && flag;
            var entry = new KeywordHighlightEntry(keyword, color, isRegex);
            entry.PropertyChanged += (_, _) => QueueSave();
            HighlightKeywordEntries.Add(entry);
        }
    }

    private void LoadMuteEntries(AppSettings settings)
    {
        MuteKeywordEntries.Clear();

        if (settings.MuteRules.Count > 0)
        {
            foreach (var rule in settings.MuteRules)
            {
                var entry = new MuteKeywordEntry(rule.Keyword, rule.IsRegex, rule.Scope, rule.AppFilter);
                entry.PropertyChanged += (_, _) => QueueSave();
                MuteKeywordEntries.Add(entry);
            }

            return;
        }

        foreach (var keyword in settings.MuteKeywords)
        {
            var isRegex = settings.MuteKeywordRegexFlags.TryGetValue(keyword, out var flag) && flag;
            var entry = new MuteKeywordEntry(keyword, isRegex);
            entry.PropertyChanged += (_, _) => QueueSave();
            MuteKeywordEntries.Add(entry);
        }
    }

    private void LoadNarrationEntries(AppSettings settings)
    {
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
    }

    private List<HighlightRuleDefinition> BuildHighlightRules()
    {
        return HighlightKeywordEntries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Keyword))
            .Select(entry => new HighlightRuleDefinition
            {
                Keyword = entry.Keyword,
                Color = entry.Color,
                IsRegex = entry.IsRegex,
                Scope = NotificationMatchScopeHelper.Normalize(entry.Scope),
                AppFilter = entry.AppFilter?.Trim() ?? string.Empty
            })
            .ToList();
    }

    private List<MuteRuleDefinition> BuildMuteRules()
    {
        return MuteKeywordEntries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Keyword))
            .Select(entry => new MuteRuleDefinition
            {
                Keyword = entry.Keyword,
                IsRegex = entry.IsRegex,
                Scope = NotificationMatchScopeHelper.Normalize(entry.Scope),
                AppFilter = entry.AppFilter?.Trim() ?? string.Empty
            })
            .ToList();
    }

    private List<NarrationRuleDefinition> BuildNarrationRules()
    {
        return NarrationRuleEntries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Keyword))
            .Select(entry => new NarrationRuleDefinition
            {
                Keyword = entry.Keyword,
                IsRegex = entry.IsRegex,
                Scope = NotificationMatchScopeHelper.Normalize(entry.Scope),
                AppFilter = entry.AppFilter?.Trim() ?? string.Empty,
                Action = NarrationRuleActionHelper.Normalize(entry.Action),
                ReadMode = NarrationRuleReadModeHelper.Normalize(entry.ReadMode)
            })
            .ToList();
    }

    private void AddNarrationRule()
    {
        var keyword = NewNarrationKeyword?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
            return;

        if (!NarrationRuleEntries.Any(entry => string.Equals(entry.Keyword, keyword, StringComparison.OrdinalIgnoreCase)))
        {
            var entry = new NarrationRuleEntry(keyword);
            entry.PropertyChanged += (_, _) => QueueSave();
            NarrationRuleEntries.Add(entry);
            QueueSave();
        }

        NewNarrationKeyword = string.Empty;
    }

    private void RemoveNarrationRule(object? parameter)
    {
        NarrationRuleEntry? entry = parameter switch
        {
            NarrationRuleEntry ruleEntry => ruleEntry,
            string keyword => NarrationRuleEntries.FirstOrDefault(item => item.Keyword == keyword),
            _ => null
        };

        if (entry == null)
            return;

        NarrationRuleEntries.Remove(entry);
        QueueSave();
    }

    private void BrowseCardBackgroundImage()
    {
        var importedPath = ImportBackgroundImage("Choose a notification card background image");
        if (!string.IsNullOrWhiteSpace(importedPath))
            CardBackgroundImagePath = importedPath;
    }

    private void ClearCardBackgroundImage()
    {
        if (string.IsNullOrWhiteSpace(CardBackgroundImagePath))
            return;

        CardBackgroundImagePath = string.Empty;
    }

    private void BrowseFullscreenOverlayImage()
    {
        var importedPath = ImportBackgroundImage("Choose a fullscreen backdrop image");
        if (!string.IsNullOrWhiteSpace(importedPath))
            FullscreenOverlayImagePath = importedPath;
    }

    private void ClearFullscreenOverlayImage()
    {
        if (string.IsNullOrWhiteSpace(FullscreenOverlayImagePath))
            return;

        FullscreenOverlayImagePath = string.Empty;
    }

    private static string? ImportBackgroundImage(string dialogTitle)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp",
            Title = dialogTitle
        };

        if (dialog.ShowDialog() != true)
            return null;

        BackgroundImageService.EnsureBackgroundsDirExists();
        var destinationDirectory = BackgroundImageService.GetCustomBackgroundsDir();
        var fileName = Path.GetFileName(dialog.FileName);
        var destinationPath = Path.Combine(destinationDirectory, fileName);

        try
        {
            File.Copy(dialog.FileName, destinationPath, overwrite: true);
            return destinationPath;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not copy the selected background image.\n\n{ex.Message}",
                "Background Image",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return null;
        }
    }
}
