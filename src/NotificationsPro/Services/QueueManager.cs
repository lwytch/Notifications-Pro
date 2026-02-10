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

    private static readonly TimeSpan DeduplicateWindow = TimeSpan.FromSeconds(2);

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

        // Deduplicate: skip if an identical notification arrived within the window
        foreach (var existing in _visibleNotifications)
        {
            if (existing.IsDuplicateOf(appName, title, body, DeduplicateWindow))
                return;
        }

        int maxVisible = Math.Max(1, _settingsManager.Settings.MaxVisibleNotifications);

        if (_visibleNotifications.Count >= maxVisible)
        {
            // Over capacity — increment overflow count, discard content immediately
            OverflowCount++;
            return;
        }

        var item = new NotificationItem(appName, title, body);
        if (_settingsManager.Settings.NewestOnTop)
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
