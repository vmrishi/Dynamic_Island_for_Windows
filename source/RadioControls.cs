namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Radios;
public static class RadioControls
{
    public static async Task<Dictionary<string, string>> Read()
    {
        var result = new Dictionary<string, string>();
        foreach (var radio in await Radio.GetRadiosAsync())
            result[radio.Kind.ToString()] = radio.State.ToString();
        return result;
    }
    public static async Task Toggle(string kind)
    {
        RadioKind target = kind == "WiFi" ? RadioKind.WiFi : RadioKind.Bluetooth;
        Radio? chosen = null;
        foreach (var radio in await Radio.GetRadiosAsync())
            if (radio.Kind == target)
            {
                chosen = radio;
                break;
            }
        if (chosen == null)
            throw new InvalidOperationException("No " + kind + " radio is available.");
        var access = await Radio.RequestAccessAsync();
        if (access != RadioAccessStatus.Allowed)
            throw new InvalidOperationException("Windows denied radio access: " + access + ".");
        var result = await chosen.SetStateAsync(chosen.State == RadioState.On ? RadioState.Off : RadioState.On);
        if (result != RadioAccessStatus.Allowed)
            throw new InvalidOperationException("Windows did not allow changing this radio: " + result + ".");
    }
}
