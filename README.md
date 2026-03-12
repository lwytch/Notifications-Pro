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
- **Capture mode selector** — `Settings > System > Notification Access` now offers `Auto`, `Prefer WinRT`, and `Force Accessibility` so you can recover quickly if live notifications stop flowing through the direct path.
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
- **Custom overlay lanes** — create reusable routed overlays with their own monitor, position preset, width, max-height, colours, and background images.

### Layout Modes
- **Stacked cards** — each notification is a separate card, scrollable when many accumulate.
- **Single-line banner mode** — all text compressed to a single line per notification, with optional wrapping and a configurable max-line count.
- **Newest-on-top** toggle — controls whether new notifications appear at the top or bottom.
- **Max visible** — configurable 1–40 visible cards, with new installs/reset defaults now starting at `40`. Extra notifications increment a `+N not shown` summary instead of being retained, and clicking that summary can raise the limit for future cards.
- **Max overlay height** — the overlay expands vertically up to this limit, then shows a scrollbar. Clamped to the active monitor work area.
- **Width / height presets** — quick buttons for 1080p / 2K / 4K / 8K display sizes.

### Appearance & Theming
- **Built-in theme presets** — select from a dropdown and apply in one click.
- **Custom themes** — save your current overlay look by name; re-apply or delete any time.
- **Import / export** — share a complete settings profile as a JSON file. Position and session state are preserved on the receiving machine.
- **Named profiles** — save, load, and delete full profile snapshots from the Profiles tab or tray menu.
- **Typography** — independently configure font family, size, and weight for app name, title, and body text. Line spacing control.
- **Timestamps** — optional per-card timestamps in Relative (`2m ago`), Time (`14:35`), or DateTime format, with independent size, weight, and colour.
- **Colours** — independent hex colours for title, app name, body text, background, and accent stripe.
- **Background opacity** — from fully opaque to near-transparent.
- **Card shape** — corner radius, internal padding, card gap, outer margin.
- **Grouping appearance** — grouped notifications can render as a `Framed Group`, `Header Chip`, or `Minimal Label`, with optional per-group counts, while reusing the normal accent/border/text styling controls.
- **Accent stripe** — 3 px coloured bar on the left edge of each card.
- **Optional border** — thin border around each card, configurable colour and thickness.
- **Per-app tint** — subtle colour tint on each card based on the source app name.
- **Icons** — optional per-app icons using 10 built-in vector presets (Bell, Megaphone, Star, Warning, Info, Heart, Lightning, Fire, Chat, Checkmark) or your own image files. Icon size configurable 16–48 px.
- **Chroma key** — solid-colour background (green / blue / magenta / custom) for OBS chroma-key filtering.
- **Information density presets** — Compact / Comfortable / Spacious — adjusts typography, spacing, and line limits in one click from the `Appearance` tab.

