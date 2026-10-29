namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public record AudioDevice(string Id, string Name, bool Default);
public static partial class Native
{
    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-C0A336DA1CC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceCollection
    {
        [PreserveSig] int GetCount(out uint count); [PreserveSig] int Item(uint index, out IMMDevice device);
    }
    [StructLayout(LayoutKind.Sequential)]
    struct PropertyKey
    {
        public Guid Format; public uint Id;
    }
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    struct PropVariant
    {
        [FieldOffset(0)] public ushort Type; [FieldOffset(8)] public IntPtr Pointer;
    }
    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count); [PreserveSig] int GetAt(uint index, out PropertyKey key); [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value); [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value); [PreserveSig] int Commit();
    }
    [DllImport("ole32.dll")] static extern int PropVariantClear(ref PropVariant value);
    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    class PolicyConfigClient
    {
    }
    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string id, out IntPtr format);
        [PreserveSig] int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, int mode, out IntPtr format);
        [PreserveSig] int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id);
        [PreserveSig] int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr endpoint, IntPtr mix);
        [PreserveSig] int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, int mode, out long current, out long minimum);
        [PreserveSig] int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, ref long period);
        [PreserveSig] int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr mode);
        [PreserveSig] int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr mode);
        [PreserveSig] int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, ref PropertyKey key, ref PropVariant value);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string id, int role);
        [PreserveSig] int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string id, int visible);
    }
    public static List<AudioDevice> AudioDevices(int flow)
    {
        var enumerator = (IMMDeviceEnumerator)new DeviceEnumerator();
        IMMDeviceCollection? collection = null;
        IMMDevice? defaultDevice = null;
        string? defaultId = null;
        try
        {
            if (enumerator.GetDefaultAudioEndpoint(flow, 1, out defaultDevice) >= 0)
                defaultDevice.GetId(out defaultId);
            Marshal.ThrowExceptionForHR(enumerator.EnumAudioEndpoints(flow, 1, out collection));
            Marshal.ThrowExceptionForHR(collection.GetCount(out uint count));
            var result = new List<AudioDevice>();
            for (uint i = 0; i < count; i++)
            {
                Marshal.ThrowExceptionForHR(collection.Item(i, out var device));
                try
                {
                    Marshal.ThrowExceptionForHR(device.GetId(out var id));
                    string name = "Audio device";
                    if (device.OpenPropertyStore(0, out var pointer) >= 0)
                    {
                        try
                        {
                            var properties = (IPropertyStore)Marshal.GetObjectForIUnknown(pointer);
                            try
                            {
                                var key = new PropertyKey { Format = Guid.Parse("A45C254E-DF1C-4EFD-8020-67D146A850E0"), Id = 14 };
                                if (properties.GetValue(ref key, out var value) >= 0)
                                {
                                    try
                                    {
                                        if (value.Type == 31)
                                            name = Marshal.PtrToStringUni(value.Pointer) ?? name;
                                    }
                                    finally { PropVariantClear(ref value); }
                                }
                            }
                            finally { Marshal.ReleaseComObject(properties); }
                        }
                        finally { Marshal.Release(pointer); }
                    }
                    result.Add(new(id, name, id == defaultId));
                }
                finally { Marshal.ReleaseComObject(device); }
            }
            return result;
        }
        finally { if (defaultDevice != null) Marshal.ReleaseComObject(defaultDevice); if (collection != null) Marshal.ReleaseComObject(collection); Marshal.ReleaseComObject(enumerator); }
    }
    public static void SelectAudioDevice(string id)
    {
        var policy = (IPolicyConfig)new PolicyConfigClient();
        try
        {
            for (int role = 0; role < 3; role++)
                Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(id, role));
        }
        finally { Marshal.ReleaseComObject(policy); }
    }
}
