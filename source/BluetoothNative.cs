namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
public record BluetoothPeer(ulong Address, string Name, bool Connected, bool Paired);
public static class BluetoothNative
{
    [StructLayout(LayoutKind.Sequential)]
    struct RadioParams
    {
        public uint Size;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct SystemTime
    {
        public ushort Year, Month, DayOfWeek, Day, Hour, Minute, Second, Milliseconds;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DeviceInfo
    {
        public uint Size; public ulong Address; public uint Class; [MarshalAs(UnmanagedType.Bool)] public bool Connected; [MarshalAs(UnmanagedType.Bool)] public bool Remembered; [MarshalAs(UnmanagedType.Bool)] public bool Authenticated; public SystemTime Seen, Used; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)] public string Name;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct Search
    {
        public uint Size; [MarshalAs(UnmanagedType.Bool)] public bool Authenticated; [MarshalAs(UnmanagedType.Bool)] public bool Remembered; [MarshalAs(UnmanagedType.Bool)] public bool Unknown; [MarshalAs(UnmanagedType.Bool)] public bool Connected; [MarshalAs(UnmanagedType.Bool)] public bool Inquiry; public byte Timeout; public IntPtr Radio;
    }
    [DllImport("bthprops.cpl", SetLastError = true)] static extern IntPtr BluetoothFindFirstRadio(ref RadioParams p, out IntPtr radio);
    [DllImport("bthprops.cpl")] static extern bool BluetoothFindRadioClose(IntPtr find);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("bthprops.cpl", SetLastError = true)] static extern IntPtr BluetoothFindFirstDevice(ref Search search, ref DeviceInfo device);
    [DllImport("bthprops.cpl", SetLastError = true)] static extern bool BluetoothFindNextDevice(IntPtr find, ref DeviceInfo device);
    [DllImport("bthprops.cpl")] static extern bool BluetoothFindDeviceClose(IntPtr find);
    [DllImport("bthprops.cpl")] static extern uint BluetoothGetDeviceInfo(IntPtr radio, ref DeviceInfo device);
    [DllImport("bthprops.cpl")] static extern uint BluetoothEnumerateInstalledServices(IntPtr radio, ref DeviceInfo device, ref uint count, [Out] Guid[]? services);
    [DllImport("bthprops.cpl")] static extern uint BluetoothSetServiceState(IntPtr radio, ref DeviceInfo device, ref Guid service, uint flags);
    [DllImport("bthprops.cpl")] static extern uint BluetoothAuthenticateDeviceEx(IntPtr parent, IntPtr radio, ref DeviceInfo device, IntPtr oob, int requirements);
    public static (int Device, int Search) LayoutSize() => (Marshal.SizeOf<DeviceInfo>(), Marshal.SizeOf<Search>());
    static IntPtr Radio()
    {
        var p = new RadioParams { Size = (uint)Marshal.SizeOf<RadioParams>() };
        var find = BluetoothFindFirstRadio(ref p, out var radio);
        if (find == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "No accessible classic Bluetooth radio is available.");
        BluetoothFindRadioClose(find);
        return radio;
    }
    static DeviceInfo Info(ulong address) => new() { Size = (uint)Marshal.SizeOf<DeviceInfo>(), Address = address, Name = "" };
    public static List<BluetoothPeer> Read(bool discover = false)
    {
        var radio = Radio();
        try
        {
            var p = new Search { Size = (uint)Marshal.SizeOf<Search>(), Authenticated = true, Remembered = true, Unknown = discover, Connected = true, Inquiry = discover, Timeout = 2, Radio = radio };
            var d = Info(0);
            var find = BluetoothFindFirstDevice(ref p, ref d);
            var result = new List<BluetoothPeer>();
            if (find == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                if (error is 259 or 0)
                    return result;
                throw new Win32Exception(error);
            }
            try
            {
                do
                {
                    result.Add(new(d.Address, string.IsNullOrWhiteSpace(d.Name) ? "Bluetooth device" : d.Name, d.Connected, d.Authenticated));
                    d = Info(0);
                } while (BluetoothFindNextDevice(find, ref d));
            }
            finally { BluetoothFindDeviceClose(find); }
            return result.GroupBy(d => d.Address).Select(g => g.First()).OrderByDescending(d => d.Connected).ThenBy(d => d.Name).ToList();
        }
        finally { CloseHandle(radio); }
    }
    public static void Pair(ulong address, IntPtr owner)
    {
        var radio = Radio();
        try
        {
            var d = Info(address);
            uint result = BluetoothAuthenticateDeviceEx(owner, radio, ref d, IntPtr.Zero, 0);
            if (result != 0)
                throw new Win32Exception((int)result);
        }
        finally { CloseHandle(radio); }
    }
    public static void SetConnection(ulong address, bool connect)
    {
        var radio = Radio();
        try
        {
            var d = Info(address);
            uint result = BluetoothGetDeviceInfo(radio, ref d);
            if (result != 0)
                throw new Win32Exception((int)result);
            uint count = 0;
            BluetoothEnumerateInstalledServices(radio, ref d, ref count, null);
            if (count > 64)
                throw new InvalidOperationException("The device returned an invalid profile list.");
            var services = new Guid[count];
            if (count > 0)
            {
                result = BluetoothEnumerateInstalledServices(radio, ref d, ref count, services);
                if (result != 0)
                    throw new Win32Exception((int)result);
            }
            var known = new[] { "0000110b", "0000111e", "00001108", "00001124" }.Select(p => Guid.Parse(p + "-0000-1000-8000-00805f9b34fb")).ToArray();
            var profiles = services.Take((int)count).Where(known.Contains).ToArray();
            if (profiles.Length == 0)
            {
                uint major = d.Class & 0x1f00;
                if (major == 0x0400)
                    profiles = known.Take(3).ToArray();
                else if (major == 0x0500)
                    profiles = new[] { known[3] };
                else
                    throw new InvalidOperationException("This device has no supported audio/headset/HID classic profile. Windows does not expose a universal device-connect API.");
            }
            int success = 0;
            uint last = 0;
            foreach (var profile in profiles)
            {
                var service = profile;
                if (connect && !d.Connected)
                    BluetoothSetServiceState(radio, ref d, ref service, 0);
                uint code = BluetoothSetServiceState(radio, ref d, ref service, connect ? 1u : 0u);
                if (code is 0 or 1056)
                {
                    success++;
                    if (connect)
                        break;
                }
                else
                    last = code;
            }
            if (success == 0)
                throw new Win32Exception((int)last);
        }
        finally { CloseHandle(radio); }
    }
}
