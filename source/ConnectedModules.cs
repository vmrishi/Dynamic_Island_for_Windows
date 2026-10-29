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
            double latitude = r.GetProperty("latitude").GetDouble(), longitude = r.GetProperty("longitude").GetDouble();
            string label = r.GetProperty("name").GetString() + ", " + (r.TryGetProperty("admin1", out var a) ? a.GetString() + ", " : "") + r.GetProperty("country").GetString();
            var forecast = await FetchForecast(latitude, longitude);
            S.Latitude = latitude;
            S.Longitude = longitude;
            S.City = label;
            weather = forecast;
            weatherError = "";
            Save();
            if (expanded && tab is "Weather" or "Dashboard")
                RenderTab();
        }
        catch (Exception e) { weatherError = e.Message; RenderTab(); }
    }
    async Task RefreshWeather()
    {
        if (S.Latitude == null || S.Longitude == null)
            return;
        try
        {
            weather = await FetchForecast(S.Latitude.Value, S.Longitude.Value);
            weatherError = "";
        }
        catch (Exception e) { weatherError = "Forecast unavailable: " + e.Message; }
        if (expanded && (tab == "Weather" || tab == "Dashboard"))
            RenderTab();
    }
    static async Task<JsonElement> FetchForecast(double latitude, double longitude)
    {
        string lat = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture), lon = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        using var doc = JsonDocument.Parse(await http.GetStringAsync($"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,is_day,wind_speed_10m,surface_pressure&hourly=temperature_2m,weather_code,is_day,uv_index&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,uv_index_max,sunrise,sunset&timezone=auto&forecast_days=7"));
        return doc.RootElement.Clone();
    }
    static string WeatherLabel(int code) => code switch { 0 => "Clear skies", 1 or 2 => "Partly cloudy", 3 => "Overcast", 45 or 48 => "Foggy", >= 51 and <= 57 => "Drizzle", >= 61 and <= 67 => "Rain", >= 71 and <= 77 => "Snow", >= 80 and <= 82 => "Rain showers", 85 or 86 => "Snow showers", >= 95 => "Thunderstorms", _ => "Conditions unavailable" };
    static string WeatherIcon(int code) => code switch { 0 => "☀", 1 or 2 => "⛅", >= 51 and <= 67 => "☂", >= 71 and <= 77 => "❄", >= 95 => "ϟ", _ => "☁" };

    bool connectionsBusy;
    List<WifiNetwork> wifiNetworks = new(); List<BluetoothPeer> bluetoothPeers = new();
    string connectedSsid = "Not connected", connectionMessage = ""; bool deviceOperation;
    async Task RefreshConnections(bool discover = false)
    {
        if (connectionsBusy)
            return;
        connectionsBusy = true;
        connectionError = "";
        try
        {
            try
            {
                radios = await RadioControls.Read();
            }
            catch (Exception e) { connectionError = e.Message; }
            try
            {
                var snapshot = await Task.Run(() => WifiNative.Read(discover));
                connectedSsid = snapshot.Connected;
                wifiNetworks = snapshot.Networks;
            }
            catch (Exception e) { connectedSsid = "Unavailable"; wifiNetworks.Clear(); if (connectionTab == "Wi-Fi") connectionError = e.Message; }
            try
            {
                bluetoothPeers = await Task.Run(() => BluetoothNative.Read(discover && connectionTab == "Bluetooth"));
                devices = bluetoothPeers.Select(d => (d.Name, d.Connected ? "Connected" : "Not connected")).ToList();
            }
            catch (Exception e) { bluetoothPeers.Clear(); devices.Clear(); if (connectionTab == "Bluetooth") connectionError = e.Message; }
        }
        finally { connectionsBusy = false; }
        if (expanded && tab is "Connections" or "Control" or "Dashboard")
            RenderTab();
    }
    static IEnumerable<JsonElement> Elements(JsonElement e) => e.ValueKind == JsonValueKind.Array ? e.EnumerateArray().ToArray() : e.ValueKind == JsonValueKind.Object ? new[] { e } : Array.Empty<JsonElement>();
    bool radioBusy;
    async Task ToggleRadio(string kind)
    {
        if (radioBusy)
            return;
        radioBusy = true;
        try
        {
            await RadioControls.Toggle(kind);
            await RefreshConnections();
        }
        catch (Exception e) { ShowIslandError("Radio control", e.Message); }
        finally { radioBusy = false; if (expanded && !modalOpen) RenderTab(); }
    }
    async Task JoinNetwork(string ssid)
    {
        var network = wifiNetworks.FirstOrDefault(n => n.Name == ssid);
        if (network == null)
        {
            connectionError = "Refresh the network list and try again.";
            RenderTab();
            return;
        }
        ChooseNetwork(network);
        await Task.CompletedTask;
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
