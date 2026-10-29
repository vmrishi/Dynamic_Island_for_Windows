namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
public record WifiNetwork(string Name, string Profile, int Signal, bool Connected, bool Secured, uint Auth, uint Cipher, Guid Adapter);
public record WifiSnapshot(string Connected, List<WifiNetwork> Networks);
public static class WifiNative
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct InterfaceInfo
    {
        public Guid Id; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Description; public int State;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct Ssid
    {
        public uint Length; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Bytes; public string Text => Encoding.UTF8.GetString(Bytes, 0, (int)Math.Min(Length, 32));
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct Available
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Profile; public Ssid Ssid; public uint Bss, Bssids; [MarshalAs(UnmanagedType.Bool)] public bool Connectable; public uint Reason, PhyCount; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public uint[] Phys; [MarshalAs(UnmanagedType.Bool)] public bool More; public uint Signal; [MarshalAs(UnmanagedType.Bool)] public bool Secured; public uint Auth, Cipher, Flags, Reserved;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct Connection
    {
        public int Mode; public IntPtr Profile, Ssid, Bssids; public uint Bss, Flags;
    }
    [DllImport("wlanapi.dll")] static extern uint WlanOpenHandle(uint version, IntPtr reserved, out uint negotiated, out IntPtr client);
    [DllImport("wlanapi.dll")] static extern uint WlanCloseHandle(IntPtr client, IntPtr reserved);
    [DllImport("wlanapi.dll")] static extern uint WlanEnumInterfaces(IntPtr client, IntPtr reserved, out IntPtr list);
    [DllImport("wlanapi.dll")] static extern void WlanFreeMemory(IntPtr memory);
    [DllImport("wlanapi.dll")] static extern uint WlanGetAvailableNetworkList(IntPtr client, ref Guid adapter, uint flags, IntPtr reserved, out IntPtr list);
    [DllImport("wlanapi.dll")] static extern uint WlanQueryInterface(IntPtr client, ref Guid adapter, int opcode, IntPtr reserved, out uint size, out IntPtr data, out int type);
    [DllImport("wlanapi.dll")] static extern uint WlanScan(IntPtr client, ref Guid adapter, IntPtr ssid, IntPtr data, IntPtr reserved);
    [DllImport("wlanapi.dll", CharSet = CharSet.Unicode)] static extern uint WlanSetProfile(IntPtr client, ref Guid adapter, uint flags, string xml, string? security, [MarshalAs(UnmanagedType.Bool)] bool overwrite, IntPtr reserved, out uint reason);
    [DllImport("wlanapi.dll")] static extern uint WlanConnect(IntPtr client, ref Guid adapter, ref Connection parameters, IntPtr reserved);
    [DllImport("wlanapi.dll")] static extern uint WlanDisconnect(IntPtr client, ref Guid adapter, IntPtr reserved);
    public static (int Interface, int Network, int Connection) LayoutSize() => (Marshal.SizeOf<InterfaceInfo>(), Marshal.SizeOf<Available>(), Marshal.SizeOf<Connection>());
    static void Check(uint code)
    {
        if (code != 0)
            throw new Win32Exception((int)code, code == 5 ? "Windows denied Wi-Fi access. Location permission or a Windows-managed profile may be required." : new Win32Exception((int)code).Message);
    }
    static IntPtr Open()
    {
        Check(WlanOpenHandle(2, IntPtr.Zero, out _, out var h));
        return h;
    }
    static List<InterfaceInfo> Interfaces(IntPtr h)
    {
        Check(WlanEnumInterfaces(h, IntPtr.Zero, out var list));
        try
        {
            int count = Marshal.ReadInt32(list);
            var result = new List<InterfaceInfo>();
            for (int i = 0; i < count; i++)
                result.Add(Marshal.PtrToStructure<InterfaceInfo>(IntPtr.Add(list, 8 + i * Marshal.SizeOf<InterfaceInfo>())));
            return result;
        }
        finally { WlanFreeMemory(list); }
    }
    public static WifiSnapshot Read(bool scan = false)
    {
        var h = Open();
        try
        {
            var result = new List<WifiNetwork>();
            string connected = "Not connected";
            foreach (var inf in Interfaces(h))
            {
                var id = inf.Id;
                if (scan)
                    WlanScan(h, ref id, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                uint query = WlanQueryInterface(h, ref id, 7, IntPtr.Zero, out _, out var data, out _);
                if (query == 0)
                {
                    try
                    {
                        if (Marshal.ReadInt32(data) == 1)
                            connected = Marshal.PtrToStructure<Ssid>(IntPtr.Add(data, 520)).Text;
                    }
                    finally { WlanFreeMemory(data); }
                }
                uint code = WlanGetAvailableNetworkList(h, ref id, 0, IntPtr.Zero, out var list);
                Check(code);
                try
                {
                    int count = Marshal.ReadInt32(list);
                    for (int i = 0; i < count; i++)
                    {
                        var n = Marshal.PtrToStructure<Available>(IntPtr.Add(list, 8 + i * Marshal.SizeOf<Available>()));
                        string name = n.Ssid.Text;
                        if (name.Length > 0)
                            result.Add(new(name, n.Profile ?? "", (int)n.Signal, (n.Flags & 1) != 0, n.Secured, n.Auth, n.Cipher, id));
                    }
                }
                finally { WlanFreeMemory(list); }
            }
            return new(connected, result.GroupBy(n => (n.Name, n.Secured)).Select(g => g.OrderByDescending(n => n.Connected).ThenByDescending(n => n.Profile.Length > 0).ThenByDescending(n => n.Signal).First()).OrderByDescending(n => n.Connected).ThenByDescending(n => n.Signal).ToList());
        }
        finally { WlanCloseHandle(h, IntPtr.Zero); }
    }
    public static void Connect(WifiNetwork network, string? password = null)
    {
        var h = Open();
        try
        {
            var id = network.Adapter;
            string profile = network.Profile;
            if (profile.Length == 0 || password != null)
            {
                profile = network.Name;
                Check(WlanSetProfile(h, ref id, 2, WifiProfiles.Build(network.Name, password, network.Auth, network.Cipher, network.Secured), null, true, IntPtr.Zero, out var reason));
            }
            var pointer = Marshal.StringToHGlobalUni(profile);
            try
            {
                var p = new Connection { Mode = 0, Profile = pointer, Bss = 1 };
                Check(WlanConnect(h, ref id, ref p, IntPtr.Zero));
            }
            finally { Marshal.FreeHGlobal(pointer); }
        }
        finally { WlanCloseHandle(h, IntPtr.Zero); }
    }
    public static void Disconnect(Guid adapter)
    {
        var h = Open();
        try
        {
            Check(WlanDisconnect(h, ref adapter, IntPtr.Zero));
        }
        finally { WlanCloseHandle(h, IntPtr.Zero); }
    }
}
