namespace OrbitIsland;

using System;
using System.Security;
using System.Text;
public static class WifiProfiles
{
    public static string Build(string ssid, string? password, uint auth, uint cipher, bool secured)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(ssid);
        if (bytes.Length < 1 || bytes.Length > 32)
            throw new ArgumentException("Wi-Fi names must contain 1–32 UTF-8 bytes.");
        string authentication = secured ? auth switch
        {
            4 => "WPAPSK",
            7 => "WPA2PSK",
            9 => "WPA3SAE",
            10 => "OWE",
            _ => throw new NotSupportedException("This enterprise or legacy network needs an existing Windows-managed profile. Orbit cannot create its authentication configuration.")
        } : "open";
        bool key = secured && auth != 10;
        if (key && (string.IsNullOrEmpty(password) || password.Length < (auth == 9 ? 1 : 8) || password.Length > 63))
            throw new ArgumentException("Enter a valid personal-network password (8–63 characters for WPA/WPA2; 1–63 for WPA3-SAE).");
        string encryption = secured ? (cipher == 2 ? "TKIP" : "AES") : "none";
        string shared = key ? $"<sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{SecurityElement.Escape(password)}</keyMaterial></sharedKey>" : "";
        return $"<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{SecurityElement.Escape(ssid)}</name><SSIDConfig><SSID><hex>{Convert.ToHexString(bytes)}</hex><name>{SecurityElement.Escape(ssid)}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>manual</connectionMode><MSM><security><authEncryption><authentication>{authentication}</authentication><encryption>{encryption}</encryption><useOneX>false</useOneX></authEncryption>{shared}</security></MSM></WLANProfile>";
    }
}
