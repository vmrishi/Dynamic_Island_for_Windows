namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
public partial class Island
{
    static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(15) };
    async Task SetCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            weatherError = "Enter a city name.";
            RenderTab();
            return;
        }
        try
        {
            status.Text = "Finding your city…";
            using var doc = JsonDocument.Parse(await http.GetStringAsync("https://geocoding-api.open-meteo.com/v1/search?name=" + Uri.EscapeDataString(city.Trim()) + "&count=1&language=en&format=json"));
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                throw new Exception("No city found. Try a nearby city or include its full name.");
            var r = results[0];
            S.Latitude = r.GetProperty("latitude").GetDouble();
            S.Longitude = r.GetProperty("longitude").GetDouble();
            S.City = r.GetProperty("name").GetString() + ", " + (r.TryGetProperty("admin1", out var a) ? a.GetString() + ", " : "") + r.GetProperty("country").GetString();
            Save();
            await RefreshWeather();
        }
        catch (Exception e) { weatherError = e.Message; RenderTab(); }
    }
    async Task RefreshWeather()
    {
        if (S.Latitude == null || S.Longitude == null)
            return;
        try
        {
            string lat = S.Latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), lon = S.Longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m,surface_pressure&hourly=temperature_2m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,uv_index_max,sunrise,sunset&timezone=auto&forecast_days=7";
            using var doc = JsonDocument.Parse(await http.GetStringAsync(url));
            weather = doc.RootElement.Clone();
            weatherError = "";
        }
        catch (Exception e) { weatherError = "Forecast unavailable: " + e.Message; }
        if (expanded && (tab == "Weather" || tab == "Dashboard"))
            RenderTab();
    }
    static string WeatherLabel(int code) => code switch { 0 => "Clear skies", 1 or 2 => "Partly cloudy", 3 => "Overcast", 45 or 48 => "Foggy", >= 51 and <= 57 => "Drizzle", >= 61 and <= 67 => "Rain", >= 71 and <= 77 => "Snow", >= 80 and <= 82 => "Rain showers", 85 or 86 => "Snow showers", >= 95 => "Thunderstorms", _ => "Conditions unavailable" };
    static string WeatherIcon(int code) => code switch { 0 => "☀", 1 or 2 => "⛅", >= 51 and <= 67 => "☂", >= 71 and <= 77 => "❄", >= 95 => "ϟ", _ => "☁" };

    bool connectionsBusy;
    async Task RefreshConnections()
    {
        if (connectionsBusy)
            return;
        connectionsBusy = true;
        connectionError = "";
        try
        {
            status.Text = "Reading Windows connections…";
            try
            {
                var r = await Native.Broker("radios");
                radios.Clear();
                foreach (var radio in Elements(r))
                    radios[radio.GetProperty("kind").GetString()!] = radio.GetProperty("state").GetString() ?? "Unknown";
            }
            catch (Exception e) { connectionError = e.Message; }
            try
            {
                var inf = await Native.Run("netsh.exe", "wlan", "show", "interfaces");
                wifiDetails = inf.Code == 0 ? inf.Output : "";
            }
            catch { wifiDetails = ""; }
            try
            {
                var r = await Native.Broker("bluetooth");
                devices = Elements(r).Select(x => (x.GetProperty("FriendlyName").GetString() ?? "Bluetooth device", x.GetProperty("Status").GetString() ?? "Unknown")).ToList();
            }
            catch (Exception e) { devices.Clear(); if (connectionTab == "Bluetooth") connectionError = e.Message; }
            if (connectionTab == "Wi-Fi")
            {
                try
                {
                    var r = await Native.Run("netsh.exe", "wlan", "show", "networks", "mode=bssid");
                    networks.Clear();
                    string? ssid = null;
                    int signal = 0;
                    foreach (var line in r.Output.Split('\n'))
                    {
                        var m = Regex.Match(line, @"^\s*SSID\s+\d+\s*:\s*(.*)");
                        if (m.Success)
                        {
                            if (!string.IsNullOrWhiteSpace(ssid))
                                networks.Add((ssid, signal));
                            ssid = m.Groups[1].Value.Trim();
                            signal = 0;
                        }
                        var value = Regex.Match(line, @"(?:Signal|信号)\s*:\s*(\d+)%");
                        if (value.Success)
                            signal = Math.Max(signal, int.Parse(value.Groups[1].Value));
                    }
                    if (!string.IsNullOrWhiteSpace(ssid))
                        networks.Add((ssid, signal));
                    if (r.Code != 0 || networks.Count == 0)
                        connectionError = r.Output.Trim();
                }
                catch (Exception e) { connectionError = e.Message; }
            }
        }
        finally { connectionsBusy = false; }
        if (expanded && tab is "Connections" or "Control" or "Dashboard")
            RenderTab();
    }
    static IEnumerable<JsonElement> Elements(JsonElement e) => e.ValueKind == JsonValueKind.Array ? e.EnumerateArray().ToArray() : e.ValueKind == JsonValueKind.Object ? new[] { e } : Array.Empty<JsonElement>();
    async Task ToggleRadio(string kind)
    {
        try
        {
            status.Text = "Changing " + kind + " radio…";
            if (!radios.ContainsKey(kind))
            {
                var r = await Native.Broker("radios");
                foreach (var x in Elements(r))
                    radios[x.GetProperty("kind").GetString()!] = x.GetProperty("state").GetString() ?? "Unknown";
            }
            if (!radios.ContainsKey(kind))
                throw new Exception("No " + kind + " radio detected. Check Windows settings.");
            await Native.Broker((kind == "WiFi" ? "wifi" : "bluetooth") + (radios[kind] == "On" ? "-off" : "-on"));
            await RefreshConnections();
        }
        catch (Exception e) { Notify("Radio control unavailable", e.Message); }
    }
    async Task JoinNetwork(string ssid)
    {
        try
        {
            var r = await Native.Run("netsh.exe", "wlan", "connect", "name=" + ssid);
            Notify(r.Code == 0 ? "Connection requested" : "Windows Wi-Fi setup needed", r.Output.Trim(), false);
            if (r.Code != 0)
                TryOpen("ms-settings:network-wifi");
            else
                await RefreshConnections();
        }
        catch (Exception e) { Notify("Wi-Fi unavailable", e.Message); }
    }


    FrameworkElement Spark(List<double> values, Brush color)
    {
        var canvas = new Canvas { Height = 72, ClipToBounds = true };
        void Draw()
        {
            canvas.Children.Clear();
            if (values.Count < 2)
                return;
            double max = Math.Max(1, values.Max());
            var poly = new Polyline { Stroke = color, StrokeThickness = 2 };
            for (int i = 0; i < values.Count; i++)
                poly.Points.Add(new Point(i * canvas.ActualWidth / (values.Count - 1), 68 - values[i] / max * 60));
            canvas.Children.Add(poly);
        }
        canvas.SizeChanged += (_, _) => Draw();
        return canvas;
    }
    static string Bytes(double value) => value >= 1048576 ? $"{value / 1048576:0.0} MB" : value >= 1024 ? $"{value / 1024:0.0} KB" : $"{value:0} B";
}