### App Profiles & Routing
- **Apps tab** — manage per-app read aloud, overlay lane assignment, sound, and icon from one place.
- **Lanes tab** — manage the reusable routed overlays themselves: monitor, position preset, width, max-height, colours, and background images.
- **Per-app profiles** — route X, Codex, Antigravity, Chrome, Edge, or any other source into the right lane without storing notification content.
- **Background images** — optional local-only lane images with opacity, hue, and brightness controls. Imported files are copied into `%AppData%\NotificationsPro\backgrounds\`.

### Behaviour & Control
- **Notification duration** — configurable display time in seconds; each card has its own timer.
- **App grouping** — optional grouping by source app, with a separate appearance control so the grouping behaviour lives in `Behavior` and the styling lives in `Appearance`.
- **Persistent notifications** — disable auto-expiry; cards stay until manually dismissed.
- **Auto-duration** — extends display time based on notification length (configurable base seconds + seconds-per-line).
- **Animations** — slide-in from Left / Right / Top / Bottom or fade-only, with configurable duration up to `1200ms` and that full range used as the default so the animation is actually visible out of the box.
- **Deduplication** — suppress identical notifications within a configurable time window.
- **Always-on-top** — toggle without restarting; also available from the tray menu.

### Filtering & Smart Control
- **Per-app mute** — silence notifications from specific apps. Muted apps are remembered per session; the settings UI shows every app seen this session.
- **Field-scoped keyword rules** — highlight or mute rules can target `Title`, `Body`, or `Title + Body`, optionally use regex, and optionally limit matching to a specific app or browser host.
- **Narration rules** — trigger `Read aloud` or `Skip read aloud` from title/body matches, with optional spoken-content overrides for that rule.
- **Quiet hours** — block all notifications between configurable start/end times (supports overnight ranges).
- **Burst limiting** — cap the number of notifications accepted in a sliding time window.
- **Focus mode** — a timed DND period (configurable minutes) accessible from the tray menu.
- **Presentation mode** — auto-DND when a configured app (PowerPoint, Zoom, Teams, etc.) goes fullscreen.

### Sounds
- **Master toggle** for notification sounds.
- **System sound presets** — Asterisk, Beep, Exclamation, Hand, Question.
  > Note: Windows 11 unified many system sound events, so some system sound choices may produce the same audio. For distinct sounds use a custom WAV file.
- **Custom WAV files** — browse for any `.wav` file; stored under `%AppData%\NotificationsPro\sounds\`.
- **Per-app overrides** — assign a different sound to each app from the `Apps` tab.
- **Test button** — play the current default sound immediately.

### System Integration
- **Start with Windows** — enables/disables the packaged Windows Startup Task for Notifications Pro.
- **Notification access recovery** — the System tab shows current capture status, includes buttons to open Windows notification access and retry the direct WinRT access check, and exposes `Auto`, `Prefer WinRT`, and `Force Accessibility` capture modes.
- **Session archive** — optional RAM-only archive for the current app session, with clipboard export and no disk persistence of notification text.
- **About dialog** — tray menu About shows the installed version, package identity, listener mode/status, runtime version, and project link.
- **Tray listener health** — tray tooltip surfaces the active listener mode plus current status details for faster troubleshooting.
- **Global hotkeys** — register system-wide keyboard shortcuts for: toggle overlay visibility, dismiss all notifications, toggle Do Not Disturb.
- **Settings window theming** — Dark / Light / System / any named overlay theme. Colours are fully customisable (background, surface, text, accent, border).
- **Settings popup mode** — settings window can float as a popup above the taskbar with optional auto-close.

### Accessibility
- **Accessibility mode** toggle — enables persistent notifications + system motion/contrast/text-scaling respect in one click.
- **Respect Reduce Motion** — disables slide animations when Windows "Reduce Motion" is active.
- **Respect High Contrast** — adapts overlay colours when Windows High Contrast is active.
- **Respect Text Scaling** — scales notification text with the Windows text-size accessibility setting.
- **Auto-duration** — longer notifications stay visible longer so there is time to read them.
- **Spoken notifications** — built-in narration can read multiple title/body/timestamp combinations, using any Windows-installed Microsoft-signed voice with adjustable speed and volume, a preview button, app-level `Read aloud` checkboxes in `Settings > Apps`, and once-only playback per visible card.
- **Narration targeting** — `Settings > Filtering > Narration Rules` can read or skip notifications from title/body/title+body matches without relying only on per-app narration choices.
- **Microsoft Voice Access labels** — choose `Off`, `Body Only`, or `Title + Body + Timestamp` for the card-level UI Automation label used by Voice Access and similar assistive tools.
- **Scrollable overlay** — when content exceeds the max height, a scrollbar appears so no text is lost.
- **Overlay scrollbar customisation** — show/hide scrollbar, configurable width (4–20 px) and opacity.

### Automation & Scheduling
- **CLI arguments** — `--pause`, `--resume`, `--theme <name>`, `--send-test`, `--hide`, and `--show` can control the app from shortcuts or scripts.
- **Theme scheduling** — automatically switch between day/night themes on a configured schedule.

---

## Common Use Cases

### Streaming / OBS
Keep notifications readable on-stream without disrupting your layout:
1. Settings > Streaming — enable **Chroma Key**, pick a key colour (Green / Blue / Magenta).
2. Settings > Streaming — enable **OBS Fixed Window Mode**, then set width and height to match your capture dimensions.
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
- Turn on **Settings > Accessibility > Read Notifications Aloud** if you want Notifications Pro itself to narrate incoming notifications.
- Use **Timestamps** in DateTime mode to track when notifications arrived.
- Use **Settings > Accessibility > Microsoft Voice Access** to expose either a generic card label, the notification body only, or the title + body + timestamp through Windows accessibility APIs.

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

## Workflow Guides

### Getting the Most Out of Notifications Pro
- Start with **Settings > System > Notification Access** and confirm the app is capturing reliably before tuning the visual side. If live notifications ever stall, switch **Capture Mode** to `Force Accessibility` first.
- Keep the queue readable for your workload. `40` visible cards is the current default, but long-running monitoring setups usually work best when paired with **Auto-duration** or **Persistent Notifications** rather than a lower visible-card limit alone.
- Use the app controls in layers: start with **Settings > Apps** for per-app lane/icon/sound/read-aloud decisions, use **Settings > Lanes** for routed overlay styling and placement, then use **Settings > Filtering** for title/body-scoped highlight, mute, and narration rules inside noisy shared apps.
- Build one layout for passive monitoring and one for active work. For example: a wide **Single-Line Banner Mode** profile for streaming or browser-heavy days, and a denser stacked-card profile for operations or moderation work.
- Route the noisiest source family to a dedicated **Lane** when one queue is not enough. This works well for keeping ops/agent traffic separate from chat or social notifications.
- Use tray actions and exports as part of the workflow, not just setup. **Pause**, **Focus Mode**, **Theme/Profile switching**, and **Export Settings** make it easier to move between work, streaming, and monitoring contexts without rebuilding the app each time.

### Getting the Most Out of X
- Pick the most consistent notification source you can. If X notifications arrive through a dedicated app or wrapper, per-app settings target X directly. If they arrive through **Chrome** or **Edge**, per-app settings apply to the browser host and **keyword rules** become the main way to separate X from the rest of your browser traffic.
- Use **Settings > Filtering > Keyword Highlighting**, **Keyword Muting**, and **Narration Rules** for account names, handles, campaign names, hashtags, watchwords, or brand terms. Rules can target `Title`, `Body`, or `Title + Body`, can be limited to the X source app/browser host, and can use either literal matches such as `@openai` or full regex patterns.
- Use **Keyword Muting** for repetitive low-signal phrases that tend to create noise, such as routine engagement wording or campaign traffic that does not need immediate attention.
- Use **Settings > Apps** to assign the X source to its own lane, sound, and icon, then use **Settings > Lanes** to give that routed overlay its own colours and optional background image so it is visually distinct from other browser-hosted traffic.
- Turn on **Read Notifications Aloud** globally in `Settings > Accessibility`, then use **Settings > Apps** to enable narration for the X source app and **Filtering > Narration Rules** for targeted spoken callouts.
- Use **Deduplication** and **Burst Limiting** when you expect spikes around large posts or breaking events. They help keep the overlay readable without implying that discarded overflow content is stored anywhere.
- Route X to its own **Lane** if you want social monitoring to stay visible without mixing it into work/tooling traffic on the main overlay.
- If X notifications stop appearing while previews still work, go back to **Settings > System > Notification Access** and try `Force Accessibility`, especially for browser-hosted toasts.

### Other Social Platforms

#### Reddit
- Track subreddit names, usernames, moderation phrases, and brand mentions with **Keyword Highlighting**.
- If Reddit notifications arrive through a browser, style the browser host with per-app controls and use subreddit or thread keywords for the real separation.
- Use **Burst Limiting** to keep fast comment chains or upvote storms from flooding the overlay.

#### Instagram / Facebook / LinkedIn
- Highlight client names, campaign tags, priority contacts, or product names so high-signal social activity stands out immediately.
- Use **Quiet Hours** and **Per-app Mute** when you only need business-hour visibility from these platforms.
- If several services share one browser host, treat the host app as the transport layer and use keyword rules to distinguish each platform or campaign.

#### Discord Communities and Similar Social Feeds
- Use **Per-app Mute** and **Keyword Highlighting** together so only important channels, community names, or alert words stay noisy.
- Add a custom **sound** only for the app you actively moderate, and leave lower-priority communities visual-only.
- Use **Copy Text** from the card context menu when you need to move a notification snippet quickly into another tool or response workflow.

### Common Notification-Heavy Tools

#### Codex / Antigravity / Browser-Based Agent Tooling
- If these tools notify through a browser, treat **Chrome** or **Edge** as the source app for per-app styling, narration, routing, and sounds, then use **keyword rules** for project names, environments, repo names, agent names, or failure wording.
- Use a clear **per-app icon**, **colour set**, and optional **background image** so agent/tool notifications are immediately distinct from social traffic arriving from the same host app.
- Route noisy agent/tool traffic to its own **Lane** when you want it visible but separated from chat, email, or social notifications.
- Pair **Read Notifications Aloud** with aggressive keyword filtering if you want spoken updates for builds, completions, or interventions without hearing every low-value event.
- Use **Persistent Notifications** or longer durations for actions that require a response instead of a glance.

#### GitHub / CI / Issue Trackers / Developer Alerts
- Highlight words such as `failed`, `blocked`, `review requested`, `incident`, `critical`, or release names that matter to your workflow.
- Add a distinct **sound** for the delivery app or browser host when build and deploy updates need to cut through ambient noise.
- Use **Deduplication** when automated systems tend to repeat the same failure or reminder wording.

#### Monitoring / Ops / Support Tooling
- Use **Persistent Notifications** for anything that must remain visible until acknowledged.
- Keep **Max Visible** high, but rely on **Burst Limiting** so storms remain readable.
- Combine **Per-app Mute** and **Quiet Hours** carefully so background systems stay visible when needed without turning the overlay into a permanent wall of noise.

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
- If **Spoken Notifications** is enabled, the text is spoken through your selected Windows audio output and may be audible to people nearby. Notifications Pro still keeps that text in RAM only and never saves spoken content to disk.
- Visible notification text is available to Windows accessibility tools while on screen. The Voice Access setting controls the card-level UI Automation label only; it does not save or transmit the text.

Windows may keep notification history in the Action Center independently. The optional toast-suppression feature removes captured notifications from the Action Center; leave it off to preserve Windows default behaviour.

### Files Written

Under `%AppData%\NotificationsPro\`:

| Path | Contents |
|------|----------|
| `settings.json` | All app preferences (no notification content) |
| `themes\*.json` | Named custom overlay themes |
| `icons\` | Optional user-provided icon image files |
| `sounds\` | Optional custom WAV files |
| `backgrounds\` | Optional user-provided background images for routed overlay lane styling |

---

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK (to build from source) or .NET 8 Runtime (to run a published build)

## Build / Run / Test

```bash
dotnet restore
dotnet build src/NotificationsPro/NotificationsPro.csproj
dotnet run --project src/NotificationsPro
dotnet test tests/NotificationsPro.Tests/NotificationsPro.Tests.csproj
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

