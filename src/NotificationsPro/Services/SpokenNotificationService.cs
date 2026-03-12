using System.Collections.Specialized;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using System.Windows.Threading;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace NotificationsPro.Services;

/// <summary>
/// Speaks notifications from the live in-memory queue using Windows-installed speech voices.
/// Overflow content is never spoken because it is never retained.
/// </summary>
public sealed class SpokenNotificationService : IDisposable
{
    private readonly QueueManager _queueManager;
    private readonly SettingsManager _settingsManager;
    private readonly Dispatcher _dispatcher;
    private readonly Queue<NotificationItem> _pendingItems = new();
    private readonly MediaPlayer _mediaPlayer;
    private readonly SpokenNotificationPlaybackTracker _playbackTracker = new();

    private int _playbackGeneration;
    private bool _isSynthesizing;
    private NotificationItem? _currentItem;
    private SpeechSynthesisStream? _currentStream;
    private MediaSource? _currentSource;
    private bool _wasSpeechEnabled;
    private bool _disposed;

    public SpokenNotificationService(QueueManager queueManager, SettingsManager settingsManager, Dispatcher dispatcher)
    {
        _queueManager = queueManager;
        _settingsManager = settingsManager;
        _dispatcher = dispatcher;

        _mediaPlayer = new MediaPlayer();
        _mediaPlayer.CommandManager.IsEnabled = false;
        _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
        _mediaPlayer.MediaEnded += OnMediaEnded;
        _mediaPlayer.MediaFailed += OnMediaFailed;

        _queueManager.NotificationDisplayed += OnNotificationDisplayed;
        ((INotifyCollectionChanged)_queueManager.VisibleNotifications).CollectionChanged += OnVisibleNotificationsChanged;
        _settingsManager.SettingsChanged += OnSettingsChanged;
        _wasSpeechEnabled = ShouldUseSpeech();
    }

    public static IReadOnlyList<NarrationVoiceOption> GetInstalledVoices()
    {
        var voices = new List<NarrationVoiceOption>();

        try
        {
            var defaultVoice = SpeechSynthesizer.DefaultVoice;
            var defaultLabel = string.IsNullOrWhiteSpace(defaultVoice?.DisplayName)
                ? "System Default"
                : $"System Default ({defaultVoice.DisplayName})";

            voices.Add(new NarrationVoiceOption(string.Empty, defaultLabel, defaultVoice?.Language ?? string.Empty, isSystemDefault: true));

            foreach (var voice in SpeechSynthesizer.AllVoices
                         .OrderBy(v => v.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                var label = string.IsNullOrWhiteSpace(voice.Language)
                    ? voice.DisplayName
                    : $"{voice.DisplayName} ({voice.Language})";
                voices.Add(new NarrationVoiceOption(voice.Id, label, voice.Language));
            }
        }
        catch
        {
            voices.Add(new NarrationVoiceOption(string.Empty, "System Default", string.Empty, isSystemDefault: true));
        }

        return voices;
    }

    public static async Task PlayPreviewAsync(AppSettings settings)
    {
        using var synthesizer = new SpeechSynthesizer();
        ConfigureSynthesizer(synthesizer, settings);

        var previewText = SpokenNotificationTextFormatter.BuildText(
            "Notifications Pro",
            "Sample notification body. Build succeeded and the release package is ready.",
            DateTime.Now,
            settings.ReadNotificationsAloudMode,
            settings.TimestampDisplayMode);

        using var stream = await synthesizer.SynthesizeTextToStreamAsync(previewText);
        using var player = new MediaPlayer();
        player.CommandManager.IsEnabled = false;
        player.SystemMediaTransportControls.IsEnabled = false;
        player.Volume = Math.Clamp(settings.ReadNotificationsAloudVolume, 0.0, 1.0);

        var playbackCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        player.MediaEnded += (_, _) => playbackCompletion.TrySetResult(true);
        player.MediaFailed += (_, _) => playbackCompletion.TrySetResult(false);

        using var source = MediaSource.CreateFromStream(stream, stream.ContentType);
        player.Source = source;
        player.Play();

        await playbackCompletion.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }

    private void OnNotificationDisplayed(NotificationItem item)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed || !ShouldUseSpeech())
                return;

            if (!_queueManager.VisibleNotifications.Contains(item))
                return;

            if (!ShouldSpeakItem(item))
                return;

            if (!_playbackTracker.TryQueue(item, _currentItem))
                return;

