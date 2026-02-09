using System.Collections.ObjectModel;
using System.Windows.Threading;
using NotificationsPro.Models;
using NotificationsPro.ViewModels;

namespace NotificationsPro.Services;

/// <summary>
/// Manages the in-memory notification queue.
/// - Max 3 visible notifications at a time.
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
    }

    public void AddNotification(string title, string body)
    {
        if (_settingsManager.Settings.NotificationsPaused)
            return;

        // Skip if both title and body are empty
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            return;

        // Deduplicate: skip if an identical notification arrived within the window
        foreach (var existing in _visibleNotifications)
        {
            if (existing.IsDuplicateOf(title, body, DeduplicateWindow))
                return;
        }

        int maxVisible = _settingsManager.Settings.MaxVisibleNotifications;

        if (_visibleNotifications.Count >= maxVisible)
        {
            // Over capacity — increment overflow count, discard content immediately
            OverflowCount++;
            return;
        }

        var item = new NotificationItem(title, body);
        // Insert at position 0 so newest appears at the top
        _visibleNotifications.Insert(0, item);
        StartExpiryTimer(item);
    }

    private void StartExpiryTimer(NotificationItem item)
    {
        var duration = TimeSpan.FromSeconds(_settingsManager.Settings.NotificationDuration);
        var animDuration = TimeSpan.FromMilliseconds(_settingsManager.Settings.AnimationDurationMs);

        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
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
}
