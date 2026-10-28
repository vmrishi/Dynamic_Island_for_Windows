namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
public partial class Island
{
    void Weather()
    {
        if (weather == null)
        {
            var empty = Col(Icon("Weather", 54, Muted));
            Gap(empty, 15);
            empty.Children.Add(T("Your local forecast", 24, Fg, true));
            Gap(empty, 8);
            empty.Children.Add(T(weatherError.Length > 0 ? weatherError : "Choose your location to see the weather here.", 14, Muted));
            Gap(empty, 16);
            empty.Children.Add(Pill("Choose location", "pin", () => OpenSettingsPage("Weather"), true));
            body.Children.Add(Center(empty));
            return;
        }
        var w = weather.Value;
        var cur = w.GetProperty("current");
        var daily = w.GetProperty("daily");
        var main = new DockPanel();
        var top = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        top.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(1.3, GridUnitType.Star)
        });
        top.ColumnDefinitions.Add(new());
        var overview = new Grid();
        overview.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(90)
        });
        overview.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        overview.ColumnDefinitions.Add(new());
        Put(overview, Icon(WeatherVector(cur.GetProperty("weather_code").GetInt32()), 64, Brushes.White), 0);
        Put(overview, T(cur.GetProperty("temperature_2m").GetDouble().ToString("0") + "°", 52, Brushes.White, true), 1);
        var place = Col(T(WeatherLabel(cur.GetProperty("weather_code").GetInt32()), 17, Brushes.White, true), Line(S.City, 13, Brush("#C5C8DC")), T($"H {daily.GetProperty("temperature_2m_max")[0].GetDouble():0}° · L {daily.GetProperty("temperature_2m_min")[0].GetDouble():0}° · Feels {cur.GetProperty("apparent_temperature").GetDouble():0}°", 12, Brush("#C5C8DC")));
        place.Margin = new Thickness(12, 0, 12, 0);
        Put(overview, place, 2);
        Put(top, overview, 0);
        var chips = new UniformGrid { Columns = 3 };
        foreach (var metric in new[] { ("water", cur.GetProperty("relative_humidity_2m").GetDouble().ToString("0") + "%"), ("wifi", cur.GetProperty("wind_speed_10m").GetDouble().ToString("0") + " km/h"), ("sun", "UV max " + OptionalNumber(daily, "uv_index_max", 0)), ("System", OptionalNumber(cur, "surface_pressure") + " hPa"), ("sun", OptionalTime(daily, "sunrise")), ("moon", OptionalTime(daily, "sunset")) })
        {
            var chip = new Border { Background = Brush("#121835"), CornerRadius = new CornerRadius(15), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(3), Child = Row(Icon(metric.Item1, 14, Brush("#C5C8DC")), Space(6), T(metric.Item2, 11, Brushes.White, true)) };
            chips.Children.Add(chip);
        }
        Put(top, chips, 1);
        DockPanel.SetDock(top, Dock.Top);
        main.Children.Add(top);
        var forecasts = Columns(1, 1);
        var hourly = w.GetProperty("hourly");
        var hours = new UniformGrid { Columns = 7 };
        var localNow = DateTime.Parse(cur.GetProperty("time").GetString()!);
        int first = 0;
        for (int i = 0; i < hourly.GetProperty("time").GetArrayLength(); i++)
        if (DateTime.Parse(hourly.GetProperty("time")[i].GetString()!) >= localNow)
        {
            first = i;
            break;
        }
        for (int i = 0; i < 7; i++)
        {
            int index = Math.Min(first + i * 2, hourly.GetProperty("time").GetArrayLength() - 1);
            var date = DateTime.Parse(hourly.GetProperty("time")[index].GetString()!);
            hours.Children.Add(ForecastColumn(i == 0 ? "Now" : date.ToString("H:mm"), WeatherVector(hourly.GetProperty("weather_code")[index].GetInt32()), hourly.GetProperty("temperature_2m")[index].GetDouble().ToString("0") + "°", null));
        }
        var days = new UniformGrid { Columns = 7 };
        for (int i = 0; i < 7; i++)
            days.Children.Add(ForecastColumn(i == 0 ? "Today" : DateTime.Parse(daily.GetProperty("time")[i].GetString()!).ToString("ddd"), WeatherVector(daily.GetProperty("weather_code")[i].GetInt32()), daily.GetProperty("temperature_2m_max")[i].GetDouble().ToString("0") + "°", daily.GetProperty("temperature_2m_min")[i].GetDouble().ToString("0") + "°"));
        var hourlyCard = PanelCard(hours, 12);
        hourlyCard.Background = Brush("#151B3B");
        hourlyCard.Margin = new Thickness(0, 0, 12, 0);
        var dailyCard = PanelCard(days, 12);
        dailyCard.Background = Brush("#1C2346");
        Put(forecasts, hourlyCard, 0);
        Put(forecasts, dailyCard, 1);
        main.Children.Add(forecasts);
        var background = PanelCard(main, 20);
        background.Background = new LinearGradientBrush(Color.FromRgb(13, 18, 44), Color.FromRgb(43, 47, 87), 35);
        body.Children.Add(background);
    }
    static string WeatherVector(int code) => code == 0 ? "sun" : code is 1 or 2 ? "Weather" : "Weather";
    string OptionalNumber(JsonElement obj, string name, int? index = null)
    {
        try
        {
            return (index == null ? obj.GetProperty(name) : obj.GetProperty(name)[index.Value]).GetDouble().ToString("0");
        }
        catch { return "—"; }
    }
    string OptionalTime(JsonElement obj, string name)
    {
        try
        {
            return DateTime.Parse(obj.GetProperty(name)[0].GetString()!).ToString("h:mm tt").ToLowerInvariant();
        }
        catch { return "—"; }
    }
    FrameworkElement ForecastColumn(string label, string icon, string high, string? low)
    {
        var p = new Grid();
        p.RowDefinitions.Add(new()
        {
            Height = GridLength.Auto
        });
        p.RowDefinitions.Add(new());
        p.RowDefinitions.Add(new()
        {
            Height = new GridLength(42)
        });
        if (low != null)
            p.RowDefinitions.Add(new()
            {
                Height = new GridLength(36)
            });
        var title = Line(label, 11, Brush("#E3E5F3"));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        p.Children.Add(title);
        var symbol = Icon(icon, 24, Brushes.White);
        Grid.SetRow(symbol, 1);
        p.Children.Add(symbol);
        var temp = T(high, 13, Brushes.White, true);
        temp.HorizontalAlignment = HorizontalAlignment.Center;
        Grid.SetRow(temp, 2);
        p.Children.Add(temp);
        if (low != null)
        {
            var l = T(low, 12, Brush("#939AB8"), true);
            l.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetRow(l, 3);
            p.Children.Add(l);
        }
        return p;
    }
    void ScreenTime()
    {
        if (!S.TrackApps)
        {
            Empty("Know where your time goes", "Record foreground app usage locally on this PC. Tracking starts when enabled.", Pill("Enable tracking", "Screen Time", () => { S.TrackApps = true; Save(); RenderTab(); }, true));
            return;
        }
        var usage = usageWeek ? D.Days.Where(x => DateTime.TryParse(x.Key, out var day) && day >= DateTime.Today.AddDays(-6)).SelectMany(x => x.Value.Apps).GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Sum(y => y.Value)) : D.Today.Apps;
        var categories = usage.GroupBy(x => UsageCategory(x.Key)).Select(x => (Name: x.Key, Seconds: x.Sum(y => y.Value))).OrderByDescending(x => x.Seconds).ToList();
        double total = usage.Values.Sum();
        var grid = Columns(1.15, 2);
        body.Children.Add(grid);
        var left = new DockPanel { Margin = new Thickness(0, 0, 24, 0) };
        var footer = Line($"{D.Today.AppSwitches} app switches · now: {lastForeground ?? "Idle"}", 12, Muted);
        DockPanel.SetDock(footer, Dock.Bottom);
        left.Children.Add(footer);
        var summary = Columns(1, 1.1);
        var ring = new Grid { Width = 138, Height = 138 };
        ring.Children.Add(new Ellipse { Stroke = CardBorder, StrokeThickness = 17 });
        double start = -90;
        foreach (var c in categories)
        {
            double sweep = c.Seconds / Math.Max(1, total) * 360;
            ring.Children.Add(RingArc(138, start, sweep, CategoryColor(c.Name), 17));
            start += sweep;
        }
        var center = Col(T(UsageDuration(total), 23, Fg, true), T(usageWeek ? "7 days" : "today", 11, Muted));
        foreach (var t in center.Children.OfType<TextBlock>())
            t.HorizontalAlignment = HorizontalAlignment.Center;
        ring.Children.Add(Center(center));
        Put(summary, ring, 0);
        var legend = Col();
        foreach (var c in categories)
        {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 7) };
            var time = Line(UsageDuration(c.Seconds), 12, Fg, true);
            DockPanel.SetDock(time, Dock.Right);
            row.Children.Add(time);
            row.Children.Add(Row(new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(3), Background = CategoryColor(c.Name) }, Space(7), Line(c.Name, 12, Muted)));
            legend.Children.Add(row);
        }
        if (categories.Count == 0)
            legend.Children.Add(T("No usage recorded yet", 13, Muted));
        Put(summary, legend, 1);
        DockPanel.SetDock(summary, Dock.Top);
        left.Children.Add(summary);
        var bars = new UniformGrid { Columns = 24, VerticalAlignment = VerticalAlignment.Bottom, Height = 72, Margin = new Thickness(0, 12, 0, 12) };
        double max = Math.Max(1, D.Today.Hourly.Values.DefaultIfEmpty(1).Max());
        for (int i = 0; i < 24; i++)
        {
            D.Today.Hourly.TryGetValue(i, out var value);
            var b = new Border { Background = Accent, CornerRadius = new CornerRadius(3, 3, 0, 0), Height = Math.Max(1, value / max * 66), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 2, 0), ToolTip = $"{i:00}:00 · {UsageDuration(value)}" };
            bars.Children.Add(b);
        }
        left.Children.Add(bars);
        Put(grid, left, 0);
        var right = new DockPanel();
        var top = new DockPanel { Margin = new Thickness(0, 0, 0, 17) };
        var filters = Row(Btn("Today", () => { usageWeek = false; RenderTab(); }, !usageWeek), Btn("7 days", () => { usageWeek = true; RenderTab(); }, usageWeek));
        DockPanel.SetDock(filters, Dock.Right);
        top.Children.Add(filters);
        top.Children.Add(Row(Icon("chart", 17, Muted), Space(8), T("MOST USED", 12, Muted, true)));
        DockPanel.SetDock(top, Dock.Top);
        right.Children.Add(top);
        var bottom = Row(Pill("Export CSV", "download", ExportScreenTime), Pill("Pause", "pause", () => { S.TrackApps = false; Save(); RenderTab(); }));
        bottom.Margin = new Thickness(0, 10, 0, 0);
        DockPanel.SetDock(bottom, Dock.Bottom);
        right.Children.Add(bottom);
        var list = Col();
        foreach (var app in usage.OrderByDescending(x => x.Value))
        {
            var row = Columns(1.3, 1.8, .55);
            Put(row, Line(app.Key, 14, Fg, true), 0);
            Put(row, Progress(app.Value / Math.Max(1, usage.Values.DefaultIfEmpty(1).Max()), Accent, 7), 1);
            var duration = Line(UsageDuration(app.Value), 12, Muted);
            duration.HorizontalAlignment = HorizontalAlignment.Right;
            Put(row, duration, 2);
            row.Margin = new Thickness(0, 0, 0, 14);
            list.Children.Add(row);
        }
        right.Children.Add(Scroll(list));
        Put(grid, right, 1);
    }
    string? lastForeground;
    static string UsageDuration(double seconds) => seconds >= 3600 ? $"{(int)(seconds / 3600)}h {(int)(seconds % 3600 / 60)}m" : $"{(int)(seconds / 60)}m";
    static string UsageCategory(string app)
    {
        var name = app.ToLowerInvariant();
        if (new[] { "chrome", "edge", "firefox", "brave", "opera" }.Any(name.Contains))
            return "Browsing";
        if (new[] { "code", "studio", "rider", "terminal", "powershell", "cmd" }.Any(name.Contains))
            return "Development";
        if (new[] { "photoshop", "illustrator", "figma", "blender", "premiere", "paint" }.Any(name.Contains))
            return "Creativity";
        if (new[] { "spotify", "vlc", "steam", "game", "netflix" }.Any(name.Contains))
            return "Entertainment";
        if (new[] { "word", "excel", "notion", "outlook", "onenote", "codex" }.Any(name.Contains))
            return "Productivity";
        return "Other";
    }
    Brush CategoryColor(string category) => Brush(category switch { "Browsing" => "#1988FF", "Development" => "#27CF63", "Creativity" => "#FFA62D", "Entertainment" => "#FF3C68", "Productivity" => "#C453EB", _ => "#68686E" });
    FrameworkElement RingArc(double size, double startDegrees, double sweep, Brush color, double thickness)
    {
        double r = (size - thickness) / 2, c = size / 2, a = startDegrees * Math.PI / 180, b = (startDegrees + Math.Clamp(sweep, .01, 359.99)) * Math.PI / 180;
        var figure = new PathFigure { StartPoint = new Point(c + r * Math.Cos(a), c + r * Math.Sin(a)) };
        figure.Segments.Add(new ArcSegment(new Point(c + r * Math.Cos(b), c + r * Math.Sin(b)), new Size(r, r), 0, sweep > 180, SweepDirection.Clockwise, true));
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return new System.Windows.Shapes.Path { Data = geometry, Stroke = color, StrokeThickness = thickness };
    }
    void DayProgress()
    {
        var panel = Col();
        body.Children.Add(panel);
        double progress = Store.DayProgress(DateTime.Now, S.WakeHour, S.BedHour);
        panel.Children.Add(Row(T(progress.ToString("P0"), 43, Fg, true), Space(15), T("of your day · " + (progress >= 1 ? "past bedtime, get some rest  ☾" : "make time for what matters"), 17, Muted)));
        Gap(panel, 10);
        var bar = Progress(progress, new LinearGradientBrush(Color.FromRgb(22, 57, 87), Color.FromRgb(35, 108, 181), 0), 68);
        panel.Children.Add(bar);
        var ticks = new UniformGrid { Columns = 6, Margin = new Thickness(0, 10, 0, 18) };
        for (int i = 0; i < 6; i++)
        {
            double hour = S.WakeHour + (S.BedHour - S.WakeHour) * i / 5d;
            var t = T($"{(int)hour}:00", 11, Muted);
            t.HorizontalAlignment = i == 0 ? HorizontalAlignment.Left : i == 5 ? HorizontalAlignment.Right : HorizontalAlignment.Center;
            ticks.Children.Add(t);
        }
        panel.Children.Add(ticks);
        var lower = Columns(1, 1, 1, 2.8);
        var now = DateTime.Now;
        var monday = now.Date.AddDays(-((7 + (int)now.DayOfWeek - 1) % 7));
        var month = new DateTime(now.Year, now.Month, 1);
        var year = new DateTime(now.Year, 1, 1);
        var data = new[] { ("WEEK", (now - monday).TotalDays / 7), ("MONTH", (now - month).TotalDays / DateTime.DaysInMonth(now.Year, now.Month)), ("YEAR", (now - year).TotalDays / (DateTime.IsLeapYear(now.Year) ? 366 : 365)) };
        for (int i = 0; i < 3; i++)
        {
            var p = Col(T(data[i].Item1, 12, Muted, true));
            Gap(p, 12);
            p.Children.Add(T(data[i].Item2.ToString("P0"), 25, Fg, true));
            p.Children.Add(Progress(data[i].Item2, Accent, 7));
            Put(lower, Card(p, 18), i);
        }
        var upcoming = Col(Row(Icon("clock", 16, Muted), Space(7), T("COMING UP", 12, Muted, true)));
        Gap(upcoming, 8);
        var events = D.Events.Where(e => e.Start > DateTime.Now && e.Start.Date == DateTime.Today).OrderBy(e => e.Start).Take(3).ToList();
        if (events.Count == 0)
            upcoming.Children.Add(T("Nothing else on the calendar today", 14, Muted));
        foreach (var e in events)
        {
            upcoming.Children.Add(Line(e.Start.ToString("HH:mm") + "  " + e.Title, 14));
            Gap(upcoming, 7);
        }
        Put(lower, upcoming, 3);
        panel.Children.Add(lower);
    }
    void Connections()
    {
        var dock = new DockPanel();
        body.Children.Add(dock);
        var bar = new DockPanel { Margin = new Thickness(0, 0, 0, 13) };
        var tabs = Row(Btn("Devices", async () => { connectionTab = "Bluetooth"; RenderTab(); await RefreshConnections(); }, connectionTab == "Bluetooth"), Btn("Networks", async () => { connectionTab = "Wi-Fi"; RenderTab(); await RefreshConnections(); }, connectionTab == "Wi-Fi"), Pill("Add device", "plus", () => TryOpen(connectionTab == "Wi-Fi" ? "ms-settings:network-wifi" : "ms-settings:bluetooth")));
        DockPanel.SetDock(tabs, Dock.Right);
        bar.Children.Add(tabs);
        var radio = Row(Pill("Wi-Fi · " + radios.GetValueOrDefault("WiFi", "Unknown"), "wifi", async () => await ToggleRadio("WiFi")), Pill("Bluetooth · " + radios.GetValueOrDefault("Bluetooth", "Unknown"), "bluetooth", async () => await ToggleRadio("Bluetooth")));
        bar.Children.Add(radio);
        DockPanel.SetDock(bar, Dock.Top);
        dock.Children.Add(bar);
        var foot = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
        var controls = Row(Pill("Refresh", "refresh", async () => await RefreshConnections()), Pill(connectionTab == "Wi-Fi" ? "Wi-Fi settings" : "Bluetooth settings", "settings", () => TryOpen(connectionTab == "Wi-Fi" ? "ms-settings:network-wifi" : "ms-settings:bluetooth")));
        DockPanel.SetDock(controls, Dock.Right);
        foot.Children.Add(controls);
        foot.Children.Add(T(connectionTab == "Wi-Fi" ? ConnectedWifi() : $"{devices.Count(d => d.State == "Connected")} connected · {devices.Count} paired", 12, Muted));
        DockPanel.SetDock(foot, Dock.Bottom);
        dock.Children.Add(foot);
        var content = new DockPanel();
        if (connectionError.Length > 0)
        {
            var error = T(connectionError[..Math.Min(220, connectionError.Length)], 11, Brush("#FFAA86"));
            error.Margin = new Thickness(0, 0, 0, 10);
            DockPanel.SetDock(error, Dock.Bottom);
            content.Children.Add(error);
        }
        var cards = new WrapPanel();
        if (connectionTab == "Bluetooth")
        foreach (var d in devices)
        {
            bool connected = d.State == "Connected";
            var p = Col(Row(Icon("headphones", 24, connected ? Fg : Muted), Space(9), Line(d.Name, 15, connected ? Fg : Muted, true)));
            Gap(p, 18);
            p.Children.Add(Row(new Ellipse { Width = 7, Height = 7, Fill = connected ? Brush("#27CF63") : Muted }, Space(7), T(d.State, 12, Muted)));
            Gap(p, 14);
            var connect = Btn(connected ? "Manage connection" : "Connect in Windows", () => TryOpen("ms-settings:bluetooth"));
            connect.Background = connected ? Brush(S.Light ? "#DDDEE2" : "#252525") : Accent;
            connect.Foreground = connected ? Fg : Brushes.White;
            connect.Margin = new Thickness(0);
            p.Children.Add(connect);
            var card = PanelCard(p, 16);
            card.Width = 235;
            card.Margin = new Thickness(0, 0, 12, 12);
            cards.Children.Add(card);
        }
        else
        foreach (var n in networks)
        {
            bool connected = n.Name == ConnectedWifi();
            var p = Col(Row(Icon("wifi", 24, connected ? Accent : Fg), Space(9), Line(n.Name, 15, Fg, true)));
            Gap(p, 14);
            p.Children.Add(T(connected ? "Connected" : n.Signal > 0 ? $"Signal {n.Signal}%" : "Available", 12, connected ? Accent : Muted));
            p.Children.Add(Progress(n.Signal / 100d, Accent, 5));
            Gap(p, 10);
            p.Children.Add(Pill(connected ? "Network settings" : "Connect", "chevron", () => { if (connected) TryOpen("ms-settings:network-wifi"); else _ = JoinNetwork(n.Name); }, !connected));
            var card = PanelCard(p, 16);
            card.Width = 235;
            card.Margin = new Thickness(0, 0, 12, 12);
            cards.Children.Add(card);
        }
        if (cards.Children.Count == 0)
            content.Children.Add(EmptyState(connectionTab == "Wi-Fi" ? "wifi" : "bluetooth", "No devices found", "Refresh to scan. Windows may require location access for Wi-Fi."));
        else
            content.Children.Add(Scroll(cards));
        dock.Children.Add(content);
    }
    void SystemPage()
    {
        var g = Columns(1, 1, 1, 1, 1);
        body.Children.Add(g);
        var cpu = MetricCard("cpu", "CPU", metrics.Cpu.ToString("0") + "%", Environment.ProcessorCount + " logical processors", Spark(cpuHistory, Accent));
        Put(g, cpu, 0);
        Put(g, MetricCard("memory", "Memory", metrics.Memory.ToString("0") + "%", $"{metrics.UsedGb:0.0} GB of {metrics.TotalGb:0.0} GB", Spark(memHistory, Brush("#BE64E6"))), 1);
        Put(g, MetricCard("wifi", "Network", Bytes(metrics.Rx) + "/s", $"↓ {Bytes(metrics.Rx)}/s ↑ {Bytes(metrics.Tx)}/s", Spark(rxHistory, Brush("#E6A637"))), 2);
        try
        {
            var drive = new DriveInfo(System.IO.Path.GetPathRoot(Environment.SystemDirectory)!);
            double used = 1 - drive.AvailableFreeSpace / (double)drive.TotalSize;
            Put(g, MetricCard("disk", "Disk", used.ToString("P0"), $"{drive.AvailableFreeSpace / 1073741824d:0} GB free of {drive.TotalSize / 1073741824d:0} GB", Progress(used, Brush("#5AC3E3"), 12)), 3);
        }
        catch { Put(g, MetricCard("disk", "Disk", "—", "Drive unavailable", new Border()), 3); }
        Put(g, MetricCard("temperature", "Temperature", "—", "No supported sensor", T("Unavailable on this hardware", 11, Muted)), 4);
    }
    Border MetricCard(string icon, string label, string value, string detail, UIElement chart)
    {
        var grid = new DockPanel();
        var graph = new Border { Child = chart, VerticalAlignment = VerticalAlignment.Bottom };
        DockPanel.SetDock(graph, Dock.Bottom);
        grid.Children.Add(graph);
        var top = Col();
        var symbol = Icon(icon, 21, Muted);
        symbol.HorizontalAlignment = HorizontalAlignment.Left;
        top.Children.Add(symbol);
        Gap(top, 14);
        top.Children.Add(T(label, 14, Muted));
        top.Children.Add(T(value, 29, Fg, true));
        top.Children.Add(Line(detail, 11, Muted));
        grid.Children.Add(top);
        var card = Card(grid, 16);
        return card;
    }
    void CalendarPage()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(Math.Min(365, (Width - 116) * .4))
        });
        grid.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        grid.ColumnDefinitions.Add(new());
        body.Children.Add(grid);
        var month = selectedDate.Date.AddDays(1 - selectedDate.Day);
        var left = Col();
        var head = new DockPanel();
        var buttons = Row(IconButton("back", () => { selectedDate = selectedDate.AddMonths(-1); RenderTab(); }, "Previous month", Muted, 15), Btn("Today", () => { selectedDate = DateTime.Today; RenderTab(); }), IconButton("chevron", () => { selectedDate = selectedDate.AddMonths(1); RenderTab(); }, "Next month", Muted, 15));
        DockPanel.SetDock(buttons, Dock.Right);
        head.Children.Add(buttons);
        head.Children.Add(Row(T(month.ToString("MMMM"), 18, Fg, true), Space(4), T(month.Year.ToString(), 16, Muted)));
        left.Children.Add(head);
        var days = new UniformGrid { Columns = 7, Margin = new Thickness(0, 9, 0, 0) };
        foreach (var label in new[] { "M", "T", "W", "T", "F", "S", "S" })
        {
            var t = T(label, 12, Muted, true);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            days.Children.Add(t);
        }
        var start = month.AddDays(-((int)month.DayOfWeek + 6) % 7);
        for (int i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            var button = Btn(date.Day.ToString(), () => { selectedDate = date; RenderTab(); }, date == selectedDate.Date);
            button.Padding = new Thickness(3, 5, 3, 5);
            button.Margin = new Thickness(3, 1, 3, 1);
            button.FontWeight = FontWeights.SemiBold;
            button.Background = date == DateTime.Today ? Brush("#FF4945") : date == selectedDate.Date ? Surface : Brushes.Transparent;
            button.Foreground = date == DateTime.Today ? Brushes.White : date.Month == month.Month ? Fg : Brush(S.Light ? "#A7A7B0" : "#505055");
            button.Height = 34;
            days.Children.Add(button);
        }
        left.Children.Add(days);
        Put(grid, left, 0);
        Put(grid, Divider(), 1);
        var right = new DockPanel();
        var add = Row(Pill("Event", "plus", AddEvent), Pill("Import .ics", "download", ImportIcs), Pill("Export .ics", "share", ExportIcs));
        DockPanel.SetDock(add, Dock.Bottom);
        right.Children.Add(add);
        var heading = T(selectedDate.ToString("dddd, d MMMM"), 20, Fg, true);
        heading.Margin = new Thickness(0, 0, 0, 19);
        DockPanel.SetDock(heading, Dock.Top);
        right.Children.Add(heading);
        var events = D.Events.Where(e => e.Start.Date == selectedDate.Date).OrderBy(e => e.Start).ToList();
        if (events.Count == 0)
            right.Children.Add(T("No events scheduled.", 15, Muted));
        else
        {
            var list = Col();
            foreach (var e in events)
            {
                var row = new DockPanel();
                var remove = IconButton("trash", () => { D.Events.Remove(e); Save(); RenderTab(); }, "Delete event", Muted, 16);
                DockPanel.SetDock(remove, Dock.Right);
                row.Children.Add(remove);
                row.Children.Add(Col(T(e.Title, 16, Fg, true), T(e.Start.ToString("h:mm tt").ToLowerInvariant(), 12, Accent)));
                var card = PanelCard(row, 14);
                card.Margin = new Thickness(0, 0, 0, 10);
                list.Children.Add(card);
            }
            right.Children.Add(Scroll(list));
        }
        Put(grid, right, 2);
    }
    string notificationFilter = "All";
    void Notifications()
    {
        var dock = new DockPanel();
        body.Children.Add(dock);
        var heading = new DockPanel { Margin = new Thickness(0, 0, 0, 15) };
        var actions = Row(Pill("Mark all read", "checks", () => { D.Notices.ForEach(n => n.Read = true); Save(); Build(); }), Pill("Clear all", "trash", () => { D.Notices.Clear(); notificationFilter = "All"; Save(); Build(); }));
        DockPanel.SetDock(actions, Dock.Right);
        heading.Children.Add(actions);
        heading.Children.Add(T("Notifications", 21, Fg, true));
        DockPanel.SetDock(heading, Dock.Top);
        dock.Children.Add(heading);
        var foot = T("Stored locally · up to 200 notifications", 11, Muted);
        foot.Margin = new Thickness(0, 10, 0, 0);
        DockPanel.SetDock(foot, Dock.Bottom);
        dock.Children.Add(foot);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(220)
        });
        grid.ColumnDefinitions.Add(new());
        var filters = Col();
        void Filter(string name, int count, string icon)
        {
            var row = new DockPanel();
            var number = T(count.ToString(), 12, Muted);
            DockPanel.SetDock(number, Dock.Right);
            row.Children.Add(number);
            row.Children.Add(Row(Icon(icon, 17), Space(10), Line(name, 14)));
            var b = Btn("", () => { notificationFilter = name; RenderTab(); }, notificationFilter == name);
            b.Content = row;
            b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            b.Margin = new Thickness(0, 0, 10, 7);
            b.Background = notificationFilter == name ? Surface : Brushes.Transparent;
            filters.Children.Add(b);
        }
        Filter("All", D.Notices.Count, "bell");
        Filter("Unread", D.Notices.Count(n => !n.Read), "moon");
        foreach (var group in D.Notices.GroupBy(n => n.Title).OrderByDescending(n => n.Count()).Take(10))
            Filter(group.Key, group.Count(), "bell");
        Put(grid, Scroll(filters), 0);
        var notices = D.Notices.Where(n => notificationFilter == "All" || notificationFilter == "Unread" && !n.Read || n.Title == notificationFilter).ToList();
        if (notices.Count == 0)
            Put(grid, EmptyState("bell", "All quiet. You’re up to date."), 1);
        else
        {
            var list = Col(T("RECENT", 12, Muted, true));
            Gap(list, 12);
            foreach (var n in notices)
            {
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 17) };
                var stamp = Col(T(n.Time.ToString("h:mm tt").ToLowerInvariant(), 11, Muted));
                if (!n.Read)
                {
                    Gap(stamp, 8);
                    stamp.Children.Add(new Ellipse { Width = 9, Height = 9, Fill = Accent, HorizontalAlignment = HorizontalAlignment.Right });
                }
                DockPanel.SetDock(stamp, Dock.Right);
                row.Children.Add(stamp);
                var content = Col(T(n.Title, 15, Fg, true), T(n.Detail, 13, Muted));
                content.Margin = new Thickness(0, 0, 20, 0);
                row.Children.Add(content);
                row.MouseLeftButtonUp += (_, _) => { n.Read = true; Save(); Build(); };
                list.Children.Add(row);
            }
            Put(grid, Scroll(list), 1);
        }
        dock.Children.Add(grid);
    }
    void AiChat()
    {
        var dock = new DockPanel();
        body.Children.Add(dock);
        var composer = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        composer.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(48)
        });
        composer.ColumnDefinitions.Add(new());
        composer.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(62)
        });
        var fresh = IconButton("edit", () => { if (chatBusy) return; chat.Clear(); chatError = ""; RenderTab(); }, "New chat", Muted, 22);
        Put(composer, fresh, 0);
        var input = Input(chatDraft, "Message… Enter to send, Shift+Enter for a new line", true);
        input.MinHeight = 62;
        input.MaxHeight = 100;
        input.IsEnabled = !chatBusy;
        input.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        input.TextChanged += (_, _) => chatDraft = input.Text;
        input.KeyDown += async (_, e) => { if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0) { e.Handled = true; await SendChat(); } };
        Put(composer, input, 1);
        var send = IconButton("send", async () => await SendChat(), "Send message", Brushes.White, 23);
        send.Background = Accent;
        send.Width = 46;
        send.Height = 46;
        send.Padding = new Thickness(10);
        send.IsEnabled = !chatBusy;
        send.Margin = new Thickness(10, 0, 0, 0);
        Put(composer, send, 2);
        DockPanel.SetDock(composer, Dock.Bottom);
        dock.Children.Add(composer);
        var list = Col();
        if (chat.Count == 0)
        {
            var empty = Col(Row(Icon("AI Chat", 25, Brush("#C453EB")), Space(9), T("Your assistant, in the notch", 23, Fg, true)));
            Gap(empty, 14);
            var text = T("Connect your Ollama model in Settings → AI Chat to start chatting.", 14, Muted);
            text.HorizontalAlignment = HorizontalAlignment.Center;
            empty.Children.Add(text);
            Gap(empty, 17);
            var links = Row(Pill("Get Ollama", "external", () => TryOpen("https://ollama.com/")), Pill("Open Settings", "settings", () => OpenSettingsPage("AI Chat"), true));
            links.HorizontalAlignment = HorizontalAlignment.Center;
            empty.Children.Add(links);
            if (chatError.Length > 0)
            {
                Gap(empty, 12);
                empty.Children.Add(T(chatError, 12, Brush("#FFAA86")));
            }
            dock.Children.Add(Center(empty));
            return;
        }
        foreach (var m in chat)
        {
            var card = PanelCard(Col(T(m.Role == "user" ? "YOU" : "ASSISTANT", 10, Muted, true), T(m.Text, 15)), 14);
            card.Margin = new Thickness(m.Role == "user" ? 65 : 0, 0, m.Role == "user" ? 0 : 65, 12);
            list.Children.Add(card);
        }
        if (chatBusy)
            list.Children.Add(T("Thinking…", 13, Muted));
        if (chatError.Length > 0)
            list.Children.Add(T(chatError, 13, Brush("#FFAA86")));
        var scroll = Scroll(list);
        dock.Children.Add(scroll);
        scroll.Loaded += (_, _) => scroll.ScrollToEnd();
    }
}