            _pendingItems.Enqueue(item);
            TryStartNext();
        });
    }

    private void OnVisibleNotificationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;

            PrunePendingItems();

            if (_currentItem != null && !_queueManager.VisibleNotifications.Contains(_currentItem))
            {
                StopCurrentPlayback(clearPending: false);
                TryStartNext();
                return;
            }

            if (_currentItem == null && !_isSynthesizing)
                TryStartNext();
        });
    }

    private void OnSettingsChanged()
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;

            var speechEnabled = ShouldUseSpeech();
            if (!speechEnabled)
            {
                StopCurrentPlayback(clearPending: true);
                _wasSpeechEnabled = false;
                return;
            }

            _mediaPlayer.Volume = Math.Clamp(_settingsManager.Settings.ReadNotificationsAloudVolume, 0.0, 1.0);
            PrunePendingItems();

            if (_currentItem != null && !ShouldSpeakItem(_currentItem))
                StopCurrentPlayback(clearPending: false);

            EnqueueVisibleNotifications();

            if (_currentItem == null && !_isSynthesizing)
                TryStartNext();

            _wasSpeechEnabled = true;
        });
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;

            if (_currentItem != null)
                _playbackTracker.MarkSpoken(_currentItem);

            StopCurrentPlayback(clearPending: false);
            TryStartNext();
        });
    }

    private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;

            StopCurrentPlayback(clearPending: false);
            TryStartNext();
        });
    }

    private bool ShouldUseSpeech()
    {
        var settings = _settingsManager.Settings;
        return settings.ReadNotificationsAloudEnabled
            && !settings.NotificationsPaused;
    }

    private bool ShouldSpeakItem(NotificationItem item)
    {
        if (item.ReadAloudEnabledOverride.HasValue)
            return item.ReadAloudEnabledOverride.Value;

        var appName = item.AppName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(appName))
            return true;

        return !_settingsManager.Settings.SpokenMutedApps.Contains(appName, StringComparer.OrdinalIgnoreCase);
    }

    private void TryStartNext()
    {
        if (_disposed || !ShouldUseSpeech() || _currentItem != null || _isSynthesizing)
            return;

        PrunePendingItems();
        if (_pendingItems.Count == 0)
            return;

        var item = _pendingItems.Dequeue();
        _playbackTracker.MarkDequeued(item);
        if (!_queueManager.VisibleNotifications.Contains(item) || !ShouldSpeakItem(item))
        {
            TryStartNext();
            return;
        }

        StartSpeaking(item);
    }

    private async void StartSpeaking(NotificationItem item)
    {
        if (_disposed || !ShouldUseSpeech() || !_queueManager.VisibleNotifications.Contains(item) || !ShouldSpeakItem(item))
            return;

        var settings = _settingsManager.Settings;
        var spokenText = SpokenNotificationTextFormatter.BuildText(
            item.Title,
            item.Body,
            item.ReceivedAt,
            string.IsNullOrWhiteSpace(item.ReadAloudModeOverride)
                ? settings.ReadNotificationsAloudMode
                : item.ReadAloudModeOverride,
            settings.TimestampDisplayMode);

        if (string.IsNullOrWhiteSpace(spokenText))
        {
            TryStartNext();
            return;
        }

        var generation = ++_playbackGeneration;
        _currentItem = item;
        _isSynthesizing = true;

        SpeechSynthesisStream? stream = null;
        try
        {
            using var synthesizer = new SpeechSynthesizer();
            ConfigureSynthesizer(synthesizer, settings);
            stream = await synthesizer.SynthesizeTextToStreamAsync(spokenText);
        }
        catch
        {
            _currentItem = null;
            _isSynthesizing = false;
            stream?.Dispose();
            TryStartNext();
            return;
        }

        _isSynthesizing = false;

        if (_disposed
            || generation != _playbackGeneration
            || !ShouldUseSpeech()
            || _currentItem == null
            || !_queueManager.VisibleNotifications.Contains(item))
        {
            if (ReferenceEquals(_currentItem, item))
                _currentItem = null;
            stream.Dispose();
            TryStartNext();
            return;
        }

        try
        {
            _currentStream = stream;
            _currentSource = MediaSource.CreateFromStream(stream, stream.ContentType);
            _mediaPlayer.Volume = Math.Clamp(settings.ReadNotificationsAloudVolume, 0.0, 1.0);
            _mediaPlayer.Source = _currentSource;
            _mediaPlayer.Play();
        }
        catch
        {
            StopCurrentPlayback(clearPending: false);
            TryStartNext();
        }
    }

    private static void ConfigureSynthesizer(SpeechSynthesizer synthesizer, AppSettings settings)
    {
        var voiceId = settings.ReadNotificationsAloudVoiceId?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(voiceId))
        {
            var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => string.Equals(v.Id, voiceId, StringComparison.Ordinal));
            if (voice != null)
                synthesizer.Voice = voice;
        }

        synthesizer.Options.SpeakingRate = Math.Clamp(settings.ReadNotificationsAloudRate, 0.5, 6.0);
        synthesizer.Options.AudioVolume = Math.Clamp(settings.ReadNotificationsAloudVolume, 0.0, 1.0);
    }

    private void EnqueueVisibleNotifications()
    {
        foreach (var item in _queueManager.VisibleNotifications.OrderBy(notification => notification.ReceivedAt))
        {
            if (!ShouldSpeakItem(item))
                continue;

            if (!_playbackTracker.TryQueue(item, _currentItem))
                continue;

            _pendingItems.Enqueue(item);
        }
    }

    private void PrunePendingItems()
    {
        _playbackTracker.Prune(_queueManager.VisibleNotifications);

        if (_pendingItems.Count == 0)
            return;

        var remaining = _pendingItems
            .Where(item => _queueManager.VisibleNotifications.Contains(item) && ShouldSpeakItem(item))
            .ToArray();
        _pendingItems.Clear();

        foreach (var item in remaining)
            _pendingItems.Enqueue(item);
    }

    private void StopCurrentPlayback(bool clearPending)
    {
        _playbackGeneration++;

        try { _mediaPlayer.Pause(); } catch { }
        try { _mediaPlayer.Source = null; } catch { }

        _currentSource?.Dispose();
        _currentSource = null;

        _currentStream?.Dispose();
        _currentStream = null;

        _currentItem = null;
        _isSynthesizing = false;

        if (clearPending)
        {
            foreach (var item in _pendingItems)
                _playbackTracker.MarkDequeued(item);
            _pendingItems.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _queueManager.NotificationDisplayed -= OnNotificationDisplayed;
        ((INotifyCollectionChanged)_queueManager.VisibleNotifications).CollectionChanged -= OnVisibleNotificationsChanged;
        _settingsManager.SettingsChanged -= OnSettingsChanged;
        _mediaPlayer.MediaEnded -= OnMediaEnded;
        _mediaPlayer.MediaFailed -= OnMediaFailed;

        StopCurrentPlayback(clearPending: true);
        _mediaPlayer.Dispose();
    }
}
