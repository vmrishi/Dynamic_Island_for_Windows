namespace OrbitIsland;

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static partial class Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct PhysicalMonitor
    {
        public IntPtr Handle; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Description;
    }
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("dxva2.dll", SetLastError = true)] static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr monitor, out uint count);
    [DllImport("dxva2.dll", SetLastError = true)] static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr monitor, uint count, [Out] PhysicalMonitor[] monitors);
    [DllImport("dxva2.dll")] static extern bool DestroyPhysicalMonitors(uint count, PhysicalMonitor[] monitors);
    [DllImport("dxva2.dll", SetLastError = true)] static extern bool GetMonitorBrightness(IntPtr monitor, out uint minimum, out uint current, out uint maximum);
    [DllImport("dxva2.dll", SetLastError = true)] static extern bool SetMonitorBrightness(IntPtr monitor, uint value);
    public static int MonitorBrightness(IntPtr window, int? percent = null)
    {
        var monitor = MonitorFromWindow(window, 2);
        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out uint count) || count == 0 || count > 16)
            throw new InvalidOperationException("The display has no accessible DDC/CI brightness control.");
        var displays = new PhysicalMonitor[count];
        if (!GetPhysicalMonitorsFromHMONITOR(monitor, count, displays))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            foreach (var display in displays)
                if (GetMonitorBrightness(display.Handle, out uint min, out uint current, out uint max) && max > min)
                {
                    if (percent != null)
                    {
                        uint value = min + (uint)Math.Round((max - min) * Math.Clamp(percent.Value, 0, 100) / 100d);
                        if (!SetMonitorBrightness(display.Handle, value))
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        current = value;
                    }
                    return (int)Math.Round((current - min) * 100d / (max - min));
                }
            throw new InvalidOperationException("This display does not expose WMI or DDC/CI brightness. Enable DDC/CI in the monitor's own menu if available.");
        }
        finally { DestroyPhysicalMonitors(count, displays); }
    }
}
public static partial class Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DisplayDevice
    {
        public uint Size; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString; public uint State; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceId; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Key;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool EnumDisplayDevices(string name, uint index, ref DisplayDevice device, uint flags);
    public static string MonitorInstanceId(IntPtr window)
    {
        var display = System.Windows.Forms.Screen.FromHandle(window);
        var info = new DisplayDevice { Size = (uint)Marshal.SizeOf<DisplayDevice>(), DeviceName = "", DeviceString = "", DeviceId = "", Key = "" };
        if (EnumDisplayDevices(display.DeviceName, 0, ref info, 1))
        {
            var parts = info.DeviceId.Split('#');
            if (parts.Length >= 3)
                return "DISPLAY\\" + parts[1] + "\\" + parts[2];
        }
        if (System.Windows.Forms.Screen.AllScreens.Length == 1)
            return "";
        throw new InvalidOperationException("Cannot safely identify the selected display for WMI brightness.");
    }
}
