using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using NotificationsPro.Models;
using NotificationsPro.ViewModels;

namespace NotificationsPro.Services;

/// <summary>
/// Manages the in-memory notification queue.
/// - Max N visible notifications at a time (from settings).
/// - Overflow notifications store only a count (content is discarded).
/// - All notification content exists only in RAM and is discarded after expiry.
/// </summary>
public class QueueManager : BaseViewModel
{
    private readonly SettingsManager _settingsManager;
    private readonly ObservableCollection<NotificationItem> _visibleNotifications = new();
    private readonly Dictionary<NotificationItem, DispatcherTimer> _expiryTimers = new();
    private readonly List<DateTime> _recentNotificationTimes = new();

    /// <summary>
    /// App names seen this session (RAM only, never persisted). Used by Settings UI for mute toggles.
    /// </summary>
    public HashSet<string> SeenAppNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Session-only notification archive (RAM only, never persisted to disk).
    /// Stores lightweight snapshots of notifications that were displayed this session.
    /// Cleared when the app closes. Opt-in via SessionArchiveEnabled setting.
    /// </summary>
    public List<ArchiveEntry> SessionArchive { get; } = new();

    public record ArchiveEntry(string AppName, string Title, string Body, DateTime ReceivedAt);

    public ReadOnlyObservableCollection<NotificationItem> VisibleNotifications { get; }

    private int _overflowCount;
    public int OverflowCount
    {
        get => _overflowCount;
        private set
        {
            if (SetProperty(ref _overflowCount, value))
                OnPropertyChanged(nameof(HasOverflow));
        }
    }

    public bool HasOverflow => OverflowCount > 0;

    /// <summary>
    /// Fires after a notification is successfully added (with the app name).
    /// Used by SoundService to play per-app sounds.
    /// </summary>
    public event Action<string>? NotificationAdded;

    /// <summary>
    /// Fires after a notification becomes visible in the overlay.
    /// Used by spoken-notification features without persisting any extra content.
    /// </summary>
    public event Action<NotificationItem>? NotificationDisplayed;

    public QueueManager(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        VisibleNotifications = new ReadOnlyObservableCollection<NotificationItem>(_visibleNotifications);
        _settingsManager.SettingsChanged += OnSettingsChanged;
    }

    public void AddNotification(string title, string body)
        => AddNotification(string.Empty, title, body);

    public void AddNotification(string appName, string title, string body)
    {
        if (_settingsManager.Settings.NotificationsPaused)
            return;

        appName = appName?.Trim() ?? string.Empty;
        title = title?.Trim() ?? string.Empty;
        body = body?.Trim() ?? string.Empty;

        // Skip if both title and body are empty
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            return;

        // Track seen app names (RAM only for settings UI)
        if (!string.IsNullOrWhiteSpace(appName))
            SeenAppNames.Add(appName);

        // Check quiet hours
        if (IsInQuietHours())
            return;

        // Check per-app mute
        var settings = _settingsManager.Settings;
        if (!string.IsNullOrWhiteSpace(appName) &&
            settings.MutedApps.Contains(appName, StringComparer.OrdinalIgnoreCase))
            return;

        // Check mute keywords
        if (MatchesAnyKeyword(settings.MuteKeywords, settings.MuteKeywordRegexFlags, title, body))
            return;

        // Check burst rate limiting
        if (IsBurstLimited())
            return;

        // Deduplicate: skip if an identical notification arrived within the window
        if (settings.DeduplicationEnabled)
        {
            var deduplicateWindow = TimeSpan.FromSeconds(settings.DeduplicationWindowSeconds);
            foreach (var existing in _visibleNotifications)
            {
                if (existing.IsDuplicateOf(appName, title, body, deduplicateWindow))
                    return;
            }
        }

        // Track for burst rate (clean up stale entries to prevent unbounded growth)
        var burstWindow = TimeSpan.FromSeconds(Math.Max(10, _settingsManager.Settings.BurstLimitWindowSeconds));
        var burstCutoff = DateTime.Now - burstWindow;
        _recentNotificationTimes.RemoveAll(t => t < burstCutoff);
        _recentNotificationTimes.Add(DateTime.Now);

        int maxVisible = Math.Max(1, settings.MaxVisibleNotifications);

        // Replace mode: clear all current notifications so the new one
        // takes their place instead of stacking or going to overflow.
        if (settings.ReplaceMode && _visibleNotifications.Count > 0)
        {
            foreach (var old in _visibleNotifications.ToList())
            {
                if (_expiryTimers.TryGetValue(old, out var t)) { t.Stop(); _expiryTimers.Remove(old); }
                _visibleNotifications.Remove(old);
            }
            OverflowCount = 0;
        }

        if (_visibleNotifications.Count >= maxVisible)
        {
            OverflowCount++;
            return;
        }

        var item = new NotificationItem(appName, title, body);

        // Check highlight keywords — find the first match and use its per-keyword color (falls back to global)
        var highlightColor = FindMatchingKeywordColor(settings.HighlightKeywords, settings.HighlightKeywordRegexFlags, settings.PerKeywordColors, settings.HighlightColor, title, body);
        if (highlightColor != null)
        {
            item.IsHighlighted = true;
            item.HighlightColor = highlightColor;
        }

        if (settings.NewestOnTop)
            _visibleNotifications.Insert(0, item);
        else
            _visibleNotifications.Add(item);
        StartExpiryTimer(item);

        // Add to session archive (RAM only, never persisted)
        if (settings.SessionArchiveEnabled)
        {
            var maxArchive = Math.Clamp(settings.SessionArchiveMaxItems, 10, 1000);
            SessionArchive.Add(new ArchiveEntry(appName, title, body, item.ReceivedAt));
            while (SessionArchive.Count > maxArchive)
                SessionArchive.RemoveAt(0);
        }

        NotificationAdded?.Invoke(appName);
        NotificationDisplayed?.Invoke(item);
    }