In-place MSIX updates are expected to preserve `%AppData%\NotificationsPro\settings.json` because the settings file lives outside the packaged install directory.

> **Note on Windows Defender / SmartScreen:** 
> Because this is a new, indie open-source app targeting low-level UI Automation, Microsoft Defender or "Attack Surface Reduction" tools may flag it initially. This is a false positive completely expected for newly compiled binaries. You can safely click "More info" and "Run Anyway". If Defender continually blocks it, do not whitelist the entire WindowsApps folder; instead, add a specific exclusion for the `NotificationsPro.exe` process.
> 
> How you manage exclusions in your antivirus software is up to you. We are not liable for changes you make to your own security solutions. Please ensure you narrow down any changes specifically to this application using the method you see fit.

---

## How To Use

### Tray Icon
Right-click the tray icon to access: show/hide overlay, pause (DND), always-on-top, click-through, focus mode timer, quick mute, theme quick-switch, clear all, open notification-access settings, retry access check, settings, quit.

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

### Spoken Notifications
In **Settings > Accessibility > Spoken Notifications**, turn on **Read Notifications Aloud** to make Notifications Pro narrate captured notifications itself.

Choose from `Body Only`, `Title Only`, `Title + Body`, `Body + Timestamp`, `Title + Timestamp`, or `Title + Body + Timestamp`. You can also pick an installed Windows voice, adjust rate and volume, and use **Preview Voice** to test the current settings immediately.

