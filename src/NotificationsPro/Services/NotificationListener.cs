using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Threading;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace NotificationsPro.Services;

/// <summary>
/// Captures Windows toast notifications and forwards app/title/body text to QueueManager.
///
/// Strategy:
///   1. Try the WinRT UserNotificationListener API (works for packaged/privileged apps)
///   2. If that fails, fall back to accessibility mode: SetWinEventHook detects new
///      toast windows, then UI Automation reads the text from them
///
/// Privacy: only notification IDs (system uint) or transient automation elements are
/// accessed. No notification content is stored beyond QueueManager's in-memory queue.
/// </summary>
public class NotificationListener
{
    private readonly QueueManager _queueManager;
    private readonly Dispatcher _dispatcher;
    private readonly SettingsManager _settingsManager;

    // WinRT API
    private UserNotificationListener? _listener;
    private readonly HashSet<uint> _seenIds = new();
    private DispatcherTimer? _pollTimer;
    private bool _isPolling;

    // Accessibility fallback
    private IntPtr _winEventHook;
    private WinEventDelegate? _winEventDelegate; // prevent GC collection
    private DispatcherTimer? _accessibilityStatusTimer;
    private readonly object _accessibilityGate = new();
    private readonly Dictionary<IntPtr, DateTime> _recentAccessibilityCandidates = new();
    private bool _usingAccessibility;
    private int _accessibilityEventCount;
    private int _accessibilityCandidateCount;

    // WinRT auto-retry
    private DispatcherTimer? _winrtRetryTimer;
    private int _winrtRetryCount;

    // Shell host process ID cache (lock-free — only accessed from dispatcher thread)
    // Avoids expensive Process.GetProcessById on every accessibility event
    private readonly Dictionary<uint, (bool IsShellHost, DateTime CachedAt)> _shellHostCache = new();
    private static readonly TimeSpan ShellHostCacheTtl = TimeSpan.FromSeconds(60);

    // Shared state
    private bool _isRunning;
    private int _pollCount;
    private int _capturedCount;

    public bool IsAccessGranted { get; private set; }
    public string ListenerMode => _usingAccessibility ? "Accessibility" : (IsAccessGranted ? "WinRT" : "Initializing");
    public string StatusMessage { get; private set; } = "Not initialized";
    public event Action? StatusChanged;

    // --- Win32 imports for accessibility fallback ---

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
    private static readonly TimeSpan AccessibilityDebounceWindow = TimeSpan.FromMilliseconds(150);
    private static readonly string[] KnownBrowserHostNames =
    {
        "Google Chrome",
        "Chrome",
        "Microsoft Edge",
        "Edge",
        "Mozilla Firefox",
        "Firefox",
        "Brave",
        "Brave Browser",
        "Opera",
        "Vivaldi"
    };

    public NotificationListener(QueueManager queueManager, Dispatcher dispatcher, SettingsManager settingsManager)
    {
        _queueManager = queueManager;
        _dispatcher = dispatcher;
        _settingsManager = settingsManager;
    }

    // ========================
    //  Initialization
    // ========================

    public async Task<bool> InitializeAsync()
    {
        // Try the WinRT API first
        try
        {
            _listener = UserNotificationListener.Current;
            var accessStatus = await _listener.RequestAccessAsync();

            if (accessStatus == UserNotificationListenerAccessStatus.Allowed)
            {
                IsAccessGranted = true;
                StatusMessage = "Access granted — starting WinRT listener";
                StatusChanged?.Invoke();
                StartWinRTListening();
                return true;
            }

            // Access denied or pending — fall through to accessibility
            StatusMessage = $"WinRT access: {accessStatus} — switching to accessibility mode";
        }
        catch (Exception ex)
        {
            StatusMessage = $"WinRT unavailable ({ex.GetType().Name}) — switching to accessibility mode";
        }

        StatusChanged?.Invoke();

        // Fall back to accessibility-based capture
        StartAccessibilityCapture();

        // Do NOT auto-retry WinRT — RequestAccessAsync can report "Allowed"
        // for unpackaged desktop apps even though the WinRT listener never
        // delivers notifications. Auto-upgrading would kill the working
        // accessibility hook and replace it with a broken WinRT path.
        // Users can manually retry via the tray menu if needed.

        return _usingAccessibility;
    }

