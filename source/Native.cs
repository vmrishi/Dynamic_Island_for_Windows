using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;
namespace OrbitIsland;

public static class Native
{
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll")] public static extern bool LockWorkStation();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr SendMessageTimeout(IntPtr h, uint msg, IntPtr w, string l, uint flags, uint timeout, out IntPtr result);
    [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public uint size, time;
    }
    [DllImport("kernel32.dll")] static extern bool GetSystemTimes(out long idle, out long kernel, out long user);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX s);
    [StructLayout(LayoutKind.Sequential)]
    struct MEMORYSTATUSEX
    {
        public uint length, load; public ulong total, avail, page, availpage, virt, availvirt, extended;
    }
    [DllImport("user32.dll")] static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    public static void MediaKey(byte key)
    {
        keybd_event(key, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, 2, UIntPtr.Zero);
    }
    public static string? ForegroundApp()
    {
        var info = new LASTINPUTINFO { size = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        GetLastInputInfo(ref info);
        if (unchecked((uint)Environment.TickCount - info.time) > 300000)
            return null;
        GetWindowThreadProcessId(GetForegroundWindow(), out var pid);
        try
        {
            var p = Process.GetProcessById((int)pid);
            if (p.ProcessName is "OrbitIsland" or "Idle" or "LockApp")
                return null;
            return p.ProcessName;
        }
        catch { return null; }
    }
    static long oldIdle, oldTotal, oldRx, oldTx; static DateTime lastNet = DateTime.UtcNow;
    public static (double Cpu, double Memory, double UsedGb, double TotalGb, double Rx, double Tx) Metrics()
    {
        GetSystemTimes(out var idle, out var kernel, out var user);
        long total = kernel + user;
        double cpu = oldTotal == 0 ? 0 : 100d * (total - oldTotal - idle + oldIdle) / Math.Max(1, total - oldTotal);
        oldTotal = total;
        oldIdle = idle;
        var m = new MEMORYSTATUSEX { length = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        GlobalMemoryStatusEx(ref m);
        long rx = 0, tx = 0;
        foreach (var n in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            try
            {
                var s = n.GetIPv4Statistics();
                rx += s.BytesReceived;
                tx += s.BytesSent;
            }
            catch { }
        }
        var dt = Math.Max(.1, (DateTime.UtcNow - lastNet).TotalSeconds);
        double r = oldRx == 0 ? 0 : Math.Max(0, rx - oldRx) / dt, t = oldTx == 0 ? 0 : Math.Max(0, tx - oldTx) / dt;
        oldRx = rx;
        oldTx = tx;
        lastNet = DateTime.UtcNow;
        return (Math.Clamp(cpu, 0, 100), m.load, (m.total - m.avail) / 1073741824d, m.total / 1073741824d, r, t);
    }
    public static void Open(string path)
    {
        if (path.StartsWith("explorer.exe shell:::"))
        {
            var si = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
            si.ArgumentList.Add(path.Substring(13));
            Process.Start(si);
        }
        else
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
    public static async Task<(int Code, string Output)> Run(string file, params string[] args)
    {
        var si = new ProcessStartInfo(file) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 };
        foreach (var a in args)
            si.ArgumentList.Add(a);
        using var p = Process.Start(si) ?? throw new Exception("Could not start Windows helper.");
        var read = p.StandardOutput.ReadToEndAsync();
        var err = p.StandardError.ReadToEndAsync();
        using var cancel = new System.Threading.CancellationTokenSource(18000);
        try
        {
            await p.WaitForExitAsync(cancel.Token);
        }
        catch { try { p.Kill(true); } catch { } throw new Exception("Windows helper timed out."); }
        var output = await read;
        var error = await err;
        return (p.ExitCode, string.IsNullOrWhiteSpace(output) ? error : output);
    }
    public static async Task<JsonElement> Broker(string action, int value = 50)
    {
        var r = await Run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell/v1.0/powershell.exe"), "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(AppContext.BaseDirectory, "Broker.ps1"), "-Action", action, "-Value", value.ToString());
        try
        {
            using var json = JsonDocument.Parse(r.Output);
            var e = json.RootElement.Clone();
            if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty("error", out var er))
                throw new InvalidOperationException(er.GetString());
            return e;
        }
        catch (JsonException) { throw new Exception("Windows helper is unavailable. " + r.Output.Trim().Substring(0, Math.Min(180, r.Output.Trim().Length))); }
    }
    public static bool WindowsDark()
    {
        using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return (int?)(k?.GetValue("AppsUseLightTheme")) == 0;
    }
    public static void SetWindowsDark(bool dark)
    {
        using var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        k.SetValue("AppsUseLightTheme", dark ? 0 : 1);
        k.SetValue("SystemUsesLightTheme", dark ? 0 : 1);
        SendMessageTimeout(new IntPtr(0xffff), 0x1A, IntPtr.Zero, "ImmersiveColorSet", 2, 1000, out _);
    }
    public static (float Volume, bool Muted) Audio(int flow = 0)
    {
        var a = Endpoint(flow);
        try
        {
            Marshal.ThrowExceptionForHR(a.GetMasterVolumeLevelScalar(out var v));
            Marshal.ThrowExceptionForHR(a.GetMute(out var mute));
            return (v, mute);
        }
        finally { Marshal.ReleaseComObject(a); }
    }
    public static void SetVolume(float v)
    {
        var a = Endpoint(0);
        try
        {
            Marshal.ThrowExceptionForHR(a.SetMasterVolumeLevelScalar(Math.Clamp(v, 0, 1), Guid.Empty));
            if (v > 0)
                Marshal.ThrowExceptionForHR(a.SetMute(false, Guid.Empty));
        }
        finally { Marshal.ReleaseComObject(a); }
    }
    public static void ToggleMute(int flow)
    {
        var a = Endpoint(flow);
        try
        {
            Marshal.ThrowExceptionForHR(a.GetMute(out bool muted));
            Marshal.ThrowExceptionForHR(a.SetMute(!muted, Guid.Empty));
        }
        finally { Marshal.ReleaseComObject(a); }
    }
    static IAudioEndpointVolume Endpoint(int flow)
    {
        var en = (IMMDeviceEnumerator)new DeviceEnumerator();
        IMMDevice? device = null;
        try
        {
            int endpointResult = en.GetDefaultAudioEndpoint(flow, 1, out device);
            if (endpointResult < 0)
                throw new InvalidOperationException(flow == 1 ? "No default microphone is available. Choose an input in Windows Sound Settings." : "No default speaker is available. Choose an output in Windows Sound Settings.");
            var iid = typeof(IAudioEndpointVolume).GUID;
            Marshal.ThrowExceptionForHR(device.Activate(ref iid, 23, IntPtr.Zero, out object obj));
            return (IAudioEndpointVolume)obj;
        }
        finally { if (device != null) Marshal.ReleaseComObject(device); Marshal.ReleaseComObject(en); }
    }
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class DeviceEnumerator
    {
    }
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int flow, int mask, out IntPtr devices); [PreserveSig] int GetDefaultAudioEndpoint(int flow, int role, out IMMDevice device);
    }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int cls, IntPtr activation, [MarshalAs(UnmanagedType.IUnknown)] out object obj); [PreserveSig] int OpenPropertyStore(int access, out IntPtr properties); [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id); [PreserveSig] int GetState(out uint state);
    }
    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr p); [PreserveSig] int UnregisterControlChangeNotify(IntPtr p); [PreserveSig] int GetChannelCount(out uint count); [PreserveSig] int SetMasterVolumeLevel(float level, Guid context); [PreserveSig] int SetMasterVolumeLevelScalar(float level, Guid context); [PreserveSig] int GetMasterVolumeLevel(out float level); [PreserveSig] int GetMasterVolumeLevelScalar(out float level); [PreserveSig] int SetChannelVolumeLevel(uint channel, float level, Guid context); [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, Guid context); [PreserveSig] int GetChannelVolumeLevel(uint channel, out float level); [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level); [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, Guid context); [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }
}




