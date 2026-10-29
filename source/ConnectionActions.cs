namespace OrbitIsland;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
public partial class Island
{
    void ShowAudioDevices(int flow = 0)
    {
        try
        {
            var content = Col();
            var controls = Row(Pill("Output", "volume", () => ShowAudioDevices(0)), Pill("Input", "mic", () => ShowAudioDevices(1)));
            content.Children.Add(controls);
            Gap(content, 14);
            var devices = Native.AudioDevices(flow);
            if (devices.Count == 0)
                content.Children.Add(T("No active audio devices found.", 14, Muted));
            foreach (var device in devices)
            {
                var b = Pill(device.Name, device.Default ? "check" : flow == 0 ? "headphones" : "mic", () => { try { Native.SelectAudioDevice(device.Id); CloseOverlay(); } catch (Exception e) { ShowIslandError("Cannot switch audio device", e.Message); } }, device.Default);
                b.HorizontalContentAlignment = HorizontalAlignment.Left;
                b.Margin = new Thickness(0, 0, 0, 8);
                content.Children.Add(b);
            }
            Gap(content, 12);
            content.Children.Add(Pill("Toggle mute", flow == 0 ? "volume-off" : "mic-off", () => { Native.ToggleMute(flow); CloseOverlay(); }));
            ShowOverlay(flow == 0 ? "Sound output" : "Microphone input", content);
        }
        catch (Exception e) { ShowIslandError("Sound devices", e.Message); }
    }
    void ShowIslandError(string title, string message) => ShowOverlay(title, T(message, 14, Muted));
    void ChooseNetwork(WifiNetwork network)
    {
        if (deviceOperation)
            return;
        if (network.Connected)
        {
            _ = DisconnectNetwork(network);
            return;
        }
        if (network.Profile.Length > 0 || !network.Secured || network.Auth == 10)
        {
            _ = ConnectNetwork(network, null);
            return;
        }
        if (network.Auth is not (4 or 7 or 9))
        {
            ShowIslandError("Managed network", "This enterprise or legacy network needs an existing Windows-managed authentication profile.");
            return;
        }
        RequestNetworkPassword(network);
    }
    void RequestNetworkPassword(WifiNetwork network)
    {
        if (deviceOperation)
            return;
        var password = new PasswordBox { Padding = new Thickness(16, 11, 16, 11), Background = Surface, Foreground = Fg, BorderThickness = new Thickness(0), MinHeight = 42 };
        ShowOverlay("Join " + network.Name, Col(T("Wi-Fi password", 14, Muted), password), () => { string value = password.Password; if (value.Length < (network.Auth == 9 ? 1 : 8) || value.Length > 63) throw new Exception("Enter a valid Wi-Fi password for this network."); _ = ConnectNetwork(network, value); }, "Connect");
    }
    async Task ConnectNetwork(WifiNetwork network, string? password)
    {
        deviceOperation = true;
        connectionMessage = "Connecting to " + network.Name + "…";
        connectionError = "";
        RenderTab();
        try
        {
            await Task.Run(() => WifiNative.Connect(network, password));
            bool connected = false;
            for (int i = 0; i < 12; i++)
            {
                await Task.Delay(500);
                var state = await Task.Run(() => WifiNative.Read());
                connectedSsid = state.Connected;
                wifiNetworks = state.Networks;
                if (connectedSsid == network.Name)
                {
                    connected = true;
                    break;
                }
            }
            connectionMessage = connected ? "Connected to " + network.Name : "Connection requested; Windows has not confirmed a connection yet.";
        }
        catch (Exception e) { connectionError = e.Message; connectionMessage = ""; }
        finally { deviceOperation = false; if (expanded && tab is "Connections" or "Control" or "Dashboard") RenderTab(); }
    }
    async Task DisconnectNetwork(WifiNetwork network)
    {
        if (deviceOperation)
            return;
        deviceOperation = true;
        connectionMessage = "Disconnecting…";
        RenderTab();
        try
        {
            await Task.Run(() => WifiNative.Disconnect(network.Adapter));
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(250);
                var state = await Task.Run(() => WifiNative.Read());
                connectedSsid = state.Connected;
                wifiNetworks = state.Networks;
                if (connectedSsid != network.Name)
                    break;
            }
            connectionMessage = connectedSsid != network.Name ? "Disconnected" : "Disconnection requested; waiting for Windows confirmation.";
        }
        catch (Exception e) { connectionError = e.Message; }
        finally { deviceOperation = false; if (expanded && tab == "Connections") RenderTab(); }
    }
    async Task ConnectBluetooth(BluetoothPeer peer)
    {
        if (deviceOperation)
            return;
        deviceOperation = true;
        connectionError = "";
        connectionMessage = (peer.Connected ? "Disconnecting " : peer.Paired ? "Connecting " : "Pairing ") + peer.Name + "…";
        RenderTab();
        try
        {
            if (!peer.Paired)
            {
                var owner = new WindowInteropHelper(this).Handle;
                await Task.Run(() => BluetoothNative.Pair(peer.Address, owner));
            }
            await Task.Run(() => BluetoothNative.SetConnection(peer.Address, !peer.Connected));
            bool confirmed = false;
            for (int i = 0; i < 12; i++)
            {
                await Task.Delay(500);
                bluetoothPeers = await Task.Run(() => BluetoothNative.Read());
                devices = bluetoothPeers.Select(d => (d.Name, d.Connected ? "Connected" : "Not connected")).ToList();
                var current = bluetoothPeers.FirstOrDefault(d => d.Address == peer.Address);
                if (current != null && current.Connected != peer.Connected)
                {
                    confirmed = true;
                    break;
                }
            }
            connectionMessage = confirmed ? (peer.Connected ? "Disconnected from " : "Connected to ") + peer.Name : "Profile request sent; the device has not confirmed the requested connection state.";
        }
        catch (Exception e) { connectionError = e.Message; connectionMessage = ""; }
        finally { deviceOperation = false; if (expanded && tab is "Connections" or "Control" or "Dashboard") RenderTab(); }
    }
}
