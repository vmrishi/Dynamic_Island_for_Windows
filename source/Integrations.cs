namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
public partial class Island
{
    System.Windows.FrameworkElement ActivityRing(double size, double progress, System.Windows.Media.Brush color)
    {
        var grid = new Grid { Width = size, Height = size };
        grid.Children.Add(new System.Windows.Shapes.Ellipse { Stroke = color, StrokeThickness = 11, Opacity = .18 });
        double r = (size - 11) / 2, c = size / 2, angle = Math.Clamp(progress, 0, .99999) * Math.PI * 2;
        var figure = new System.Windows.Media.PathFigure { StartPoint = new Point(c, 5.5) };
        figure.Segments.Add(new System.Windows.Media.ArcSegment(new Point(c + r * Math.Sin(angle), c - r * Math.Cos(angle)), new Size(r, r), 0, angle > Math.PI, System.Windows.Media.SweepDirection.Clockwise, true));
        var geometry = new System.Windows.Media.PathGeometry();
        geometry.Figures.Add(figure);
        if (progress > 0)
            grid.Children.Add(new System.Windows.Shapes.Path { Data = geometry, Stroke = color, StrokeThickness = 11, StrokeStartLineCap = System.Windows.Media.PenLineCap.Round, StrokeEndLineCap = System.Windows.Media.PenLineCap.Round });
        return grid;
    }
    System.Windows.FrameworkElement AppIcon(string path)
    {
        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (icon != null)
                return new Image { Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()), Width = 32, Height = 32, HorizontalAlignment = HorizontalAlignment.Left };
        }
        catch { }
        return Icon("external", 27, Accent);
    }
    void AddInstalledApp()
    {
        var list = new ListBox { Height = 320, DisplayMemberPath = "Key" };
        var apps = new List<KeyValuePair<string, string>>();
        var type = Type.GetTypeFromProgID("Shell.Application");
        if (type == null)
            throw new Exception("Windows app list unavailable.");
        dynamic shellApp = Activator.CreateInstance(type)!;
        dynamic folder = shellApp.NameSpace("shell:AppsFolder");
        foreach (dynamic item in folder.Items())
        {
            string name = item.Name;
            string path = item.Path;
            if (!string.IsNullOrWhiteSpace(path))
                apps.Add(new(name, path.Contains("!") ? "shell:AppsFolder\\" + path : path));
        }
        list.ItemsSource = apps.OrderBy(x => x.Key).ToList();
        var search = Input("", "Search installed apps");
        search.TextChanged += (_, _) => list.ItemsSource = apps.Where(x => x.Key.Contains(search.Text, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Key).ToList();
        ShowDialog("Add application", Col(search, list), () => { if (list.SelectedItem is not KeyValuePair<string, string> chosen) throw new Exception("Select an application."); if (!D.Shelf.Contains(chosen.Value)) D.Shelf.Add(chosen.Value); Save(); RenderTab(); });
    }
    string ConnectedWifi() => connectedSsid;
    string BluetoothSummary() => devices.Any(x => x.State == "Connected") ? string.Join(", ", devices.Where(x => x.State == "Connected").Select(x => x.Name)) : "No connected device";
    bool notificationBusy;
    async Task RefreshSystemNotifications()
    {
        if (notificationBusy)
            return;
        notificationBusy = true;
        try
        {
            var result = await Native.Broker("notifications");
            foreach (var item in Elements(result))
            {
                string id = "Windows · " + item.GetProperty("id").GetString();
                var title = item.GetProperty("title").GetString() ?? "Notification";
                var detail = item.GetProperty("detail").GetString() ?? "";
                if (!D.SeenWindowsNotices.Contains(id))
                {
                    D.SeenWindowsNotices.Add(id);
                    D.Notices.Insert(0, new Notice { Title = title, Detail = detail });
                }
            }
            D.Notices = D.Notices.Take(200).ToList();
            Save();
            if (tab == "Notifications" && expanded)
                RenderTab();
        }
        catch (Exception ex) { S.SystemNotifications = false; Save(); Notify("Windows notification access unavailable", ex.Message + " Enable access in Windows privacy settings, then try again.", false); }
        finally { notificationBusy = false; }
    }
    readonly List<(string Role, string Text)> chat = new(); string chatDraft = "", chatError = ""; bool chatBusy;

    async Task SendChat()
    {
        if (chatBusy || string.IsNullOrWhiteSpace(chatDraft))
            return;
        var text = chatDraft.Trim();
        chat.Add(("user", text));
        chatDraft = "";
        chatBusy = true;
        chatError = "";
        RenderTab();
        try
        {
            if (!Uri.TryCreate(S.AiEndpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                throw new Exception("Set a valid HTTP endpoint in AI settings.");
            var payload = JsonSerializer.Serialize(new
            {
                model = S.AiModel,
                stream = false,
                messages = chat.Select(x => new { role = x.Role, content = x.Text })
            });
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            using var response = await client.PostAsync(uri, new StringContent(payload, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            chat.Add(("assistant", doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? ""));
        }
        catch (Exception ex) { chatError = "Could not reach the model: " + ex.Message; chat.RemoveAt(chat.Count - 1); chatDraft = text; }
        finally { chatBusy = false; if (tab == "AI Chat" && expanded) RenderTab(); }
    }
}
