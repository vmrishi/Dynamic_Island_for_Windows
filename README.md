# Orbit Island 0.4

Portable C#/.NET Dynamic Island for **Windows 11 or Windows 10 version 2004+**, x64. Screenshot reference: https://poni.web.id/en/. Orbit is independent of Poni.

[Download Orbit Island 0.4 Portable](https://github.com/vmrishi/Dynamic_Island_for_Windows/raw/refs/heads/work/downloads/Orbit-Island-Portable-v0.4-win-x64.zip)

## Update / run

Quit the old Orbit process from the tray. Extract the entire new ZIP into a **new writable folder** and run `OrbitIsland.exe`. No installer or separate .NET runtime is needed. Copy your old **Data** folder beside the new EXE before launching to retain settings, tasks, notes, calendar and usage history. Keep all new runtime DLLs together; do not mix the old 0.3 runtime files with 0.4. This release adds Windows Runtime projection assemblies for direct media/radio control, so there is no small DLL-only patch.

Hover opens the clock notch; leaving collapses it. Esc closes an island overlay or collapses the notch; Alt+Shift+O toggles; Ctrl+, opens Orbit Settings. `portable.mode` keeps data beside the EXE. `--data-dir <path>` overrides storage.

## Changes in 0.4

- Dashboard quick toggles are rounded rectangles with **white ON backgrounds and dark icons**, rather than stretched circles. Control connectivity tiles retain the reference's blue highlight and have properly joined split-button corners.
- Progress tracks use a clipped rounded renderer. The day bar has a restrained gradient, rounded ends and subtle timeline divisions. Sliders have rounded tracks and inset thumbs.
- The island uses one fixed design surface inside a Viewbox. Overall width/display scaling resizes icons, text and sections together. Font and icon overrides are supported without independently stretching each widget. Compact/expanded, module and overlay transitions use eased bounds changes; routine media updates no longer rebuild the player every two seconds.
- A slim 7-DIP scrollbar replaces the default menu/content scrollbar. Empty/hover tooltips and mouse-click focus rectangles are removed. Keyboard-only focus outlines remain. Text fields have explicit inset padding so the caret stays away from the edge.
- All 73 interface symbols come from the standard **Lucide** set with consistent stroke weights. These are redistributable cross-platform icons, not Apple's restricted SF Symbols. Attribution is included in `Licenses`.
- Weather opens its full forecast view and refreshes on entry. It shows current conditions, humidity, wind, current-hour UV, pressure, sunrise/sunset and seven hourly/two-hourly and daily forecast columns, with distinct day/night/rain/snow/storm symbols. Location changes, refresh and error status are available inside the island. A new location is saved only after its forecast loads successfully.
- Screen Time attributes the preceding interval to the previous foreground app, splits at hour/midnight boundaries, ignores suspend/long polling gaps and uses the same date range for seven-day charts and totals. Friendly process names/icons are saved when available; short durations show seconds. Turning tracking off also stops collecting process metadata. Existing historical records are preserved rather than fabricated or rewritten.
- Tracking is opt-in. Idle periods over 60 seconds, secure/locked desktops and Orbit itself are excluded. An actively playing media session whose source matches the foreground process is kept active while watching/listening. Windows 10 ApplicationFrameHost child processes are resolved where available. The records are estimates from one-second foreground samples, not imported Windows history.
- Calendar month navigation no longer changes the selected event date. It has a Monday-first six-week grid, distinct selection/today states, in-island event creation/editing with date, time and duration, and duration-aware ICS export. Basic ICS import/export remains local; recurrence and TZID imports are not supported.
- Device, password, media-source, sound, event and screenshot dialogs are displayed as **island overlays**. The Settings app shortcut opens Orbit's settings rather than Windows Settings. The screenshot action captures the chosen monitor, saves under `Data/Screenshots` and offers an in-island preview/copy action.

## Music / browser media

Music metadata and transport now use **Windows Global System Media Transport Controls directly in-process**. The old global media-key control and PowerShell metadata polling are no longer used.

Start playback in a compatible browser or Windows app, open Home, then use **Choose source / the displayed source name** if needed. Play/pause, next, previous and seeking target the native session represented by the displayed track. Controls are enabled according to the player's advertised capabilities. Timeline position is refreshed/estimated from Windows timestamps, and artwork is cached/decoded at thumbnail size.

Chrome, Edge, Firefox, Spotify and Windows players work **when they expose a Windows media session**. Browser settings, DRM/private content and individual players can restrict metadata or actions. Orbit cannot control a player that supplies no media session. The source picker explains missing sessions or rejected commands instead of pretending to control an app or sending keys to a different player. These integrations still need live Windows/browser verification.

## Device controls inside the island

- **Wi-Fi:** native WLAN scans/status, saved-profile connection, personal WPA/WPA2/WPA3-SAE password entry, password changes and disconnect. Credentials are passed to Windows WLAN profiles and are not stored in Orbit's JSON. Enterprise/legacy authentication requires an already provisioned Windows profile. Windows location/privacy permissions can deny scanning; the error is displayed in the island. No automatic Windows Settings redirect is used.
- **Bluetooth:** native classic-device discovery and pairing/connection status. Connect/disconnect requests enable/disable supported audio/headset/HID profile services and wait for the reported state. BLE-only devices, unsupported profiles, driver restrictions and inaccessible radios are reported as unsupported/errors. Windows has no universal classic-device connect API. Secure pairing may require a native operating-system confirmation prompt; this is not a Settings redirect. Battery readings are not invented.
- **Sound:** active output/input device lists, default-device selection and mute controls are available in the island. Default selection uses Windows audio-policy COM and is subject to OS compatibility. Apps pinned to their own output can ignore system-default changes.
- **Brightness:** selected-monitor DDC/CI first, then matching WMI support where available. No unrelated monitor is selected as a fallback. Hardware without an exposed brightness API displays an explanation/retry action rather than opening Windows Settings.
- **Media/radio:** direct Windows Runtime APIs. The small PowerShell broker remains only for WMI brightness and optional system notification capture.

## Other limits

System CPU/RAM/network/disk readings are live. CPU temperature requires a compatible sensor provider and is shown as unavailable. AI Chat uses a separately running Ollama server. Calendar does not synchronize Google/Outlook accounts. Windows notification capture may need packaged app identity and OS permission; Orbit reminders work independently. Read/clear actions apply to Orbit's copy. A signed MSIX is not included.

## Build / validation

With the .NET 10 SDK on Windows, run `Build.ps1` for the runtime-dependent app or `Build-Portable.ps1` for the complete self-contained ZIP. `Theme.xaml` is compiled, and icon data is embedded in the assembly.

The release is cross-built on Linux. **Windows GUI, browser playback, Bluetooth pairing/connection, WLAN operations, audio switching and monitor hardware have not been executed here.** Build success and regression checks do not establish hardware compatibility.

`dotnet run --project tests/ModelTests.csproj` runs portable logic/native-layout checks. With `-- --assembly <OrbitIsland.dll>`, it also inspects the built PE's embedded icons/theme. There are 34 checks with the assembly inspection enabled.

`Capture-UI.ps1` beside the portable EXE (or in the repository after building) runs isolated Windows render checks, including the earlier frozen-geometry fix, white rectangular toggles, rounded progress clipping and compact/wide layout. It captures every module under `%TEMP%/OrbitVisualTest-*` for visual review. Those Windows-only checks are included but have not been run in this environment. `Test.ps1` retains the original Windows persistence/diagnostic entry points.
