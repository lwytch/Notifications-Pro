<div align="center">
  <img src="src/NotificationsPro.Package/Images/Square150x150Logo.png" alt="Notifications Pro Logo" width="120" />
  
  # Notifications Pro

  **The ultimate open-source, highly-customisable Windows notification overlay.**
  
  [![Platform](https://img.shields.io/badge/Platform-Windows_10%20%7C%2011-blue.svg)]()
  [![Framework](https://img.shields.io/badge/Framework-.NET_8%20%7C%20WPF-purple.svg)]()
  [![License](https://img.shields.io/badge/License-MIT-green.svg)]()
  [![Status](https://img.shields.io/badge/Status-Active_Development-orange.svg)]()
</div>

---

A powerful Windows desktop productivity tool (C# .NET 8 + WPF) that captures native Windows toast notifications and mirrors them into a fully customisable, always-on-top overlay widget. Stop missing important messages when the 5-second default popup vanishes—read every notification in full, without ever clicking or switching apps.

> Still in active development. I use it every day. More updates coming.

## 🎯 Who is this for?

| Role | Why You Need It |
|------|-----------------|
| <img src="https://api.iconify.design/lucide:gamepad-2.svg?color=white" width="18" height="18" alt="Streamers" style="vertical-align: middle;"/> **Streamers & Creators** | Read chat and alerts on a single monitor while maintaining full-screen gaming focus. Use OBS Chroma-key integration for seamless stream overlays. |
| <img src="https://api.iconify.design/lucide:code-xml.svg?color=white" width="18" height="18" alt="Developers" style="vertical-align: middle;"/> **Developers & Engineers** | Catch CI/CD build failures, PR reviews, or server alerts while deep in focus mode. Texts aren't truncated like default Windows toasts. |
| <img src="https://api.iconify.design/lucide:brain-circuit.svg?color=white" width="18" height="18" alt="Neurodivergent" style="vertical-align: middle;"/> **ADHD & Neurodivergent** | Never lose a thought. Persistent notifications ensure you decide when a reminder disappears, not an arbitrary 5-second timer. |
| <img src="https://api.iconify.design/lucide:briefcase.svg?color=white" width="18" height="18" alt="Professionals" style="vertical-align: middle;"/> **Professionals** | Filter noise. Auto-mute Slack during meetings, but highlight "urgent" emails in red so you only break focus for emergencies. |
| <img src="https://api.iconify.design/lucide:message-circle-heart.svg?color=white" width="18" height="18" alt="Social Media" style="vertical-align: middle;"/> **Social & Community Managers** | Monitor brand mentions, DMs, and engagement across platforms like X (Twitter), Discord, and Reddit without drowning in browser tabs. |

---

## 📸 Screenshots
*(Add screenshots of the overlay and settings window here)*
![Overlay Preview](docs/img/placeholder_overlay.png)
![Settings Window](docs/img/placeholder_settings.png)

---

## Feature Overview

### Notification Capture
- **WinRT listener** (`UserNotificationListener`) — the primary capture path, requires a one-time permission grant via Windows Settings.
- **Accessibility fallback** — uses `SetWinEventHook` + UI Automation to read toast text when the WinRT path is unavailable (e.g. unpackaged app restrictions). Simultaneous notifications are split into individual entries rather than merged.
- **Polling guard** — a 2-second polling loop supplements the event-driven path for reliability; a flag prevents overlapping polls.
- **Toast suppression** (optional) — after capturing a notification, the app can remove the native Windows toast popup so only the overlay shows. Requires the WinRT path. Off by default.

### Overlay Window
- Always-on-top, transparent, borderless — sits over any application.
- **Click-through mode** — overlay is visually present but all mouse events pass through to whatever is beneath.
- **Drag anywhere** — click and drag the overlay to reposition. Right-click a notification card for a context menu (dismiss, copy text, clear all, mute app).
- **Click-to-dismiss** — left-click a card to remove it.
- **Hover-to-pause** — hovering over the overlay pauses all expiry timers so you can finish reading before cards disappear.
- **Multi-monitor support** — place the overlay on any connected display; it remembers which monitor.
- **Edge snapping** — configurable snap distance; overlay aligns to screen edges when dragged nearby.
- **Manual resize** — drag the left or right edge of the overlay to change its width. Resize anchors to the edge it is near so right-aligned overlays do not jump.
- **Fullscreen overlay mode** — expands the overlay to cover an entire monitor with a configurable semi-transparent backdrop (colour + opacity). Useful as a dedicated notification monitor or for focus sessions.
- **OBS fixed-window mode** — locks the overlay to a precise width/height for predictable window-capture in OBS/streaming tools.

### Layout Modes
- **Stacked cards** — each notification is a separate card, scrollable when many accumulate.
- **Single-line banner mode** — all text compressed to a single line per notification, with optional wrapping and a configurable max-line count.
- **Newest-on-top** toggle — controls whether new notifications appear at the top or bottom.
- **Max visible** — configurable 1–40 visible cards. Additional notifications increment an overflow counter ("+N more") without storing their content.
- **Max overlay height** — the overlay expands vertically up to this limit, then shows a scrollbar. Clamped to the active monitor work area.
- **Width / height presets** — quick buttons for 1080p / 2K / 4K / 8K display sizes.

### Appearance & Theming
- **Built-in theme presets** — select from a dropdown and apply in one click.
- **Custom themes** — save your current overlay look by name; re-apply or delete any time.
- **Import / export** — share a complete settings profile as a JSON file. Position and session state are preserved on the receiving machine.
- **Typography** — independently configure font family, size, and weight for app name, title, and body text. Line spacing control.
- **Timestamps** — optional per-card timestamps in Relative (`2m ago`), Time (`14:35`), or DateTime format, with independent size, weight, and colour.
- **Colours** — independent hex colours for title, app name, body text, background, and accent stripe.
- **Background opacity** — from fully opaque to near-transparent.
- **Card shape** — corner radius, internal padding, card gap, outer margin.
- **Accent stripe** — 3 px coloured bar on the left edge of each card.
- **Optional border** — thin border around each card, configurable colour and thickness.
- **Per-app tint** — subtle colour tint on each card based on the source app name.
- **Icons** — optional per-app icons using 10 built-in vector presets (Bell, Megaphone, Star, Warning, Info, Heart, Lightning, Fire, Chat, Checkmark) or your own image files. Icon size configurable 16–48 px.
- **Chroma key** — solid-colour background (green / blue / magenta / custom) for OBS chroma-key filtering.
- **Information density presets** — Compact / Comfortable / Spacious — adjusts padding and spacing in one click.

### Behaviour & Control
- **Notification duration** — configurable display time in seconds; each card has its own timer.
- **Persistent notifications** — disable auto-expiry; cards stay until manually dismissed.
- **Auto-duration** — extends display time based on notification length (configurable base seconds + seconds-per-line).
- **Animations** — slide-in from Left / Right / Top / Bottom or fade-only, with configurable duration.
- **Deduplication** — suppress identical notifications within a configurable time window.
- **Always-on-top** — toggle without restarting; also available from the tray menu.

### Filtering & Smart Control
- **Per-app mute** — silence notifications from specific apps. Muted apps are remembered per session; the settings UI shows every app seen this session.
- **Keyword mute** — suppress any notification whose title or body contains a keyword.
- **Keyword highlight** — flag notifications containing certain keywords with a custom highlight colour.
- **Quiet hours** — block all notifications between configurable start/end times (supports overnight ranges).
- **Burst limiting** — cap the number of notifications accepted in a sliding time window.
- **Focus mode** — a timed DND period (configurable minutes) accessible from the tray menu.
- **Presentation mode** — auto-DND when a configured app (PowerPoint, Zoom, Teams, etc.) goes fullscreen.

### Sounds
- **Master toggle** for notification sounds.
- **System sound presets** — Asterisk, Beep, Exclamation, Hand, Question.
  > Note: Windows 11 unified many system sound events, so some system sound choices may produce the same audio. For distinct sounds use a custom WAV file.
- **Custom WAV files** — browse for any `.wav` file; stored under `%AppData%\NotificationsPro\sounds\`.
- **Per-app overrides** — assign a different sound to each app.
- **Test button** — play the current default sound immediately.

### System Integration
- **Start with Windows** — adds/removes a registry entry under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Global hotkeys** — register system-wide keyboard shortcuts for: toggle overlay visibility, dismiss all notifications, toggle Do Not Disturb.
- **Settings window theming** — Dark / Light / System / any named overlay theme. Colours are fully customisable (background, surface, text, accent, border).
- **Settings popup mode** — settings window can float as a popup above the taskbar with optional auto-close.

### Accessibility
- **Accessibility mode** toggle — enables persistent notifications + system motion/contrast/text-scaling respect in one click.
- **Respect Reduce Motion** — disables slide animations when Windows "Reduce Motion" is active.
- **Respect High Contrast** — adapts overlay colours when Windows High Contrast is active.
- **Respect Text Scaling** — scales notification text with the Windows text-size accessibility setting.
- **Auto-duration** — longer notifications stay visible longer so there is time to read them.
- **Scrollable overlay** — when content exceeds the max height, a scrollbar appears so no text is lost.
- **Overlay scrollbar customisation** — show/hide scrollbar, configurable width (4–20 px) and opacity.

---

## Common Use Cases

### Streaming / OBS
Keep notifications readable on-stream without disrupting your layout:
1. Settings > Appearance — enable **Chroma Key**, pick a key colour (Green / Blue / Magenta).
2. Settings > Streaming — enable **OBS Fixed Window Mode**, set width and height to match your capture dimensions.
3. In OBS, add a **Window Capture** source for the overlay, then add a **Chroma Key** filter using the matching colour.
4. Use **Per-app icons** and **Per-app tint** so viewers can instantly identify which app each notification is from.

### Presenting / Meetings
Avoid distraction without missing urgent messages:
- Enable **Presentation Mode** — the app auto-pauses notifications when PowerPoint, Zoom, Teams, or any configured app goes fullscreen.
- Use **Quiet Hours** to block notifications during scheduled meeting blocks.
- Use **Keyword highlight** so words like "urgent" or "fire" still break through even in focus mode.

### Gaming / Fullscreen Apps
- Enable **Click-through** so the overlay never steals focus mid-game.
- Use **Per-app mute** to silence low-priority apps while keeping alerts from important ones.
- Position the overlay in a corner that does not overlap your HUD.
- Use **Single-line banner mode** with a wide overlay for minimal screen real-estate.

### Accessibility & Readability
- Enable **Accessibility Mode** for a sensible bundle of defaults (persistent cards, motion/contrast/scaling respect).
- Increase body/title font size for large monitors or vision needs.
- Use **Auto-duration** so you are never rushed to read a long notification.
- Enable **Density: Spacious** for larger tap targets and more breathing room between elements.
- Use **Timestamps** in DateTime mode to track when notifications arrived.

### Monitoring & Alerts
- Set up **Keyword highlight** for terms like `failed`, `down`, `critical`, `error`, `urgent`.
- Enable **Sounds** for specific apps (e.g., a monitoring tool) while keeping others silent.
- Use **Burst limiting** so a flood of alerts from a runaway process does not bury the overlay.
- Set **Max Visible** high and **Duration** long so alerts accumulate until acknowledged.
- Use **Persistent Notifications** for critical alerts that must not auto-expire.

### Daily Communications (Teams / Slack / Discord / Outlook)
- Long chat messages, email previews, and calendar reminders display in full — no truncation.
- Use **Per-app mute** to silence low-signal channels at certain times.
- Right-click a card to copy the full notification text to the clipboard.
- Use **Deduplication** to suppress rapid-fire duplicate messages from chatty threads.

### Social Media & Community Management (X / Reddit / Instagram)
- Track mentions, replies, and DMs without keeping heavy browser tabs open or switching contexts.
- Use **Keyword highlight** for your brand name or specific campaign hashtags so you never miss an important engagement.
- Use **Deduplication** and **Burst limiting** to filter out rapid-fire "liked your post" storms while keeping meaningful comments visible.
- Keep the overlay tucked in the corner of your screen to passively monitor community health while working on other tasks.

---

## Why No Clickable Links or Images?

This is deliberate:
- **Safety**: an always-on-top overlay with clickable URLs increases the risk of accidental clicks and phishing. URLs render as plain text only.
- **Predictability**: Windows toasts can include action buttons and rich media. The overlay is intentionally passive (read / dismiss / copy) so behaviour is consistent across both the WinRT and accessibility capture paths.
- **Privacy**: rendering images from notification payloads would require decoding potentially remote content.

Per-app icons are user-configured (built-in vector presets or your own files) and are never sourced from notification payloads.

---

## Privacy & Data Model

Notifications Pro is designed to avoid persisting notification content:
- **No notification title or body is ever written to disk** — no database, no cache, no logs of notification text.
- Notification content exists only in RAM while displayed, and is released immediately after dismissal or expiry.
- The app makes **no network calls** and includes **no telemetry**.

Windows may keep notification history in the Action Center independently. The optional toast-suppression feature removes captured notifications from the Action Center; leave it off to preserve Windows default behaviour.

### Files Written

Under `%AppData%\NotificationsPro\`:

| Path | Contents |
|------|----------|
| `settings.json` | All app preferences (no notification content) |
| `themes\*.json` | Named custom overlay themes |
| `icons\` | Optional user-provided icon image files |
| `sounds\` | Optional custom WAV files |

---

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK (to build from source) or .NET 8 Runtime (to run a published build)

## Build / Run / Test

```bash
dotnet restore
dotnet build
dotnet run --project src/NotificationsPro
dotnet test
```

## Publish (self-contained)

```bash
dotnet publish src/NotificationsPro -c Release -r win-x64 --self-contained
```

---

## Installation (MSIX)

Notifications Pro is distributed as a native Windows App package (`.msix`). Because it hooks deeply into Windows Notifications, the installer must be digitally signed. Since this is a free open-source tool, it is signed with a self-signed Developer Certificate.

To install it for the first time:
1. Download both the **`.msix`** installer and the **`.cer`** certificate file from the [Releases page].
2. Right-click the `.cer` file and select **Install Certificate**.
3. Select **Local Machine**, and explicitly browse to place it in the **"Trusted Root Certification Authorities"** store.
4. Double-click the `.msix` file to install the app natively.

> **Note on Windows Defender / SmartScreen:** 
> Because this is a new, indie open-source app targeting low-level UI Automation, Microsoft Defender or "Attack Surface Reduction" tools may flag it initially. This is a false positive completely expected for newly compiled binaries. You can safely click "More info" and "Run Anyway". If Defender continually blocks it, do not whitelist the entire WindowsApps folder; instead, add a specific exclusion for the `NotificationsPro.exe` process.
> 
> How you manage exclusions in your antivirus software is up to you. We are not liable for changes you make to your own security solutions. Please ensure you narrow down any changes specifically to this application using the method you see fit.

---

## How To Use

### Tray Icon
Right-click the tray icon to access: show/hide overlay, pause (DND), always-on-top, click-through, focus mode timer, quick mute, theme quick-switch, clear all, grant notification access, retry access check, settings, quit.

### Overlay Interaction
| Action | Effect |
|--------|--------|
| Left-click a card | Dismiss it |
| Drag anywhere | Reposition the overlay |
| Hover | Pauses all expiry timers |
| Right-click a card | Context menu (dismiss, copy, clear all, mute app) |
| Drag left/right edge | Resize width (when manual resize is enabled) |

### Settings
Changes are debounced and auto-saved. Use **Send Test Notification** (Ctrl+T) to preview your styling without waiting for a real notification.

---

## Notification Access

On first run, Windows will prompt for notification access. If you denied it or it was not granted:
1. Open **Windows Settings → System → Notifications** (search "notification access" if you cannot find it).
2. Find the notification listener / notification access section.
3. Enable access for **Notifications Pro**.

The tray menu also has **Grant Notification Access** and **Retry Access Check** for troubleshooting.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| No notifications captured | Verify permission, then use tray "Retry Access Check". The app falls back to accessibility capture automatically when WinRT access is unavailable. |
| Can't drag the overlay | Click-through is on. Disable from the tray menu or Settings > Behavior. |
| Windows toasts stop appearing | Ensure "Suppress Toast Popups" is off in Settings. |
| System sounds all sound the same | Windows 11 unified many system sound events. Use a custom WAV for distinct sounds. |
| Overlay disappears off-screen | Use Settings > Position > Quick Position presets to move it back. |

---

## Project Docs

- [`docs/STATUS.md`](docs/STATUS.md) — current capabilities and manual test checklist
- [`docs/PLAN.md`](docs/PLAN.md) — milestones and roadmap
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — component overview

---

## Disclaimer of Liability

**Notifications Pro is strictly provided "AS IS", without warranty of any kind.**  
While extensively tested, this software hooks into Windows UI Automation and notification systems. The authors and contributors cannot be held liable for any claim, damages, data loss, or other liability, whether in an action of contract, tort or otherwise, arising from, out of, or in connection with the software or the use or other dealings in the software. Please refer to the `LICENSE` file for the full legal text.