    /// <summary>
    /// Estimate the number of text lines in the notification for auto-duration.
    /// </summary>
    internal static int EstimateLineCount(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return 1;
        var newlines = body.Count(c => c == '\n') + 1;
        var wrapEstimate = (body.Length + 49) / 50; // ~50 chars per line
        return Math.Max(newlines, wrapEstimate);
    }

    private void StartExpiryTimer(NotificationItem item)
    {
        var settings = _settingsManager.Settings;

        // Persistent mode: no expiry timer at all
        if (settings.PersistentNotifications)
            return;

        // Calculate effective duration
        double durationSeconds = settings.NotificationDuration;
        if (settings.AutoDurationEnabled)
        {
            var lines = EstimateLineCount(item.Body);
            durationSeconds = Math.Max(durationSeconds,
                settings.AutoDurationBaseSeconds + (lines * settings.AutoDurationSecondsPerLine));
        }

        var duration = TimeSpan.FromSeconds(durationSeconds);
        var animDuration = settings.AnimationsEnabled
            ? TimeSpan.FromMilliseconds(settings.AnimationDurationMs)
            : TimeSpan.Zero;

        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (_, _) =>
        {
            timer.Stop();

            if (animDuration <= TimeSpan.Zero)
            {
                RemoveNotification(item);
                return;
            }

            // Trigger fade-out animation
            item.IsExpiring = true;

            // After animation completes, remove from memory
            var removeTimer = new DispatcherTimer { Interval = animDuration };
            removeTimer.Tick += (_, _) =>
            {
                removeTimer.Stop();
                RemoveNotification(item);
            };
            removeTimer.Start();
        };

        _expiryTimers[item] = timer;
        timer.Start();
    }

    private void RemoveNotification(NotificationItem item)
    {
        _visibleNotifications.Remove(item);
        _expiryTimers.Remove(item);
        ResetOverflowSummaryIfQueueCleared();
    }

    public void DismissNotification(NotificationItem item)
    {
        if (_expiryTimers.TryGetValue(item, out var timer))
        {
            timer.Stop();
            _expiryTimers.Remove(item);
        }

        _visibleNotifications.Remove(item);
        ResetOverflowSummaryIfQueueCleared();
    }

    public void PauseAllTimers()
    {
        foreach (var timer in _expiryTimers.Values)
            timer.Stop();
    }

    public void ResumeAllTimers()
    {
        foreach (var timer in _expiryTimers.Values)
            timer.Start();
    }

    public void ClearAll()
    {
        foreach (var timer in _expiryTimers.Values)
            timer.Stop();
        _expiryTimers.Clear();
        _visibleNotifications.Clear();
        OverflowCount = 0;
    }

    public void Pause()
    {
        if (_settingsManager.Settings.NotificationsPaused)
            return;

        var updated = _settingsManager.Settings.Clone();
        updated.NotificationsPaused = true;
        _settingsManager.Apply(updated);
    }

    public void Resume()
    {
        if (!_settingsManager.Settings.NotificationsPaused)
            return;

        var updated = _settingsManager.Settings.Clone();
        updated.NotificationsPaused = false;
        _settingsManager.Apply(updated);
    }

    public bool IsPaused => _settingsManager.Settings.NotificationsPaused;

