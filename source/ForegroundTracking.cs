namespace OrbitIsland;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
public static partial class Native
{
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll")] static extern bool CloseDesktop(IntPtr desktop);
    delegate bool ChildWindowCallback(IntPtr child, IntPtr state);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, ChildWindowCallback callback, IntPtr state);
    public record ForegroundInfo(string? Key, string Label, string? Path);
    public static ForegroundInfo ForegroundSample(string? playingSource = null)
    {
        var input = new LASTINPUTINFO { size = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        GetLastInputInfo(ref input);
        bool idle = unchecked((uint)Environment.TickCount - input.time) >= 60000;
        var desktop = OpenInputDesktop(0, false, 1);
        if (desktop == IntPtr.Zero)
            return new(null, "Locked / secure desktop", null);
        CloseDesktop(desktop);
        var foreground = GetForegroundWindow();
        GetWindowThreadProcessId(foreground, out uint pid);
        try
        {
            using var host = Process.GetProcessById((int)pid);
            if (host.ProcessName == "ApplicationFrameHost")
            {
                uint resolved = pid;
                EnumChildWindows(foreground, (child, _) => { GetWindowThreadProcessId(child, out uint other); if (other != 0 && other != pid) { resolved = other; return false; } return true; }, IntPtr.Zero);
                pid = resolved;
            }
        }
        catch { }
        if (pid == 0)
            return new(null, "Desktop", null);
        try
        {
            using var process = Process.GetProcessById((int)pid);
            if (process.ProcessName is "OrbitIsland")
                return new(null, "Orbit Island (excluded)", null);
            if (process.ProcessName is "LockApp" or "Idle" or "dwm")
                return new(null, "Idle", null);
            if (idle && !UsageRecorder.MatchesPlayingSource(process.ProcessName, playingSource))
                return new(null, "Idle", null);
            string? path = null;
            string label = process.ProcessName;
            try
            {
                path = process.MainModule?.FileName;
                if (path != null)
                {
                    var description = FileVersionInfo.GetVersionInfo(path).FileDescription;
                    if (!string.IsNullOrWhiteSpace(description))
                        label = description;
                }
            }
            catch { }
            return new(process.ProcessName, label, path);
        }
        catch { return new(null, "Unavailable", null); }
    }
}