    // ========================
    //  WinRT API approach
    // ========================

    private void StartWinRTListening()
    {
        if (_listener == null || _isRunning) return;
        _isRunning = true;

        _listener.NotificationChanged += OnNotificationChanged;
        _ = SeedAndStartPollingAsync();
    }

    private async Task SeedAndStartPollingAsync()
    {
        if (_listener == null) return;

        try
        {
            var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            foreach (var notification in notifications)
                _seenIds.Add(notification.Id);

            StatusMessage = $"Seeded {notifications.Count} — polling started";
            StatusChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Seed failed ({ex.GetType().Name})";
            StatusChanged?.Invoke();
        }

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += async (_, _) => await PollForNewNotificationsAsync();
        _pollTimer.Start();
    }

    private void OnNotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        if (args.ChangeKind != UserNotificationChangedKind.Added) return;
        _dispatcher.InvokeAsync(async () => await ProcessNewNotificationAsync(args.UserNotificationId));
    }

    private async Task PollForNewNotificationsAsync()
    {
        if (_listener == null || !_isRunning || _isPolling) return;
        _isPolling = true;

        try
        {
            _pollCount++;
            var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);

            foreach (var notification in notifications)
            {
                if (!_seenIds.Contains(notification.Id))
                {
                    _seenIds.Add(notification.Id);
                    _capturedCount++;
                    ExtractAndForwardWinRT(notification);
                }
            }

            StatusMessage = $"WinRT — {_capturedCount} captured, {notifications.Count} in system (poll #{_pollCount})";
            StatusChanged?.Invoke();

            if (_seenIds.Count > 5000)
                TrimSeenIds();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Poll #{_pollCount} error: {ex.GetType().Name}";
            StatusChanged?.Invoke();
        }
        finally
        {
            _isPolling = false;
        }
    }

    private async Task ProcessNewNotificationAsync(uint notificationId)
    {
        if (_seenIds.Contains(notificationId)) return;
        _seenIds.Add(notificationId);
        if (_listener == null) return;

        try
        {
            var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                _capturedCount++;
                ExtractAndForwardWinRT(notification);
            }
        }
        catch { }
    }

    private void ExtractAndForwardWinRT(UserNotification notification)
    {
        try
        {
            var toastBinding = notification.Notification.Visual.GetBinding(
                KnownNotificationBindings.ToastGeneric);
            if (toastBinding == null) return;

            var textElements = toastBinding.GetTextElements();
            var texts = textElements
                .Select(t => NormalizeText(t.Text))
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var appName = NormalizeText(notification.AppInfo?.DisplayInfo?.DisplayName ?? string.Empty);
            var fields = BuildNotificationFields(texts, appName, assumeLeadingAppNameWhenUnknown: false);
            _queueManager.AddNotification(fields.AppName, fields.Title, fields.Body);

            // Suppress the Windows toast popup by removing the notification from the system.
            // Note: this also removes it from Windows notification center.
            // Only active while the app is running — no cleanup needed on exit.
            if (_settingsManager.Settings.SuppressToastPopups && _listener != null)
            {
                try { _listener.RemoveNotification(notification.Id); } catch { }
            }
        }
        catch { }
    }

    private void TrimSeenIds()
    {
        if (_seenIds.Count <= 1000) return;
        var kept = _seenIds.OrderByDescending(id => id).Take(1000).ToHashSet();
        _seenIds.Clear();
        foreach (var id in kept) _seenIds.Add(id);
    }

    // ========================
    //  Accessibility fallback
    // ========================

    private void StartAccessibilityCapture()
    {
        _usingAccessibility = true;
        _isRunning = true;

        // SetWinEventHook must be called on a thread with a message loop.
        // The WPF dispatcher thread qualifies.
        _winEventDelegate = OnWinEvent;
        _winEventHook = SetWinEventHook(
            EVENT_OBJECT_SHOW, EVENT_OBJECT_NAMECHANGE,
            IntPtr.Zero, _winEventDelegate,
            0, 0, // all processes, all threads
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

        if (_winEventHook != IntPtr.Zero)
        {
            StartAccessibilityStatusTimer();
            UpdateAccessibilityStatus("Listening via accessibility mode");
        }
        else
        {
            StatusMessage = "Failed to start accessibility listener";
            StatusChanged?.Invoke();
        }
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (!_isRunning || hwnd == IntPtr.Zero) return;
        Interlocked.Increment(ref _accessibilityEventCount);

        var classNameBuilder = new StringBuilder(256);
        if (GetClassName(hwnd, classNameBuilder, classNameBuilder.Capacity) > 0)
            _lastAccessibilityClassName = classNameBuilder.ToString();

        // Toast hosting windows live under ShellExperienceHost / StartMenuExperienceHost.
        if (!IsShellHostWindow(hwnd)) return;

        // Keep a size-based filter to avoid scanning large host windows.
        if (!GetWindowRect(hwnd, out var rect)) return;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width > 800 || height > 700 || width < 80 || height < 40) return;

        if (!ShouldProcessAccessibilityCandidate(hwnd)) return;
        Interlocked.Increment(ref _accessibilityCandidateCount);

        // Extract text on a background thread to avoid blocking UI.
        var capturedHwnd = hwnd;
        _ = Task.Run(() => ExtractToastViaAutomation(capturedHwnd));
    }

    private void ExtractToastViaAutomation(IntPtr hwnd)
    {
        try
        {
            // Wait for stacked notifications to render. 300 ms gives enough time for a second
            // simultaneous toast (e.g. Reddit + X arriving together) to appear in the same host
            // window so FindNotificationSplitPanes can separate them in one scan.
            Thread.Sleep(300);

            var element = AutomationElement.FromHandle(hwnd);
            if (element == null) return;

            // When multiple toasts are stacked in the same host window they can appear as
            // Pane/Group/ListItem containers depending on Windows build. Search these
            // container types at several depths so simultaneous notifications (e.g. Reddit + X)
            // are captured as separate cards.
            var splitContainers = FindNotificationSplitContainers(element);
            if (splitContainers.Count >= 2)
            {
                foreach (var container in splitContainers)
                    ExtractAndDispatchFromElement(container);
            }
            else
            {
                ExtractAndDispatchFromElement(element);
            }
        }
        catch
        {
            // Automation element may have been disposed — skip
        }
    }

    /// <summary>
    /// Searches for notification container boundaries at depth 1 then depth 2/3.
    /// Returns the list of split containers when ≥2 found, or an empty list when the
    /// notification appears to be a single item.
    /// </summary>
    private static List<AutomationElement> FindNotificationSplitContainers(AutomationElement root)
    {
        var containerCondition = new OrCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Group),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));

        // Depth 1 — most common case
        var depth1 = FilterContainersWithRelevantText(root.FindAll(TreeScope.Children, containerCondition));
        if (depth1.Count >= 2)
            return depth1;

        // Depth 2 — notifications nested inside a single intermediate pane
        if (depth1.Count == 1)
        {
            var depth2 = FilterContainersWithRelevantText(depth1[0].FindAll(TreeScope.Children, containerCondition));
            if (depth2.Count >= 2)
                return depth2;

            // Depth 3 — one more level of nesting (seen on some Windows 11 builds)
            if (depth2.Count == 1)
            {
                var depth3 = FilterContainersWithRelevantText(depth2[0].FindAll(TreeScope.Children, containerCondition));
                if (depth3.Count >= 2)
                    return depth3;
            }
        }

        return new List<AutomationElement>();
    }

    private static List<AutomationElement> FilterContainersWithRelevantText(AutomationElementCollection candidates)
    {
        var filtered = new List<AutomationElement>();
        foreach (AutomationElement candidate in candidates)
        {
            if (HasRelevantTextDescendants(candidate))
                filtered.Add(candidate);
        }
        return filtered;
    }

    private static bool HasRelevantTextDescendants(AutomationElement element)
    {
        try
        {
            var textCondition = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.Text);
            var textElements = element.FindAll(TreeScope.Descendants, textCondition);
            foreach (AutomationElement textEl in textElements)
            {
                var text = NormalizeText(textEl.Current.Name);
                if (!string.IsNullOrWhiteSpace(text) && !IsIgnoredUiAutomationText(text))
                    return true;
            }
        }
        catch
        {
            // Ignore stale/disposed accessibility elements.
        }

        return false;
    }

    private void ExtractAndDispatchFromElement(AutomationElement element)
    {
        var textCondition = new PropertyCondition(
            AutomationElement.ControlTypeProperty, ControlType.Text);
        var textElements = element.FindAll(TreeScope.Descendants, textCondition);

        var texts = new List<string>();
        foreach (AutomationElement textEl in textElements)
        {
            var text = NormalizeText(textEl.Current.Name);
            if (!string.IsNullOrWhiteSpace(text) && !IsIgnoredUiAutomationText(text))
                texts.Add(text);
        }

        if (texts.Count == 0) return;

        // Try to split merged browser notifications from a single accessibility host
        // (e.g., simultaneous Reddit + X toasts from Chrome).
        var splitFields = SplitCombinedBrowserToasts(texts);
        var fieldsToDispatch = new List<(string AppName, string Title, string Body)>();

        if (splitFields.Count >= 2)
        {
            fieldsToDispatch.AddRange(splitFields);
        }
        else
        {
            var distinctTexts = texts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var fields = BuildNotificationFields(distinctTexts, appName: string.Empty, assumeLeadingAppNameWhenUnknown: true);
            if (!string.IsNullOrWhiteSpace(fields.Title) || !string.IsNullOrWhiteSpace(fields.Body))
                fieldsToDispatch.Add(fields);
        }

        if (fieldsToDispatch.Count == 0)
            return;

        _dispatcher.InvokeAsync(() =>
        {
            foreach (var fields in fieldsToDispatch)
            {
                _queueManager.AddNotification(fields.AppName, fields.Title, fields.Body);
                _capturedCount++;
            }
            UpdateAccessibilityStatus();
        });
    }

    internal static List<(string AppName, string Title, string Body)> SplitCombinedBrowserToasts(IReadOnlyList<string> texts)
    {
        var parts = texts
            .Select(NormalizeText)
            .Where(t => !string.IsNullOrWhiteSpace(t) && !IsIgnoredUiAutomationText(t))
            .ToList();

        if (parts.Count < 4)
            return new();

        var marker = parts[0];
        if (string.IsNullOrWhiteSpace(marker) || !IsKnownBrowserHostName(marker))
            return new();

        var markerIndexes = new List<int>();
        for (var i = 0; i < parts.Count; i++)
        {
            if (parts[i].Equals(marker, StringComparison.OrdinalIgnoreCase))
                markerIndexes.Add(i);
        }

        if (markerIndexes.Count < 2 || markerIndexes[0] != 0)
            return new();

        // Reject invalid split points that would produce empty segments.
        for (var i = 1; i < markerIndexes.Count; i++)
        {
            if (markerIndexes[i] - markerIndexes[i - 1] < 2)
                return new();
        }

        markerIndexes.Add(parts.Count);

        var results = new List<(string AppName, string Title, string Body)>();
        for (var i = 0; i < markerIndexes.Count - 1; i++)
        {
            var start = markerIndexes[i];
            var end = markerIndexes[i + 1];
            var segment = parts.Skip(start).Take(end - start).ToList();
            if (segment.Count < 2)
                return new();
            if (!segment[0].Equals(marker, StringComparison.OrdinalIgnoreCase))
                return new();

            segment.RemoveAt(0); // remove browser app marker
            if (segment.Count == 0)
                return new();

            var fields = BuildNotificationFields(
                segment,
                appName: string.Empty,
                assumeLeadingAppNameWhenUnknown: true);
            var appName = string.IsNullOrWhiteSpace(fields.AppName) ? marker : fields.AppName;

            if (!string.IsNullOrWhiteSpace(fields.Title) || !string.IsNullOrWhiteSpace(fields.Body))
                results.Add((appName, fields.Title, fields.Body));
        }

        return results.Count >= 2 ? results : new();
    }

    private static bool IsKnownBrowserHostName(string value)
    {
        return KnownBrowserHostNames.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the window belongs to a shell host process (toast container).
    /// Uses a lock-free cache to avoid calling Process.GetProcessById on every
    /// accessibility event (hundreds/sec). Safe because OnWinEvent always runs
    /// on the dispatcher thread (WINEVENT_OUTOFCONTEXT).
    /// </summary>
    private bool IsShellHostWindow(IntPtr hwnd)
    {
        _ = GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0) return false;

        var now = DateTime.UtcNow;

        // Check cache first (no lock needed — single-threaded access)
        if (_shellHostCache.TryGetValue(processId, out var cached) &&
            now - cached.CachedAt < ShellHostCacheTtl)
        {
            return cached.IsShellHost;
        }

        bool isShellHost;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            isShellHost = process.ProcessName.Equals("ShellExperienceHost", StringComparison.OrdinalIgnoreCase)
                || process.ProcessName.Equals("StartMenuExperienceHost", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            isShellHost = false;
        }

        _shellHostCache[processId] = (isShellHost, now);

        // Periodic eviction to prevent unbounded growth
        if (_shellHostCache.Count > 100)
        {
            var staleKeys = _shellHostCache
                .Where(kvp => now - kvp.Value.CachedAt > ShellHostCacheTtl)
                .Select(kvp => kvp.Key).ToList();
            foreach (var key in staleKeys) _shellHostCache.Remove(key);
        }

        return isShellHost;
    }

    private bool ShouldProcessAccessibilityCandidate(IntPtr hwnd)
    {
        var now = DateTime.UtcNow;
        lock (_accessibilityGate)
        {
            if (_recentAccessibilityCandidates.TryGetValue(hwnd, out var lastSeen) &&
                now - lastSeen < AccessibilityDebounceWindow)
            {
                return false;
            }

            _recentAccessibilityCandidates[hwnd] = now;

            if (_recentAccessibilityCandidates.Count > 256)
            {
                var staleCutoff = now - TimeSpan.FromMinutes(1);
                var staleHandles = _recentAccessibilityCandidates
                    .Where(kvp => kvp.Value < staleCutoff)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var staleHandle in staleHandles)
                    _recentAccessibilityCandidates.Remove(staleHandle);
            }
        }

        return true;
    }

    private void StartAccessibilityStatusTimer()
    {
        _accessibilityStatusTimer?.Stop();
        _accessibilityStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _accessibilityStatusTimer.Tick += OnAccessibilityStatusTick;
        _accessibilityStatusTimer.Start();
    }

    private void OnAccessibilityStatusTick(object? sender, EventArgs e)
    {
        if (!_usingAccessibility || !_isRunning) return;
        UpdateAccessibilityStatus();
    }

    private void UpdateAccessibilityStatus(string? prefix = null)
    {
        var classHint = string.Empty;
        if (_lastAccessibilityClassName != null)
            classHint = $", last class {_lastAccessibilityClassName}";

        var suffix = $"{_capturedCount} captured, {_accessibilityCandidateCount} candidates, {_accessibilityEventCount} events{classHint}";
        StatusMessage = string.IsNullOrWhiteSpace(prefix)
            ? $"Accessibility mode — {suffix}"
            : $"{prefix} — {suffix}";
        StatusChanged?.Invoke();
    }

    private string? _lastAccessibilityClassName;

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Trim();
        return normalized;
    }

    private static bool IsIgnoredUiAutomationText(string text)
    {
        return text.Equals("Dismiss", StringComparison.OrdinalIgnoreCase)
            || text.Equals("Close", StringComparison.OrdinalIgnoreCase)
            || text.Equals("Notification", StringComparison.OrdinalIgnoreCase)
            || text.Equals("Notification center", StringComparison.OrdinalIgnoreCase)
            || text.Equals("Clear all notifications", StringComparison.OrdinalIgnoreCase)
            || text.Equals("Do not disturb", StringComparison.OrdinalIgnoreCase);
    }

    private static (string AppName, string Title, string Body) BuildNotificationFields(
        IReadOnlyList<string> texts, string appName, bool assumeLeadingAppNameWhenUnknown)
    {
        var normalizedAppName = NormalizeText(appName);
        var parts = texts
            .Select(NormalizeText)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        if (parts.Count == 0)
            return (normalizedAppName, string.Empty, string.Empty);

        if (!string.IsNullOrWhiteSpace(normalizedAppName)
            && parts.Count > 1
            && parts[0].Equals(normalizedAppName, StringComparison.OrdinalIgnoreCase))
        {
            parts.RemoveAt(0);
        }
        else if (string.IsNullOrWhiteSpace(normalizedAppName)
                 && assumeLeadingAppNameWhenUnknown
                 && parts.Count >= 3)
        {
            normalizedAppName = parts[0];
            parts.RemoveAt(0);
        }

        if (parts.Count == 0)
            return (normalizedAppName, string.Empty, string.Empty);

        var title = parts[0];
        var body = parts.Count > 1
            ? string.Join("\n", parts.Skip(1))
            : string.Empty;

        return (normalizedAppName, title, body);
    }

    // ========================
    //  Shared
    // ========================

    private void StartWinRTRetryTimer()
    {
        _winrtRetryTimer?.Stop();
        _winrtRetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _winrtRetryTimer.Tick += async (_, _) => await TryWinRTUpgradeAsync();
        _winrtRetryTimer.Start();
    }

    private async Task TryWinRTUpgradeAsync()
    {
        _winrtRetryCount++;
        try
        {
            var listener = UserNotificationListener.Current;
            var accessStatus = await listener.RequestAccessAsync();

            if (accessStatus == UserNotificationListenerAccessStatus.Allowed)
            {
                // WinRT is now available — switch from accessibility
                _winrtRetryTimer?.Stop();
                _winrtRetryTimer = null;

                // Stop accessibility capture
                if (_winEventHook != IntPtr.Zero)
                {
                    UnhookWinEvent(_winEventHook);
                    _winEventHook = IntPtr.Zero;
                }
                _accessibilityStatusTimer?.Stop();
                _usingAccessibility = false;

                // Start WinRT
                _listener = listener;
                IsAccessGranted = true;
                _isRunning = false; // Reset so StartWinRTListening proceeds
                StatusMessage = "Upgraded to WinRT listener (access granted on retry)";
                StatusChanged?.Invoke();
                StartWinRTListening();
            }
        }
        catch
        {
            // Still unavailable — keep retrying
        }
    }

    public async Task RetryAccessAsync()
    {
        Stop();
        _pollCount = 0;
        _capturedCount = 0;
        _accessibilityCandidateCount = 0;
        _accessibilityEventCount = 0;
        await InitializeAsync();
    }

    public void Stop()
    {
        _isRunning = false;
        _pollTimer?.Stop();
        _pollTimer = null;
        _winrtRetryTimer?.Stop();
        _winrtRetryTimer = null;

        if (_accessibilityStatusTimer != null)
        {
            _accessibilityStatusTimer.Tick -= OnAccessibilityStatusTick;
            _accessibilityStatusTimer.Stop();
            _accessibilityStatusTimer = null;
        }

        if (_listener != null)
            _listener.NotificationChanged -= OnNotificationChanged;

        if (_winEventHook != IntPtr.Zero)
        {
            UnhookWinEvent(_winEventHook);
            _winEventHook = IntPtr.Zero;
        }

        _seenIds.Clear();
        _shellHostCache.Clear();
        lock (_accessibilityGate)
        {
            _recentAccessibilityCandidates.Clear();
        }
        _usingAccessibility = false;
    }
}
