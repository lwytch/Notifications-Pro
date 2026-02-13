using System.Diagnostics;
using System.Runtime.InteropServices;
using WinForms = System.Windows.Forms;

namespace NotificationsPro.Helpers;

/// <summary>
/// Detects whether a fullscreen application is currently running in the foreground.
/// Used by Presentation Mode to auto-enable DND.
/// </summary>
public static class FullscreenHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    /// <summary>
    /// Returns the process name of the foreground app if it is running fullscreen,
    /// or null if no fullscreen app is detected.
    /// </summary>
    public static string? GetFullscreenAppName()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            if (!GetWindowRect(hwnd, out var rect)) return null;

            // Check if the window covers an entire screen
            var windowWidth = rect.Right - rect.Left;
            var windowHeight = rect.Bottom - rect.Top;

            var isFullscreen = false;
            foreach (var screen in WinForms.Screen.AllScreens)
            {
                if (windowWidth >= screen.Bounds.Width && windowHeight >= screen.Bounds.Height
                    && rect.Left <= screen.Bounds.Left + 1 && rect.Top <= screen.Bounds.Top + 1)
                {
                    isFullscreen = true;
                    break;
                }
            }

            if (!isFullscreen) return null;

            GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == 0) return null;

            var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the foreground fullscreen app matches any of the configured presentation app names.
    /// </summary>
    public static bool IsPresentationAppFullscreen(IEnumerable<string> presentationApps)
    {
        var foregroundApp = GetFullscreenAppName();
        if (foregroundApp == null) return false;

        foreach (var app in presentationApps)
        {
            if (string.IsNullOrWhiteSpace(app)) continue;
            if (foregroundApp.Contains(app.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
