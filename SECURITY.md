# Security Policy

## Privacy Guarantee

Notifications Pro is designed with a strict **zero-persistence privacy model**:

- **Notification content (title, body, app name) is never written to disk** — not to files, databases, registry, cache, logs, or telemetry.
- Notification text exists **only in RAM** for the duration it is displayed on screen.
- Overflow notifications store **only a count**, never content.
- The only persistent data is user settings (`%AppData%\NotificationsPro\settings.json`), custom themes, and optional user-provided icon/sound assets.

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest  | Yes       |

## Reporting a Vulnerability

If you discover a security vulnerability in Notifications Pro, please report it responsibly:

1. **Do not open a public issue** for security vulnerabilities.
2. Email the maintainer directly or use GitHub's [private vulnerability reporting](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability) feature on this repository.
3. Include a clear description of the vulnerability and steps to reproduce.
4. Allow reasonable time for a fix before public disclosure.

## Security Design

- **No external dependencies** — the app uses only .NET 8 framework libraries (WPF, WinForms, WinRT). No third-party NuGet packages in the main application.
- **No network access** — the app makes no outbound connections. No telemetry, analytics, or update checks.
- **No elevated privileges** — runs as a standard user process. Registry access is limited to `HKCU` (current user only).
- **Input validation** — imported settings files are size-limited (1 MB) and numeric values are clamped to valid ranges. Custom asset paths are validated to stay within the designated AppData directory.
- **Regex safety** — keyword matching uses timeouts to prevent ReDoS.

## Threat Model

Notifications Pro reads all Windows toast notifications via the WinRT `UserNotificationListener` API (or UI Automation fallback). This means:

- The app processes notification text from all applications on the system.
- An attacker who compromises this process could read notification text currently displayed in the overlay.
- The app does **not** store, transmit, or log notification content, limiting the window of exposure to what is currently visible on screen.
