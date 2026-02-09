using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Threading;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace NotificationsPro.Services;

/// <summary>
/// Captures Windows toast notifications and forwards title + body text to QueueManager.
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
    private bool _usingAccessibility;

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

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

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
            var texts = textElements.Select(t => t.Text).ToList();
            if (texts.Count == 0) return;

            var title = texts[0] ?? string.Empty;
            var body = texts.Count > 1
                ? string.Join("\n", texts.Skip(1).Where(t => !string.IsNullOrEmpty(t)))
                : string.Empty;

            _queueManager.AddNotification(title, body);
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
            EVENT_OBJECT_SHOW, EVENT_OBJECT_SHOW,
            IntPtr.Zero, _winEventDelegate,
            0, 0, // all processes, all threads
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

        if (_winEventHook != IntPtr.Zero)
        {
            StatusMessage = "Listening via accessibility mode";
        }
        else
        {
            StatusMessage = "Failed to start accessibility listener";
        }
        StatusChanged?.Invoke();
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (!_isRunning || hwnd == IntPtr.Zero) return;

        // Only care about window-level show events (idObject == 0 == OBJID_WINDOW)
        if (idObject != 0) return;

        // Quick class name check — fast Win32 call, no COM overhead
        var sb = new StringBuilder(256);
        GetClassName(hwnd, sb, sb.Capacity);
        var className = sb.ToString();

        // Toast notifications use CoreWindow on Windows 10/11
        if (className != "Windows.UI.Core.CoreWindow") return;

        // Check window size — toasts are small, full UWP apps are large
        if (!GetWindowRect(hwnd, out var rect)) return;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width > 600 || height > 500 || width < 50 || height < 30) return;

        // Likely a toast — extract text on a background thread to avoid blocking UI
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

            if (textElements.Count < 2) return; // Need at least app name + title

            var texts = new List<string>();
            foreach (AutomationElement textEl in textElements)
            {
                var text = textEl.Current.Name;
                if (!string.IsNullOrEmpty(text))
                    texts.Add(text);
            }

            if (texts.Count < 2) return;

            // Typical structure: [0]=app name, [1]=title, [2+]=body
            var title = texts[1];
            var body = texts.Count > 2
                ? string.Join("\n", texts.Skip(2))
                : string.Empty;

            _dispatcher.InvokeAsync(() =>
            {
                _queueManager.AddNotification(title, body);
                _capturedCount++;
                StatusMessage = $"Accessibility mode — {_capturedCount} captured";
                StatusChanged?.Invoke();
            });
        }
        catch
        {
            // Automation element may have been disposed — skip
        }
    }

    // ========================
    //  Shared
    // ========================

    public async Task RetryAccessAsync()
    {
        Stop();
        _pollCount = 0;
        _capturedCount = 0;
        await InitializeAsync();
    }

    public void Stop()
    {
        _isRunning = false;
        _pollTimer?.Stop();
        _pollTimer = null;

        if (_listener != null)
            _listener.NotificationChanged -= OnNotificationChanged;

        if (_winEventHook != IntPtr.Zero)
        {
            UnhookWinEvent(_winEventHook);
            _winEventHook = IntPtr.Zero;
        }

        _seenIds.Clear();
    }
}
