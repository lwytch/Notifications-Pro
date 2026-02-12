using System.Collections.ObjectModel;
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
        if (MatchesAnyKeyword(settings.MuteKeywords, title, body))
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

        // Track for burst rate
        _recentNotificationTimes.Add(DateTime.Now);

        int maxVisible = Math.Max(1, settings.MaxVisibleNotifications);

        if (_visibleNotifications.Count >= maxVisible)
        {
            OverflowCount++;
            return;
        }

        var item = new NotificationItem(appName, title, body);

        // Check highlight keywords
        if (MatchesAnyKeyword(settings.HighlightKeywords, title, body))
            item.IsHighlighted = true;

        if (settings.NewestOnTop)
            _visibleNotifications.Insert(0, item);
        else
            _visibleNotifications.Add(item);
        StartExpiryTimer(item);
    }

    private void StartExpiryTimer(NotificationItem item)
    {
        var duration = TimeSpan.FromSeconds(_settingsManager.Settings.NotificationDuration);
        var animDuration = _settingsManager.Settings.AnimationsEnabled
            ? TimeSpan.FromMilliseconds(_settingsManager.Settings.AnimationDurationMs)
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

        if (OverflowCount > 0)
            OverflowCount--;
    }

    public void DismissNotification(NotificationItem item)
    {
        if (_expiryTimers.TryGetValue(item, out var timer))
        {
            timer.Stop();
            _expiryTimers.Remove(item);
        }

        _visibleNotifications.Remove(item);

        if (OverflowCount > 0)
            OverflowCount--;
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

    public void Pause() => _settingsManager.Settings.NotificationsPaused = true;

    public void Resume() => _settingsManager.Settings.NotificationsPaused = false;

    public bool IsPaused => _settingsManager.Settings.NotificationsPaused;

    public void MuteApp(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName)) return;
        var list = _settingsManager.Settings.MutedApps;
        if (!list.Contains(appName, StringComparer.OrdinalIgnoreCase))
        {
            list.Add(appName);
            _settingsManager.Save();
        }
    }

    public void UnmuteApp(string appName)
    {
        var list = _settingsManager.Settings.MutedApps;
        var idx = list.FindIndex(a => string.Equals(a, appName, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            list.RemoveAt(idx);
            _settingsManager.Save();
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

    private static bool MatchesAnyKeyword(List<string> keywords, string title, string body)
    {
        if (keywords.Count == 0) return false;
        var combined = $"{title} {body}";
        foreach (var kw in keywords)
        {
            if (!string.IsNullOrWhiteSpace(kw) &&
                combined.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
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
}
