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

    // Shared state
    private bool _isRunning;
    private int _pollCount;
    private int _capturedCount;

    public bool IsAccessGranted { get; private set; }
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
    private static readonly TimeSpan AccessibilityDebounceWindow = TimeSpan.FromMilliseconds(350);

    public NotificationListener(QueueManager queueManager, Dispatcher dispatcher)
    {
        _queueManager = queueManager;
        _dispatcher = dispatcher;
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
            StatusMessage = $"Seed failed ({ex.GetType().Name}): {ex.Message}";
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
            StatusMessage = $"Poll #{_pollCount} error: {ex.GetType().Name}: {ex.Message}";
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
            if (notification != null) ExtractAndForwardWinRT(notification);
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
            // Brief delay for the notification to fully render
            Thread.Sleep(300);

            var element = AutomationElement.FromHandle(hwnd);
            if (element == null) return;

            // Find all text elements in the toast
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
            texts = texts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var fields = BuildNotificationFields(texts, appName: string.Empty, assumeLeadingAppNameWhenUnknown: true);

            _dispatcher.InvokeAsync(() =>
            {
                _queueManager.AddNotification(fields.AppName, fields.Title, fields.Body);
                _capturedCount++;
                UpdateAccessibilityStatus();
            });
        }
        catch
        {
            // Automation element may have been disposed — skip
        }
    }

    private static bool IsShellHostWindow(IntPtr hwnd)
    {
        _ = GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0) return false;

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName.Equals("ShellExperienceHost", StringComparison.OrdinalIgnoreCase)
                || process.ProcessName.Equals("StartMenuExperienceHost", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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
        lock (_accessibilityGate)
        {
            _recentAccessibilityCandidates.Clear();
        }
        _usingAccessibility = false;
    }
}
