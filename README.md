# Orbit Dynamic Island 0.2

Native Windows WPF app updated from the supplied 0.1 archive using the attached screenshots as visual references.

## Portable application (Windows x64)

Download [Orbit Island Portable](https://github.com/vmrishi/Dynamic_Island_for_Windows/raw/refs/heads/work/downloads/Orbit-Island-Portable-win-x64.zip), extract the entire ZIP to a writable folder, then run `OrbitIsland.exe`. No installer or separate .NET installation is needed. Keep all extracted files together. Settings and notes are saved in the `Data` folder beside the executable, so they travel with the app. This portable package uses a separate data folder from earlier versions; import a JSON backup through Settings to transfer existing data. Delete the extracted folder to remove the portable app. Windows hardware/GUI behavior still requires Windows testing.

`portable.mode` selects this local data behavior. Removing it restores the default LocalAppData storage. `--data-dir` overrides either default. To rebuild the portable ZIP on Windows, run `Build-Portable.ps1` with the .NET 10 SDK installed.

## Run (original runtime-dependent package)

Requires Windows 10/11 x64 and the .NET 10 Desktop Runtime. Extract the entire folder, then double-click `Start Orbit.cmd`. Keep Broker.ps1 beside OrbitIsland.exe.

The island starts as a clock pill above the desktop with a top gap. Hover to expand; move away to collapse. Esc collapses, Alt+Shift+O toggles, Ctrl+, opens settings. The tray icon also opens settings or quits.

## Changes

- Dark rounded island, compact icon navigation, light appearance, sidebar settings, custom island/accent colors, font scale, icon size, width, monitor and top offset.
- Home media controls, compact dashboard and control cards, calendar month grid, health progress rings, timer view, day progress and live CPU/memory/network/disk views.
- Weather location in settings, current conditions, seven-day forecast and hourly forecast using Open-Meteo.
- Shelf supports file/shortcut drops, installed-app picker, automatic wrapping, native file icons and visible remove buttons. Original files are never deleted by removing a pin.
- Tasks stack under the entry field and support completion, removal, restore and permanent deletion from Trash.
- Notes have New note/list on the left and a large editor on the right. OK saves the draft; each saved note has a delete button. Save before switching away from a draft.
- Speaker volume unmutes when raised. COM endpoint enumeration preserves HRESULTs. Default microphone/speaker mute controls remain available.
- Bluetooth uses paired-device ConnectionStatus rather than PnP driver Status. Wi-Fi marks the connected SSID and control tiles show connection names. Opening Control or Connections refreshes state; it refreshes every 30 seconds while visible.
- Screen time uses real foreground-process tracking, with today/7-day views and CSV export. Tracking is opt-in and ignores idle periods longer than five minutes.
- AI Chat supports a configurable Ollama chat endpoint and model, in-memory conversation, new chat and send/error states. Install/run Ollama and download your chosen model separately. Default endpoint is http://localhost:11434/api/chat. Messages go to your configured server.
- Notification center supports mark all read and clear all. Optional Windows notification capture is in Settings → Notifications. Seen Windows IDs persist so clearing the local list does not immediately re-add the same notifications.

## Windows integration limits

- New Wi-Fi profiles/password entry and Bluetooth pairing/audio connection switching use Windows Settings. Saved Wi-Fi profiles can connect from the island; their profile names must match the SSID. netsh parsing assumes English field labels; location permission may be needed for scanning.
- Windows notification capture uses UserNotificationListener. Windows may deny access to an unpackaged application; a signed MSIX/package identity is not included. The app reports access failure and disables capture. Orbit reminders work independently. Clear/read controls affect Orbit's copy, not Windows Notification Center.
- Bluetooth state applies to classic paired Bluetooth devices; BLE-only devices and battery levels are not implemented. No battery values are invented.
- Temperature remains unavailable without a supported sensor provider. Brightness requires a supported built-in display. External DDC/CI is not included.
- Screen time starts with Orbit tracking; Windows does not supply prior historical per-app usage here. Live system metrics are sampled every two seconds.
- Calendar remains local with basic ICS import/export; Google/Outlook synchronization is not included.
- This is screenshot-inspired styling, not a pixel-verified reproduction. Windows GUI, hardware behavior, PowerShell WinRT calls and multi-monitor/DPI behavior require testing on Windows; this delivery was built on Linux.

## Build

Install .NET 10 SDK on Windows and run `powershell -ExecutionPolicy Bypass -File .\Build.ps1` (add `-Run` to launch expanded). `Test.ps1` runs the Windows self-test/diagnostics. Source is included.

Data is stored under `%LOCALAPPDATA%\OrbitDynamicIsland`. Existing settings are preserved; old installations can enable auto-collapse in Appearance. Backups can be exported in Settings → Data & startup.