    public void MuteApp(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName)) return;
        if (_settingsManager.Settings.MutedApps.Contains(appName, StringComparer.OrdinalIgnoreCase))
            return;

        var updated = _settingsManager.Settings.Clone();
        updated.MutedApps.Add(appName);
        _settingsManager.Apply(updated);
    }

    public void UnmuteApp(string appName)
    {
        var updated = _settingsManager.Settings.Clone();
        var idx = updated.MutedApps.FindIndex(a => string.Equals(a, appName, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            updated.MutedApps.RemoveAt(idx);
            _settingsManager.Apply(updated);
        }
    }

    public bool IsAppMuted(string appName)
    {
        return _settingsManager.Settings.MutedApps.Contains(appName, StringComparer.OrdinalIgnoreCase);
    }

    private bool IsInQuietHours()
    {
        if (!_settingsManager.Settings.QuietHoursEnabled) return false;
        if (!TimeSpan.TryParse(_settingsManager.Settings.QuietHoursStart, out var start)) return false;
        if (!TimeSpan.TryParse(_settingsManager.Settings.QuietHoursEnd, out var end)) return false;

        var now = DateTime.Now.TimeOfDay;
        return start <= end
            ? now >= start && now < end
            : now >= start || now < end;
    }

    private bool IsBurstLimited()
    {
        if (!_settingsManager.Settings.BurstLimitEnabled) return false;
        var window = TimeSpan.FromSeconds(_settingsManager.Settings.BurstLimitWindowSeconds);
        var cutoff = DateTime.Now - window;
        _recentNotificationTimes.RemoveAll(t => t < cutoff);
        return _recentNotificationTimes.Count >= _settingsManager.Settings.BurstLimitCount;
    }

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static bool MatchesAnyKeyword(List<string> keywords, Dictionary<string, bool> regexFlags, string title, string body)
    {
        if (keywords.Count == 0) return false;
        var combined = $"{title} {body}";
        foreach (var kw in keywords)
        {
            if (string.IsNullOrWhiteSpace(kw)) continue;
            var isRegex = regexFlags.TryGetValue(kw, out var flag) && flag;
            var pattern = isRegex ? kw : @"\b" + Regex.Escape(kw) + @"\b";
            try
            {
                if (Regex.IsMatch(combined, pattern, RegexOptions.IgnoreCase, RegexTimeout))
                    return true;
            }
            catch (RegexMatchTimeoutException)
            {
                // Skip this keyword to avoid UI hang
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern — skip silently
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the effective highlight color for the first matched keyword, or null if no keyword matches.
    /// Per-keyword color is used when set; otherwise falls back to the global highlight color.
    /// </summary>
    private static string? FindMatchingKeywordColor(
        List<string> keywords,
        Dictionary<string, bool> regexFlags,
        Dictionary<string, string> perKeywordColors,
        string globalColor,
        string title,
        string body)
    {
        if (keywords.Count == 0) return null;
        var combined = $"{title} {body}";
        foreach (var kw in keywords)
        {
            if (string.IsNullOrWhiteSpace(kw)) continue;

            var isRegex = regexFlags.TryGetValue(kw, out var flag) && flag;
            var pattern = isRegex ? kw : @"\b" + Regex.Escape(kw) + @"\b";
            try
            {
                if (Regex.IsMatch(combined, pattern, RegexOptions.IgnoreCase, RegexTimeout))
                    return perKeywordColors.TryGetValue(kw, out var kwColor) ? kwColor : globalColor;
            }
            catch (RegexMatchTimeoutException) { }
            catch (ArgumentException) { }
        }
        return null;
    }

    private void OnSettingsChanged()
    {
        ReorderByConfiguredDirection();

        var maxVisible = Math.Max(1, _settingsManager.Settings.MaxVisibleNotifications);
        while (_visibleNotifications.Count > maxVisible)
        {
            var oldestIndex = _settingsManager.Settings.NewestOnTop
                ? _visibleNotifications.Count - 1
                : 0;
            var oldest = _visibleNotifications[oldestIndex];
            if (_expiryTimers.TryGetValue(oldest, out var timer))
            {
                timer.Stop();
                _expiryTimers.Remove(oldest);
            }

            _visibleNotifications.RemoveAt(oldestIndex);
        }

        ResetOverflowSummaryIfQueueCleared();
    }

    private void ReorderByConfiguredDirection()
    {
        if (_visibleNotifications.Count < 2)
            return;

        var newestOnTop = _settingsManager.Settings.NewestOnTop;
        var currentlyNewestOnTop = _visibleNotifications[0].ReceivedAt >= _visibleNotifications[^1].ReceivedAt;
        if (newestOnTop == currentlyNewestOnTop)
            return;

        var reordered = newestOnTop
            ? _visibleNotifications.OrderByDescending(n => n.ReceivedAt).ToList()
            : _visibleNotifications.OrderBy(n => n.ReceivedAt).ToList();

        _visibleNotifications.Clear();
        foreach (var notification in reordered)
            _visibleNotifications.Add(notification);
    }

    private void ResetOverflowSummaryIfQueueCleared()
    {
        if (_visibleNotifications.Count == 0 && OverflowCount > 0)
            OverflowCount = 0;
    }
}
