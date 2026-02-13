using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace NotificationsPro.Services;

/// <summary>
/// Registers system-wide global hotkeys via Win32 RegisterHotKey.
/// </summary>
public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int HOTKEY_TOGGLE_OVERLAY = 9001;
    private const int HOTKEY_DISMISS_ALL = 9002;
    private const int HOTKEY_TOGGLE_DND = 9003;

    private IntPtr _hwnd;
    private HwndSource? _hwndSource;
    private bool _registered;

    public event Action? ToggleOverlayRequested;
    public event Action? DismissAllRequested;
    public event Action? ToggleDndRequested;

    public string? RegistrationError { get; private set; }

    public void Register(IntPtr hwnd, string hotkeyToggle, string hotkeyDismiss, string hotkeyDnd)
    {
        Unregister();
        _hwnd = hwnd;

        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource?.AddHook(WndProc);

        var errors = new List<string>();

        if (TryParseHotkey(hotkeyToggle, out var mod1, out var vk1))
        {
            if (!RegisterHotKey(hwnd, HOTKEY_TOGGLE_OVERLAY, mod1 | MOD_NOREPEAT, vk1))
                errors.Add($"Toggle Overlay ({hotkeyToggle})");
        }

        if (TryParseHotkey(hotkeyDismiss, out var mod2, out var vk2))
        {
            if (!RegisterHotKey(hwnd, HOTKEY_DISMISS_ALL, mod2 | MOD_NOREPEAT, vk2))
                errors.Add($"Dismiss All ({hotkeyDismiss})");
        }

        if (TryParseHotkey(hotkeyDnd, out var mod3, out var vk3))
        {
            if (!RegisterHotKey(hwnd, HOTKEY_TOGGLE_DND, mod3 | MOD_NOREPEAT, vk3))
                errors.Add($"Toggle DND ({hotkeyDnd})");
        }

        RegistrationError = errors.Count > 0
            ? $"Failed to register: {string.Join(", ", errors)}"
            : null;

        _registered = true;
    }

    public void Unregister()
    {
        if (!_registered || _hwnd == IntPtr.Zero) return;

        UnregisterHotKey(_hwnd, HOTKEY_TOGGLE_OVERLAY);
        UnregisterHotKey(_hwnd, HOTKEY_DISMISS_ALL);
        UnregisterHotKey(_hwnd, HOTKEY_TOGGLE_DND);

        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;
        _registered = false;
    }

    public void Dispose() => Unregister();

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY) return IntPtr.Zero;

        var id = wParam.ToInt32();
        switch (id)
        {
            case HOTKEY_TOGGLE_OVERLAY:
                ToggleOverlayRequested?.Invoke();
                handled = true;
                break;
            case HOTKEY_DISMISS_ALL:
                DismissAllRequested?.Invoke();
                handled = true;
                break;
            case HOTKEY_TOGGLE_DND:
                ToggleDndRequested?.Invoke();
                handled = true;
                break;
        }

        return IntPtr.Zero;
    }

    internal static bool TryParseHotkey(string combo, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        if (string.IsNullOrWhiteSpace(combo)) return false;

        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= MOD_CONTROL;
                    break;
                case "ALT":
                    modifiers |= MOD_ALT;
                    break;
                case "SHIFT":
                    modifiers |= MOD_SHIFT;
                    break;
                default:
                    return false;
            }
        }

        var keyPart = parts[^1].ToUpperInvariant();

        // Try single letter/digit
        if (keyPart.Length == 1)
        {
            var c = keyPart[0];
            if (c >= 'A' && c <= 'Z')
            {
                vk = (uint)c; // VK_A = 0x41 = 'A'
                return true;
            }
            if (c >= '0' && c <= '9')
            {
                vk = (uint)c; // VK_0 = 0x30 = '0'
                return true;
            }
        }

        // Try function keys F1-F12
        if (keyPart.StartsWith("F") && int.TryParse(keyPart[1..], out var fNum) && fNum >= 1 && fNum <= 12)
        {
            vk = (uint)(0x70 + fNum - 1); // VK_F1 = 0x70
            return true;
        }

        return false;
    }
}
