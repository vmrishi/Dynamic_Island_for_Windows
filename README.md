# Orbit Island 0.3.1 — top-attached notch

A native C#/.NET Windows app. The visual layer has been rebuilt using the supplied Poni screenshots as references. Poni's reference website is https://poni.web.id/en/; Orbit is an independent app, not a copy of Poni's source code or a verified reproduction of its implementation.

## Download the portable app

[Download Orbit Island v0.3.1 Portable — Windows x64](https://github.com/vmrishi/Dynamic_Island_for_Windows/raw/refs/heads/work/downloads/Orbit-Island-Portable-v0.3.1-win-x64.zip)

Extract **all** files to a writable folder, then run **OrbitIsland.exe**. No installer or separate .NET installation is required. Keep the extracted files together. Data is stored in the adjacent `Data` folder; export/import a JSON backup in Settings to transfer content from an older installation. Quit the old version from the tray before opening the new one.

## Startup fix in 0.3.1

Fixed the read-only geometry exception during startup and resize. The notch clip now clones the parsed geometry before applying its translation, then freezes the completed clip. Existing Data folders remain compatible.

## New interface

- A drawn notch attaches to the top edge with **outward-curving top shoulders**. The closed state is a 300-DIP-wide, 48-DIP-high clock notch, not a floating pill. Opening/collapse uses a short size transition.
- The expanded notch keeps its outward top shoulders and rounded bottom corners, as in the reference. Per-module heights match the compact and tall reference layouts.
- Consistent vector icons replace the mixed font/Unicode icons. A compact scrollable navigation strip, selected tab label, keep-awake shortcut, notification badge and settings button run along the top.
- Home has album art when the Windows media session provides it, track metadata, a thin progress bar, transport controls and a seven-day calendar strip.
- Dashboard has four cards for toggles, weather, apps and day progress. Control uses split Wi-Fi/Bluetooth tiles, clear connection captions and custom volume/brightness sliders.
- Shelf has a large dashed drop area, automatic shortcut arrangement, installed-app picker and visible removal controls. Copy paths copies paths rather than pretending to share files through an unimplemented service.
- Todo has a single task input, reminder button and Active/Done/Trash filters. Notes has its list on the left and a large editor with OK on the right. Drafts save when switching notes, leaving the tab, collapsing or quitting.
- Pomodoro has a timer ring, segmented modes, circular playback controls, session dots and daily statistics. Health has three activity rings and a 2×2 water/movement/breathing/eye-break layout.
- Weather uses a blue forecast panel with current conditions, humidity, wind, daily maximum UV, pressure, sunrise/sunset and side-by-side hourly/daily forecasts. Location is configured in Settings.
- Screen Time has a category ring, usage legend, app ranking, recorded hourly timeline and today/7-day views. Categories are inferred from process names; durations and app switches come from real local tracking. The hourly chart shows today's recorded hours.
- Day Progress has a large day bar, time labels, week/month/year cards and upcoming local events. System has five live metric cards, with unavailable temperature explicitly indicated.
- Bluetooth/Wi-Fi, calendar, AI Chat and notifications have also been redesigned. Notification filters, mark-all-read and clear-all work on Orbit's local list.
- Settings has a searchable left sidebar, Welcome feature cards and pages for every module. General controls attachment, light/dark theme, colors, font size, icon size, width, monitor, opacity and hover behavior.

## Controls

Hover to open. Move away to collapse (configurable). Esc collapses; Alt+Shift+O toggles; Ctrl+, opens settings. Use the tray icon to open, customize or quit. To detach the notch, turn off **General → Attach to top edge** and set an offset.

`portable.mode` selects storage beside the app. Removing it restores `%LOCALAPPDATA%\OrbitDynamicIsland`. `--data-dir <path>` overrides either default.

## Integration limits

- Saved Wi-Fi profiles can connect from Orbit if the profile name matches the SSID. New network credentials, Bluetooth pairing and audio device switching open Windows Settings. Wi-Fi parsing expects English netsh labels; Windows may require location access.
- Bluetooth reports paired classic-device ConnectionStatus, not PnP driver health. BLE-only devices and battery levels are not implemented; no battery values are fabricated.
- Brightness needs a supported built-in display. External DDC/CI and compatible CPU temperature sensor providers are not included.
- Screen Time begins when enabled; prior Windows app usage history cannot be imported by this implementation. Idle periods longer than five minutes are ignored.
- Calendar is local with basic ICS import/export. Recurrence and TZID-tagged imports are skipped; Google/Outlook synchronization is not implemented.
- AI Chat uses a separately installed/running Ollama server and configurable model/endpoint, not Gemini. Messages go to that endpoint.
- Optional Windows notification capture requires permission and may require packaged app identity. A signed MSIX is not included. Clear/read actions affect Orbit's local copy, not Windows Notification Center. Orbit reminders work independently.

## Build and verification

Build on Windows with the .NET 10 SDK: run `Build.ps1`, or `Build-Portable.ps1` for a self-contained Windows x64 ZIP. Source includes a compiled WPF resource dictionary (`Theme.xaml`), so templates are checked by the build.

Portable v0.3.1 was cross-compiled on Linux. Ten model/geometry regression checks passed, the Windows build passed, and the ZIP's runtime contents/integrity were checked. **Windows GUI rendering, screenshot matching, hardware behavior, media artwork and WinRT broker calls have not been executed in this environment.** This is a screenshot-guided rebuild; it is not claimed to be pixel-verified.

Run `dotnet run --project tests/ModelTests.csproj` for portable model checks. On Windows, run `Capture-UI.ps1` beside the EXE (or in the repository after building) to render every module, validate usable layouts/icon geometry and open the screenshots folder. It uses an isolated temporary data directory. The command reports rendering failures; screenshots still need visual review. `Test.ps1` runs the existing Windows self-tests.