Per-app narration lives in **Settings > Apps** via a `Read aloud` checkbox. Use **Settings > Filtering > Narration Rules** when you want only specific title/body/title+body matches read aloud or skipped. Unchecked apps still stay visible on screen but are ignored by narration. Visible cards are spoken once, so new arrivals do not replay cards that already finished speaking. Only Windows-installed Microsoft-signed voices appear in the picker.

If you want to install more voices, Microsoft’s setup guides are:
- [Customize Narrator voices](https://support.microsoft.com/windows/chapter-7-customizing-narrator-6e30e2d0-b2f3-b907-d264-a5d30502ad73)
- [Supported Narrator languages and voices](https://support.microsoft.com/windows/appendix-a-supported-languages-and-voices-for-narrator-448ec015-eb18-4ac2-8d0d-fac74d441e3b)

This is the app's own text-to-speech feature. Audio plays through your default Windows output and can be heard by people nearby. Notifications Pro does not write spoken text to disk and does not keep overflow notification content for later playback.

### Microsoft Voice Access
In **Settings > Accessibility > Microsoft Voice Access**, choose `Off`, `Body Only`, or `Title + Body + Timestamp` to control the card-level Windows UI Automation label that Microsoft Voice Access can reference for visible notifications.

This is an accessibility integration, not a separate text-to-speech engine inside Notifications Pro. The selected text is only exposed while the card is on screen, stays in RAM only, and is never written to disk, logged, or sent over the network.

---

## Notification Access

On first run, Windows will prompt for notification access. If you denied it or it was not granted:
1. Open **Windows Settings → Privacy → Notifications** (or search for `notification access`).
2. Find the notification listener / notification access section.
3. Enable access for **Notifications Pro**.

The tray menu also has **Open Privacy > Notifications...** and **Retry Access Check** for troubleshooting.

The same recovery controls now appear in **Settings > System > Notification Access**, alongside the current capture-mode status and a manual `Auto` / `Prefer WinRT` / `Force Accessibility` selector.

If live notifications stop appearing while preview/test notifications still work, switch `Capture Mode` to `Force Accessibility` first. That is the intended recovery path for WinRT delivery stalls or false-positive access states.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| No notifications captured | Verify permission, then use tray "Retry Access Check". If test notifications work but live ones do not, open **Settings > System > Notification Access** and switch **Capture Mode** to `Force Accessibility`. |
| Can't drag the overlay | Click-through is on. Disable from the tray menu or Settings > System. |
| Windows toasts stop appearing | Ensure "Suppress Toast Popups" is off in Settings > System. |
| System sounds all sound the same | Windows 11 unified many system sound events. Use a custom WAV for distinct sounds. |
| Overlay disappears off-screen | Use Settings > Layout > Quick Position presets to move it back. |
| Notifications are not read aloud | Turn on **Settings > Accessibility > Read Notifications Aloud**, then use **Preview Voice**. If you still hear nothing, check your Windows output device, ensure notifications are not paused, confirm the source app is still checked in **Settings > Apps**, and review **Filtering > Narration Rules** if you only expected specific matches to speak. |
| Voice Access only sees "Notification" | Change **Settings > Accessibility > Microsoft Voice Access** from `Off` to `Body Only` or `Title + Body + Timestamp`. |
| X, Reddit, Antigravity, or other web tools all look like browser notifications | Per-app controls apply to the browser host (`Chrome`, `Edge`, etc.). Use **Keyword Highlighting**, **Keyword Muting**, and browser-host styling to separate the actual services inside that host app. |

---

## Project Docs

- [`docs/STATUS.md`](docs/STATUS.md) — current capabilities and manual test checklist
- [`docs/PLAN.md`](docs/PLAN.md) — milestones and roadmap
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — component overview

---

## Disclaimer of Liability

**Notifications Pro is strictly provided "AS IS", without warranty of any kind.**  
While extensively tested, this software hooks into Windows UI Automation and notification systems. The authors and contributors cannot be held liable for any claim, damages, data loss, or other liability, whether in an action of contract, tort or otherwise, arising from, out of, or in connection with the software or the use or other dealings in the software. Please refer to the `LICENSE` file for the full legal text.

---

<details>
<summary><strong>Release Notes</strong></summary>

### Release v1.1.8.0
- **Lane Architecture Refresh**: Fixed the multi-lane rendering bug by giving each overlay its own lane-filtered view, and replaced the cramped per-app styling expander with a dedicated `Lanes` tab where routed overlays are edited once and reused across apps.
- **Apps Tab Simplification**: `Settings > Apps` now focuses on per-app lane assignment, narration, sounds, and icons, while routed overlay placement, colours, and background images live in `Settings > Lanes`.
- **Routing & Persistence Hardening**: Added reusable overlay-lane persistence/migration, updated settings export examples/docs, and kept legacy `Secondary` assignments compatible while the new lane model rolls forward.

### Release v1.1.7.0
- **Targeted Rules & Lane Routing**: Added title/body/title+body-scoped highlight, mute, and narration rules with optional app filters, plus a dedicated `Apps` tab for per-app read aloud, lane assignment, sounds, and icons.
- **Reusable Overlay Lanes**: Routed overlays now live in a dedicated `Lanes` tab with their own monitor, position preset, width, max-height, colours, and background images, making it practical to split social, tooling, or ops traffic cleanly.
- **Reliability & Docs Sync**: Restored display-aware first-run sizing before the overlay is created, fixed secondary-overlay preset normalization/placement, refreshed Help/README/status text, and expanded `settings.example.json` to match the shipped settings model.

### Release v1.1.6.4
- **Default UX Repair**: Re-aligned the shipped defaults so new installs and reset-to-default flows now consistently start with `40` visible notifications and the full `1200ms` animation timing, instead of the mixed `3` / `15` / `20` and `300ms` drift that had built up across files.
- **Overflow Summary Fix**: The overflow badge now reports `+N not shown` instead of implying a hidden expandable queue, and clicking it offers a privacy-safe way to raise the visible limit for future notifications without retaining discarded content.
- **Grouping & Appearance Cleanup**: `Information Density` moved into `Appearance`, grouped notifications now support `Framed Group`, `Header Chip`, and `Minimal Label` styles with optional counts, and the grouping visuals now follow the normal accent/border/corner styling instead of a hardcoded banner.

### Release v1.1.6.3
- **Narration Replay Fix**: Visible notifications are now spoken once per card, so when a new card arrives the app no longer re-reads older cards that already finished speaking.
- **Per-App Speech Checkboxes**: Replaced the earlier `Speak` / `Skip` button wording with explicit `Read aloud` checkboxes in `Settings > Accessibility` for clearer per-app narration control.
- **About & Persistence Audit**: The tray `About` dialog now shows the full installed package version and listener details, and a settings audit fixed `Text Alignment` persistence while expanding export/import test coverage for narration and capture-mode fields.

### Release v1.1.6.2
- **Per-App Spoken Notification Control**: Added per-app `Speak` / `Skip` controls in `Settings > Accessibility`, so you can keep specific apps visual-only while narration stays on globally.
- **Capture Recovery Upgrade**: Added a `System > Notification Access > Capture Mode` selector with `Auto`, `Prefer WinRT`, and `Force Accessibility`, and WinRT seed/poll failures now switch to accessibility capture automatically.
- **Voice Setup Help Links**: Added Microsoft voice-setup links in the Help tab, clarified that only Windows-installed Microsoft-signed voices appear in the picker, and fixed external Help/GitHub hyperlinks so they open reliably.

### Release v1.1.6.1
- **Expanded Spoken Content Modes**: Added `Title Only`, `Title + Body`, `Body + Timestamp`, and `Title + Timestamp` to the spoken-notification selector so you can choose the exact narration mix you want.
- **Fallback Speech Logic**: If a notification is missing the requested title or body field, narration now falls back to the available text instead of speaking an unhelpful partial result.

### Release v1.1.6.0
- **Built-in Spoken Notifications**: Added a separate Accessibility feature that can narrate notifications aloud with `Body Only` or `Title + Body + Timestamp` modes, installed Windows voice selection, speed/volume controls, and a `Preview Voice` button.
- **Pause-State Reliability**: Pausing notifications from the tray, hotkeys, or CLI now flows through the same settings pipeline, so spoken notifications stop and resume consistently with the rest of the app.
- **Transparency & Help Refresh**: Expanded the Accessibility tab, Help tab, README, and privacy notes so the difference between built-in narration and Microsoft Voice Access is explicit, including nearby-audio disclosure and RAM-only handling details.

### Release v1.1.5.0
- **Microsoft Voice Access Labels**: Added an opt-in Accessibility control with `Off`, `Body Only`, and `Title + Body + Timestamp` modes so Voice Access can target visible notification cards more naturally.
- **Privacy Transparency Upgrade**: Expanded the Accessibility and Help text to explain exactly what Voice Access exposes, when it is exposed, and that the text remains RAM-only and never goes to disk, logs, or the network.
- **System Recovery Controls**: Added an in-app `System > Notification Access` section that shows current capture status and provides `Open Windows Notification Access` plus `Retry Access Check` buttons.
- **Hotkey Failure Feedback**: Accessibility now shows when a hotkey is invalid or already taken instead of failing silently.

### Release v1.1.4.5
- **Settings IA Pass**: Reorganized the Settings tabs so controls now live in more logical places. Appearance now stays visual-only, Behavior owns card display/layout rules, Filtering owns Quiet Hours and Burst Limiting, Layout owns Fullscreen Overlay, Streaming owns Presentation Mode and app tinting, and Accessibility now owns Global Hotkeys.
- **Text Alignment Fix**: Repaired the text-alignment pipeline so Left / Center / Right now render correctly in stacked cards, compact banner cards, wrapped banner cards, and the live preview.
- **Hotkey Editor Repair**: Replaced the broken hotkey editor bindings with the three real shortcut fields used by the app: Show/Hide Overlay, Dismiss All, and Toggle Do Not Disturb.
- **Help Tab Refresh**: Updated the in-app Help content so troubleshooting and tab descriptions now match the current Settings layout and controls.

### Release v1.1.4.4
- **System Tab Missing Fix**: Restored the `System` tab to the UI layout which was fully constructed in memory but failed to render on-screen in version `1.1.4.3`. All OS-integrations are now perfectly accessible in the Settings window.

### Release v1.1.4.3
- **Settings Re-organization**: Introduced a foundational `System` tab for all OS-level configurations (Startup, Toast Suppression, Global Hotkeys, Session Archive, etc.) to heavily declutter existing styling tabs.
- **Global Replace Mode**: Decoupled 'Replace Mode' from the Single-Line layout constraint so the feature is globally available for all banner aesthetics.
- **Notification Typography**: Introduced `Text Alignment` configurations (Left, Center, Right) across all rendering templates.
- **Inline Timestamps**: Single-Line notification layouts now dynamically inject `Timestamps` directly beside the textual content when enabled.

### Release v1.1.4.2
- **Preview Scaling Logic**: Fixed a layout bug where Live Preview would fail to expand the window on the *second* attempt to enable it. Switching from a native `SizeChanged` dimension hook to an explicit `Dispatcher.InvokeAsync` render delay ensures the software forces the expansion mathematics even if the host UI layout engine hasn't fully cleared its cached bounds from the first toggle.

### Release v1.1.4.1
- **Preview Scaling Logic**: Fixed a race condition where the Live Preview card failed to physically shrink the window back down when disabled. The WPF display engine was collapsing the element faster than the app could read its height, resulting in it thinking the preview was 0-pixels tall. The software now statically caches the height in memory while the preview is active to mathematically guarantee the correct size is restored.

### Release v1.1.4.0
- **Undo/Redo Stability**: Fixed an internal data de-sync where pressing the 'Undo' button correctly reverted your setting changes in memory, but failed to visually revert the sliders on your screen. The UI now natively and instantly snaps back to match the data engine.
- **Preview Scaling**: Repaired a mathematical WPF quirk where the Settings Application flawlessly stretched taller to accommodate the Live Preview component being enabled, but failed to natively shrink the window back down when the Live Preview was disabled.

### Release v1.1.3.9
- **Window Drag Logic**: Removed the experimental Z-Index background layering on the Settings popup header. By stripping out the explicit custom cursor, native `IsEnabled="False"` cursors gracefully return so you intuitively know when Undo/Redo operations are natively unavailable without feeling your clicks are being eaten by the title bar.
- **Dynamic Preview Sizing**: Exchanged the arbitrary 120-pixel size change for a mathematical UI container `SizeChanged` event. Toggling to show or hide the preview card now algorithmically extracts the exact pixel differential before committing the new window size, resulting in perfectly precise window scaling.

### Release v1.1.3.8
- **Title Bar Drag Fix**: Decoupled the window dragging logic using Z-Index background layering. The empty title bar space remains fully draggable, but the interactive Header buttons (Undo, Redo, Preview, Settings Controls) now reliably process their clicks without being aggressively blocked by the dragger.
- **Dynamic Preview Sizing**: Toggling the Live Preview card in the Settings UI now gracefully expands and collapses the physical height of the window natively, rather than squashing or overlapping the panels beneath it.
- **Repository Maintenance**: Re-aligned the README.md Release Notes backlog.

### Release v1.1.3.7
- **Header Hit-Testing**: Introduced recursive visual tree hit-testing to prevent the title bar drag events from silencing click events aimed at the Undo/Redo/Close widgets. 

### Release v1.1.3.6
- **Mouse Event Bubbling**: Reworked `PreviewMouseLeftButtonDown` tunneling behavior to prevent the overlay dragger from eating native click events over interactive widgets.

### Release v1.1.3.5
- **Taskbar Gap Fix**: Addressed a mathematical gap limitation where bottom-aligned multi-monitor notifications sat 24 pixels above the Windows taskbar. The layout engine now sinks the invisible window padding strictly into the bezel boundaries, ensuring the fully-opaque notification cards rest flawlessly flush against the taskbar edge.

### Release v1.1.3.4
- **Dynamic Height Anchoring**: Fixed a hardcoded vertical constraint bug where bottom-aligned multi-monitor notifications relied on a 360-pixel fallback height. The app now queries the WPF overlay dimensions natively, mathematically shifting the UI dynamically to ensure pixel-perfect tight bottom layouts.

### Release v1.1.3.3
- **CenterScreen Launch Default**: Cleaned up position caching bindings. Whenever the app launches (or escapes from the anchored Popup mode payload), the Settings Window firmly launches directly in the center of your primary monitor instead of recalling an arbitrary corner.
- **Title Bar Dragger**: Replaced the restrictive top-right grab handle with a universally draggable entire title bar logic stream across the WPF Header grid.
- **Compact Window Memory**: Added an automated tracking routine to remember arbitrary user resizes. If you manually drag the window boundaries to 1200 pixels wide, swapping to Compact Layout shrinks the app tightly—and swapping back flawlessly restores your 1200 pixel preference without resetting to the 900 pixel default.
- **Tab Interface Expansion**: Expanded the standard window Height scale to natively expose all 9 settings tabs without invoking a scrollbar.

### Release v1.1.3.2
- **Compact Window Sizing & Toggle Fix:** Addressed user feedback where the Compact Layout toggle failed to seamlessly resize the window when clicked due to conflicting explicit property bindings. The exact layout dimensions are now recalculated on the fly, seamlessly scaling down towards the anchored corner tray edge without detaching into empty space.
- **Enlarged Settings Window Height:** The default height of the Settings Window across both normal and compact modes has been universally bumped up. This cures the visual issue where the two bottom-most tabs inside the left-hand navigation column were hidden beneath a scrollbar cutoff.
- **CenterScreen Launch Default:** Cleaned up position caching bindings. Whenever the app launches (or escapes from the anchored Popup mode payload), the Settings Window firmly launches directly in the center of your primary monitor instead of recalling an arbitrary corner.

### Release v1.1.3.1
- **True Frosted Glass Controls:** Introduced granular `Inner Surface Opacity` and `Interactive Element Opacity` sliders for total precision when building translucent themes. 
- **Compact Popup Fix:** Resolved a layout sizing restraint that broke Compact Mode while rendering as a Popup.

### Release v1.1.3.0
- **Compact Settings Window Layout**: Added a new styling toggle on the UI Styling tab. With a single click, you can shrink down the width of the Settings Window so that it consumes far less screen real estate, retaining full functionality in a much tighter frame.
- **Glass Transparency Layering**: The "Frosted Glass" Settings Opacity slider now flawlessly communicates its alpha transparency to visually lower the opacity of inner UI surfaces (like buttons, cards, and text box backgrounds) for a truly impressive glass aesthetic.
- **Settings Import Reliability Fixes**: Solved an internal bug where newly-introduced properties occasionally lost their values when a stored JSON file was imported. Furthermore, introduced a new built-in AI maintenance checklist (`maintain-settings-sync` skill) to structurally prevent this bug from reoccurring during future development.

### Release v1.1.2.9
- **Frosted Glass UI Control**: Added a brand new "Settings Background Opacity" slider to the UI Styling tab. This fixes the previously broken opacity mismatch for the "Frosted Glass" theme preset on the popup window, and allows users fine-grained control over how aggressively the settings window blends into the desktop behind it.

### Release v1.1.2.8
- **UI Clarifications**: Fixed the incorrect GitHub repository URL in the "About" dialog to point to the correct `lwytch` namespace.

### Release v1.1.2.7
- **Help Tab Polish**: Top-aligned the icons in the 'Settings Window Controls' section to prevent vertical misalignment when text wraps.
- **Comprehensive Documentation**: Added a full 'Settings Reference' section to the Help tab detailing the purpose of all nine configuration tabs.

### Release v1.1.2.6
- **UI Clarifications**: Swapped the Settings Drag Handle icon from a 'Pause' glyph to a literal 'Gripper' bar (`&#xE7C2;`) to convey proper intent.
- **Help Tab Completeness**: Added a new "Settings Window Controls" section to the bottom of the Help tab, properly outlining standard UI functions like drag-to-move, undo, redo, minimize, and close using the new grid-styled format.

### Release v1.1.2.5
- **Settings Popup Polish**: Added a grab handle next to the top-right controls allowing users to temporary click-and-drag the settings panel around their screens.
- **Visual Softening**: Updated the default corner radius of both the overlay and the settings popup to 20px.

### Release v1.1.2.4
- **List Indentation Polish**: Applied the exact same Grid-based indentation logic to the remainder of the Help tab, ensuring all numbered steps and bulleted lists align perfectly beneath their starting character when text wraps.

### Release v1.1.2.3
- **Help Tab Polish**: Upgraded the Privacy section bullet points to use grid-based layouts, guaranteeing symmetrical and clean text indentation when wrapping.

### Release v1.1.2.2
- **UI Consistency Pass**: Fixed visual height mismatch between TextBoxes and ComboBoxes across the Settings UI.
- **Layout Alignment**: Re-aligned the "Quick Position" buttons and the "Support & Updates" Help block to match standard panel padding.
- **Keyword Highlighting Redesign**: Overhauled the Keyword Highlighting settings block to use a neat, grid-based layout.
- **Build Pipeline Fixes**: Overhauled the native build script to forcibly bust the `.NET` publication cache to guarantee packaged artifacts are strictly up-to-date.

</details>
