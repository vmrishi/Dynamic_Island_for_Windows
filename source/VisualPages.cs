namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
public partial class Island
{
    void Home()
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(7, GridUnitType.Star)
        });
        g.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        g.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(2.6, GridUnitType.Star)
        });
        body.Children.Add(g);
        var music = new DockPanel();
        var top = new Grid { Height = 90 };
        top.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(108)
        });
        top.ColumnDefinitions.Add(new());
        var art = new Border { Width = 90, Height = 90, CornerRadius = new CornerRadius(18), Background = Brush(S.Light ? "#E4E4E7" : "#222222"), HorizontalAlignment = HorizontalAlignment.Left, Child = Icon("music", 42, Muted) };
        try
        {
            var encoded = MediaString("art");
            if (encoded.Length > 0)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(Convert.FromBase64String(encoded));
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                art.Child = new Image { Source = bitmap, Stretch = Stretch.UniformToFill };
                art.Clip = new RectangleGeometry(new Rect(0, 0, 90, 90), 18, 18);
            }
        }
        catch { }
        Put(top, art, 0);
        var meta = Col(Line(HasMedia() ? MediaString("title", "Untitled track") : "Nothing playing", 22, Fg, true), Line(HasMedia() ? MediaString("artist") : "Play something in Spotify, YouTube, Apple Music…", 17, Muted));
        meta.VerticalAlignment = VerticalAlignment.Center;
        Put(top, meta, 1);
        DockPanel.SetDock(top, Dock.Top);
        music.Children.Add(top);
        var transport = Col();
        Gap(transport, 10);
        var time = Columns(.4, 3.8, .4);
        Put(time, Line(FormatTime(MediaNumber("position")), 12, Muted), 0);
        var duration = MediaNumber("duration");
        Put(time, Progress(duration > 0 ? MediaNumber("position") / duration : 0, Accent, 6), 1);
        var end = Line(FormatTime(duration), 12, Muted);
        end.HorizontalAlignment = HorizontalAlignment.Right;
        Put(time, end, 2);
        transport.Children.Add(time);
        Gap(transport, 18);
        var keys = Row(IconButton("previous", () => Native.MediaKey(0xB1), "Previous track", HasMedia() ? Fg : Muted, 24), IconButton(MediaString("status") == "Playing" ? "pause" : "play", () => Native.MediaKey(0xB3), "Play / pause", HasMedia() ? Fg : Muted, 32), IconButton("next", () => Native.MediaKey(0xB0), "Next track", HasMedia() ? Fg : Muted, 24));
        keys.HorizontalAlignment = HorizontalAlignment.Center;
        transport.Children.Add(keys);
        music.Children.Add(transport);
        Put(g, music, 0);
        Put(g, Divider(), 1);
        var side = Col();
        var heading = new DockPanel();
        var add = IconButton("plus", () => { selectedDate = DateTime.Today; Go("Calendar"); AddEvent(); }, "Add calendar event");
        DockPanel.SetDock(add, Dock.Right);
        heading.Children.Add(add);
        heading.Children.Add(Row(T(DateTime.Today.ToString("MMMM"), 19, Fg, true), Space(5), T(DateTime.Today.Year.ToString(), 18, Muted)));
        side.Children.Add(heading);
        var week = new UniformGrid { Columns = 7, Margin = new Thickness(0, 7, 0, 18) };
        for (int i = 0; i < 7; i++)
        {
            var date = DateTime.Today.AddDays(i);
            var label = Col(T(date.ToString("ddd").ToUpperInvariant()[..2], 10, i == 0 ? Brushes.White : Muted, true), T(date.Day.ToString(), 17, i == 0 ? Brushes.White : Fg, true));
            foreach (var t in label.Children.OfType<TextBlock>())
                t.HorizontalAlignment = HorizontalAlignment.Center;
            var day = Btn("", () => { selectedDate = date; Go("Calendar"); });
            day.Content = label;
            day.Background = i == 0 ? Brush("#FF4945") : Brushes.Transparent;
            day.Margin = new Thickness(0, 0, 2, 0);
            day.Padding = new Thickness(3, 7, 3, 7);
            week.Children.Add(day);
        }
        side.Children.Add(week);
        var upcoming = D.Events.Where(e => e.Start.Date == DateTime.Today).OrderBy(e => e.Start).Take(2).ToList();
        if (upcoming.Count == 0)
        {
            side.Children.Add(T("No events today.", 16, Muted));
            var open = Btn("Open calendar", () => Go("Calendar"));
            open.Foreground = Accent;
            open.Background = Brushes.Transparent;
            side.Children.Add(open);
        }
        else
        foreach (var e in upcoming)
        {
            side.Children.Add(Line(e.Start.ToString("HH:mm") + "  " + e.Title, 14, Fg, true));
            Gap(side, 8);
        }
        Put(g, side, 2);
    }
    void Dashboard()
    {
        var g = Columns(1, 1, 1, 1);
        body.Children.Add(g);
        var quick = Col(Row(Icon("Control", 15, Muted), Space(8), T("QUICK TOGGLES", 12, Muted, true)));
        Gap(quick, 10);
        var toggles = new UniformGrid { Columns = 4 };
        foreach (var item in new[] { ("wifi", "Wi-Fi"), ("bluetooth", "Bluetooth"), ("coffee", "Keep awake"), ("moon", "Theme"), ("mic", "Microphone"), ("volume", "Volume"), ("crop", "Screenshot"), ("lock", "Lock") })
        {
            var b = IconButton(item.Item1, () => QuickToggle(item.Item1), item.Item2, Fg, 21);
            b.Background = (item.Item1 == "wifi" && ConnectedWifi() != "Not connected") || (item.Item1 == "bluetooth" && devices.Any(d => d.State == "Connected")) ? Accent : Brush(S.Light ? "#DCDCE0" : "#202020");
            b.Height = 61;
            b.Margin = new Thickness(0, 0, 7, 8);
            b.Padding = new Thickness(8);
            toggles.Children.Add(b);
        }
        quick.Children.Add(toggles);
        Put(g, Card(quick, 16), 0);
        var outside = Col(Row(Icon("pin", 15, Muted), Space(6), Line(S.City.Length > 0 ? S.City.ToUpperInvariant() : "WEATHER", 12, Muted, true)));
        Gap(outside, 14);
        if (weather != null)
        {
            var c = weather.Value.GetProperty("current");
            outside.Children.Add(Row(Icon("Weather", 49, Fg), Space(10), T(c.GetProperty("temperature_2m").GetDouble().ToString("0") + "°", 38, Fg, true)));
            Gap(outside, 6);
            outside.Children.Add(T(WeatherLabel(c.GetProperty("weather_code").GetInt32()), 14));
            var d = weather.Value.GetProperty("daily");
            Gap(outside, 10);
            outside.Children.Add(T($"H {d.GetProperty("temperature_2m_max")[0].GetDouble():0}° · L {d.GetProperty("temperature_2m_min")[0].GetDouble():0}°", 13, Muted));
        }
        else
        {
            outside.Children.Add(Icon("Weather", 48, Fg));
            Gap(outside, 12);
            outside.Children.Add(Pill("Choose location", "pin", () => OpenSettingsPage("Weather")));
        }
        Put(g, Card(outside, 16), 1);
        var apps = Col(Row(Icon("folder", 15, Muted), Space(7), T("APPS", 12, Muted, true)));
        Gap(apps, 12);
        var launchers = new UniformGrid { Columns = 3 };
        foreach (var a in new[] { ("folder", "Files", "explorer.exe"), ("download", "Downloads", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads"), ("calculator", "Calculator", "calc.exe"), ("Notes", "Notepad", "notepad.exe"), ("chart", "Task Manager", "taskmgr.exe"), ("settings", "Settings", "ms-settings:") })
        {
            var col = Col(Icon(a.Item1, 30, a.Item1 == "folder" ? Brush("#FFCF55") : a.Item1 == "chart" ? Accent : Fg));
            Gap(col, 8);
            var name = Line(a.Item2, 10, Muted);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            col.Children.Add(name);
            var button = Btn("", () => TryOpen(a.Item3));
            button.Content = col;
            button.Padding = new Thickness(2, 6, 2, 6);
            button.Margin = new Thickness(0);
            button.Background = Brushes.Transparent;
            launchers.Children.Add(button);
        }
        apps.Children.Add(launchers);
        Put(g, Card(apps, 16), 2);
        var day = Col(Row(Icon("Day Progress", 15, Muted), Space(7), T("DAY", 12, Muted, true)));
        Gap(day, 18);
        var value = Store.DayProgress(DateTime.Now, S.WakeHour, S.BedHour);
        day.Children.Add(T(value.ToString("P0"), 42, Fg, true));
        Gap(day, 6);
        day.Children.Add(Progress(value, Accent, 7));
        Gap(day, 10);
        day.Children.Add(T(value >= 1 ? "Past bedtime  ☾" : "Your day is unfolding", 13, Muted));
        Put(g, Card(day, 16), 3);
    }
    void QuickToggle(string icon)
    {
        switch (icon)
        {
            case "wifi":
                connectionTab = "Wi-Fi";
                Go("Connections");
                break;
            case "bluetooth":
                connectionTab = "Bluetooth";
                Go("Connections");
                break;
            case "coffee":
                ToggleAwake();
                break;
            case "moon":
                ChangeAppTheme();
                break;
            case "mic":
                Native.ToggleMute(1);
                RenderTab();
                break;
            case "volume":
                Go("Control");
                break;
            case "eye":
                Go("Health");
                StartHealth("Eyes", 20);
                break;
            case "crop":
                TryOpen("ms-screenclip:");
                break;
            case "lock":
                Save();
                Native.LockWorkStation();
                break;
        }
    }
    void Control()
    {
        var dock = new DockPanel();
        body.Children.Add(dock);
        var grid = new UniformGrid { Columns = 4 };
        grid.Children.Add(LabeledTile("wifi", ConnectedWifi(), async () => await ToggleRadio("WiFi"), radios.GetValueOrDefault("WiFi") == "On", () => { connectionTab = "Wi-Fi"; Go("Connections"); }));
        grid.Children.Add(LabeledTile("bluetooth", BluetoothSummary(), async () => await ToggleRadio("Bluetooth"), radios.GetValueOrDefault("Bluetooth") == "On", () => { connectionTab = "Bluetooth"; Go("Connections"); }));
        grid.Children.Add(LabeledTile("coffee", "Keep awake", ToggleAwake, keepAwake));
        grid.Children.Add(LabeledTile("moon", S.Light ? "Light mode" : "Dark mode", ChangeAppTheme, S.Light));
        bool muted = false;
        try
        {
            muted = Native.Audio(1).Muted;
        }
        catch { }
        grid.Children.Add(LabeledTile("mic", muted ? "Microphone · muted" : "Microphone", () => { Native.ToggleMute(1); RenderTab(); }, muted));
        grid.Children.Add(LabeledTile("eye", "Eye breaks", () => { Go("Health"); StartHealth("Eyes", 20); }));
        grid.Children.Add(LabeledTile("crop", "Screenshot", () => TryOpen("ms-screenclip:")));
        grid.Children.Add(LabeledTile("lock", "Lock", () => { Save(); Native.LockWorkStation(); }));
        DockPanel.SetDock(grid, Dock.Top);
        dock.Children.Add(grid);
        var sliders = Col();
        if (brightness != null)
            sliders.Children.Add(SliderControl("sun", "Brightness", brightness.Value, async v => { try { await Native.Broker("setbrightness", (int)v); brightness = (int)v; } catch (Exception e) { Notify("Brightness unavailable", e.Message); } }));
        else
        {
            var unavailable = Row(Icon("sun", 22), Space(20), T("Brightness unavailable for this display", 13, Muted), Space(10), Pill("Display settings", "chevron", () => TryOpen("ms-settings:display")));
            sliders.Children.Add(unavailable);
        }
        Gap(sliders, 7);
        try
        {
            var audio = Native.Audio();
            var row = new Grid();
            row.ColumnDefinitions.Add(new());
            row.ColumnDefinitions.Add(new()
            {
                Width = GridLength.Auto
            });
            Put(row, SliderControl("volume", "Volume", audio.Volume * 100, v => Native.SetVolume((float)(v / 100))), 0);
            var output = IconButton("chevron", () => TryOpen("ms-settings:sound"), "Sound output settings");
            output.Background = Brush(S.Light ? "#E2E2E6" : "#222222");
            output.Margin = new Thickness(14, 0, 0, 0);
            Put(row, output, 1);
            sliders.Children.Add(row);
        }
        catch { sliders.Children.Add(Pill("Choose a sound output", "volume", () => TryOpen("ms-settings:sound"))); }
        var card = PanelCard(sliders, 16);
        card.VerticalAlignment = VerticalAlignment.Bottom;
        dock.Children.Add(card);
    }
    void Shelf()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new());
        grid.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(156)
        });
        body.Children.Add(grid);
        var drop = new Grid { Margin = new Thickness(0, 0, 18, 0), AllowDrop = true, Background = Brushes.Transparent };
        var outline = new Rectangle { Stroke = Brush(S.Light ? "#C4C4CD" : "#303033"), StrokeDashArray = new DoubleCollection { 4, 3 }, StrokeThickness = 1.5, RadiusX = 24, RadiusY = 24 };
        drop.Children.Add(outline);
        drop.DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None; e.Handled = true; };
        drop.Drop += (_, e) => { if (e.Data.GetData(DataFormats.FileDrop) is string[] files) { D.Shelf = D.Shelf.Concat(files).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); Save(); RenderTab(); } };
        if (D.Shelf.Count == 0)
        {
            var empty = Col(Icon("Shelf", 38, Muted));
            Gap(empty, 18);
            var title = T("Drop files or apps here", 20, Fg, true);
            title.HorizontalAlignment = HorizontalAlignment.Center;
            empty.Children.Add(title);
            Gap(empty, 7);
            empty.Children.Add(T("Keep files handy, then drag them out anywhere", 15, Muted));
            drop.Children.Add(Center(empty));
        }
        else
        {
            var wrap = new WrapPanel { Margin = new Thickness(16) };
            foreach (var path in D.Shelf.ToArray())
            {
                var tile = new DockPanel { Width = 135, Margin = new Thickness(0, 0, 12, 12) };
                var remove = IconButton("close", () => { D.Shelf.Remove(path); Save(); RenderTab(); }, "Remove shortcut", Muted, 13);
                remove.HorizontalAlignment = HorizontalAlignment.Right;
                DockPanel.SetDock(remove, Dock.Bottom);
                tile.Children.Add(remove);
                var item = Col(AppIcon(path));
                Gap(item, 9);
                var name = Line(System.IO.Path.GetFileName(path), 12);
                name.MaxWidth = 115;
                item.Children.Add(name);
                var launch = Btn("", () => TryOpen(path), false, path);
                launch.Content = item;
                launch.Background = Surface;
                launch.Height = 84;
                launch.Margin = new Thickness(0);
                var origin = new Point();
                launch.PreviewMouseLeftButtonDown += (_, e) => origin = e.GetPosition(launch);
                launch.PreviewMouseMove += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed && (e.GetPosition(launch) - origin).Length > 8 && !path.StartsWith("shell:")) DragDrop.DoDragDrop(launch, new DataObject(DataFormats.FileDrop, new[] { path }), DragDropEffects.Copy | DragDropEffects.Link); };
                tile.Children.Add(launch);
                wrap.Children.Add(tile);
            }
            drop.Children.Add(Scroll(wrap));
        }
        Put(grid, drop, 0);
        var side = new DockPanel();
        var tools = Row(IconButton("plus", AddShelf, "Add files or apps"), IconButton("folder", AddInstalledApp, "Choose installed application"));
        tools.Margin = new Thickness(0, 10, 0, 0);
        DockPanel.SetDock(tools, Dock.Bottom);
        side.Children.Add(tools);
        var share = Btn("", () => { if (D.Shelf.Count == 0) { Notify("Shelf is empty", "Add an item before copying its path.", false); return; } Clipboard.SetText(string.Join(Environment.NewLine, D.Shelf)); Notify("Paths copied", "Paste the shortcut paths into your app to share them.", false); }, false, "Copy shelf paths");
        share.Background = Brush(S.Light ? "#E5F1FF" : "#031321");
        share.Foreground = Accent;
        var content = Col(Icon("share", 36, Accent));
        Gap(content, 12);
        var label = T("Copy paths", 16, Accent, true);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        content.Children.Add(label);
        share.Content = content;
        share.Margin = new Thickness(0);
        side.Children.Add(share);
        Put(grid, side, 1);
    }
    DateTime? taskDue; string taskDueText = "09:00";
    void Todos()
    {
        var dock = new DockPanel();
        body.Children.Add(dock);
        var top = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        top.ColumnDefinitions.Add(new());
        top.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        top.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        top.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        var input = Input("", "Add a task…");
        input.Height = 44;
        void Add()
        {
            if (string.IsNullOrWhiteSpace(input.Text))
                return;
            D.Todos.Add(new TodoItem { Text = input.Text.Trim(), Due = taskDue });
            taskDue = null;
            Save();
            RenderTab();
        }
        input.KeyDown += (_, e) => { if (e.Key == Key.Enter) { e.Handled = true; Add(); } };
        Put(top, input, 0);
        var add = IconButton("plus", Add, "Add task", Fg);
        add.Margin = new Thickness(5, 0, 4, 0);
        Put(top, add, 1);
        var reminder = IconButton("clock", () => { var date = new DatePicker { SelectedDate = taskDue?.Date ?? DateTime.Today }; var time = Input(taskDueText, "HH:mm"); ShowDialog("Task reminder", Col(T("Due date and time", 18, Fg, true), date, time), () => { if (date.SelectedDate == null || !TimeSpan.TryParse(time.Text, out var ts) || ts.TotalHours < 0 || ts.TotalHours >= 24) throw new Exception("Choose a date and a time from 00:00 to 23:59."); taskDue = date.SelectedDate.Value.Date + ts; taskDueText = time.Text; }); }, taskDue?.ToString("g") ?? "Add reminder", taskDue != null ? Accent : Muted);
        Put(top, reminder, 2);
        var filters = Row();
        foreach (var f in new[] { "Active", "Done", "Trash" })
        {
            var b = Btn(f, () => { todoFilter = f; RenderTab(); }, todoFilter == f);
            b.Background = todoFilter == f ? Brush(S.Light ? "#D4D4DB" : "#303030") : Brushes.Transparent;
            b.Margin = new Thickness(0);
            b.Padding = new Thickness(12, 7, 12, 7);
            filters.Children.Add(b);
        }
        Put(top, new Border { Background = Surface, CornerRadius = new CornerRadius(13), Padding = new Thickness(3), Child = filters }, 3);
        DockPanel.SetDock(top, Dock.Top);
        dock.Children.Add(top);
        var count = T($"{D.Todos.Count(t => !t.Done && !t.Trashed)} open · {D.Todos.Count(t => t.Done && !t.Trashed)} done", 12, Muted);
        count.Margin = new Thickness(0, 12, 0, 0);
        DockPanel.SetDock(count, Dock.Bottom);
        dock.Children.Add(count);
        var tasks = D.Todos.Where(t => todoFilter == "Trash" ? t.Trashed : !t.Trashed && t.Done == (todoFilter == "Done")).ToList();
        if (tasks.Count == 0)
        {
            dock.Children.Add(EmptyState("Todo", todoFilter == "Active" ? "Nothing to do. Add a task above." : "Nothing here yet."));
            return;
        }
        var list = Col();
        foreach (var t in tasks)
        {
            var row = new DockPanel();
            var actions = Row();
            if (t.Trashed)
            {
                actions.Children.Add(IconButton("refresh", () => { t.Trashed = false; Save(); RenderTab(); }, "Restore task"));
                actions.Children.Add(IconButton("trash", () => { D.Todos.Remove(t); Save(); RenderTab(); }, "Delete permanently"));
            }
            else
                actions.Children.Add(IconButton("trash", () => { t.Trashed = true; Save(); RenderTab(); }, "Remove task"));
            DockPanel.SetDock(actions, Dock.Right);
            row.Children.Add(actions);
            var check = new CheckBox { IsChecked = t.Done, Foreground = Fg, VerticalContentAlignment = VerticalAlignment.Center, Content = Col(T(t.Text, 15, Fg, true), T(t.Due?.ToString("ddd, dd MMM · HH:mm") ?? "", 11, Muted)) };
            check.Click += (_, _) => { t.Done = check.IsChecked == true; Save(); RenderTab(); };
            row.Children.Add(check);
            var card = PanelCard(row, 12);
            card.Margin = new Thickness(0, 0, 0, 8);
            list.Children.Add(card);
        }
        dock.Children.Add(Scroll(list));
    }
    void Notes()
    {
        var grid = Columns(1, 2.8);
        body.Children.Add(grid);
        var sidebar = new DockPanel { Margin = new Thickness(0, 0, 14, 0) };
        var add = Pill("New note", "edit", () => { SaveNoteDraft(); notesId = ""; noteDraft = ""; RenderTab(); });
        add.HorizontalContentAlignment = HorizontalAlignment.Left;
        add.Margin = new Thickness(0, 0, 0, 10);
        DockPanel.SetDock(add, Dock.Top);
        sidebar.Children.Add(add);
        if (D.Notes.Count == 0)
            sidebar.Children.Add(EmptyState("Notes", "No notes yet"));
        else
        {
            var list = Col();
            foreach (var n in D.Notes.OrderByDescending(n => n.Updated))
            {
                var item = new DockPanel();
                var remove = IconButton("trash", () => { D.Notes.Remove(n); if (n.Id == notesId) { notesId = ""; noteDraft = ""; } Save(); RenderTab(); }, "Delete note", Muted, 15);
                DockPanel.SetDock(remove, Dock.Right);
                item.Children.Add(remove);
                var select = Btn("", () => { SaveNoteDraft(); notesId = n.Id; noteDraft = n.Body; RenderTab(); }, n.Id == notesId);
                select.Content = Col(Line(n.Title, 14, Fg, true), Line(n.Updated.ToString("dd MMM · HH:mm"), 10, Muted));
                select.HorizontalContentAlignment = HorizontalAlignment.Left;
                select.Background = n.Id == notesId ? Surface : Brushes.Transparent;
                select.Margin = new Thickness(0);
                item.Children.Add(select);
                list.Children.Add(item);
                Gap(list, 7);
            }
            sidebar.Children.Add(Scroll(list));
        }
        Put(grid, sidebar, 0);
        var editor = new DockPanel();
        var tools = Row();
        tools.HorizontalAlignment = HorizontalAlignment.Right;
        tools.Children.Add(IconButton("trash", () => { if (notesId != "") D.Notes.RemoveAll(n => n.Id == notesId); notesId = ""; noteDraft = ""; Save(); RenderTab(); }, "Delete current note", Muted, 17));
        tools.Children.Add(Pill("OK", "check", () => { SaveNoteDraft(); RenderTab(); }));
        DockPanel.SetDock(tools, Dock.Bottom);
        editor.Children.Add(tools);
        var text = Input(noteDraft, "Start typing… The first line becomes the title.", true);
        text.FontSize = 16 * S.TextScale;
        text.Background = Brushes.Transparent;
        text.BorderThickness = new Thickness(0);
        text.Padding = new Thickness(0);
        text.VerticalContentAlignment = VerticalAlignment.Top;
        text.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        text.TextChanged += (_, _) => noteDraft = text.Text;
        editor.Children.Add(text);
        Put(grid, PanelCard(editor, 18), 1);
    }
    void SaveNoteDraft()
    {
        if (string.IsNullOrWhiteSpace(noteDraft))
            return;
        var note = D.Notes.FirstOrDefault(n => n.Id == notesId);
        if (note == null)
        {
            note = new Note();
            D.Notes.Add(note);
            notesId = note.Id;
        }
        if (note.Body != noteDraft)
        {
            note.Body = noteDraft;
            note.Updated = DateTime.Now;
        }
        Save();
    }
    void Pomodoro()
    {
        var grid = Columns(.85, 2.4, 1.15);
        body.Children.Add(grid);
        var dial = new Grid { Width = 160, Height = 160, VerticalAlignment = VerticalAlignment.Center };
        dial.Children.Add(ActivityRing(160, D.Timer.Mode == "Stopwatch" ? 0 : 1 - TimerSeconds() / Math.Max(1, D.Timer.SessionDuration), Accent));
        var text = Col();
        timerText = T(FormatTime(TimerSeconds()), 34, Fg, true);
        timerText.HorizontalAlignment = HorizontalAlignment.Center;
        text.Children.Add(timerText);
        var label = T(D.Timer.Mode.ToUpperInvariant(), 12, Muted, true);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        text.Children.Add(label);
        dial.Children.Add(Center(text));
        Put(grid, dial, 0);
        var controls = Col();
        controls.Margin = new Thickness(32, 0, 22, 0);
        controls.VerticalAlignment = VerticalAlignment.Center;
        var modes = Row();
        foreach (var mode in new[] { "Focus", "Short break", "Long break", "Stopwatch" })
        {
            var b = Btn(mode, () => SetTimerMode(mode), D.Timer.Mode == mode);
            b.Padding = new Thickness(11, 7, 11, 7);
            b.FontSize = 13 * S.TextScale;
            b.Background = D.Timer.Mode == mode ? Brush(S.Light ? "#DDDEE2" : "#303030") : Brushes.Transparent;
            b.Margin = new Thickness(0);
            modes.Children.Add(b);
        }
        controls.Children.Add(new Border { Background = Surface, CornerRadius = new CornerRadius(12), Padding = new Thickness(3), Child = modes, HorizontalAlignment = HorizontalAlignment.Left });
        Gap(controls, 20);
        var play = IconButton(D.Timer.Running ? "pause" : "play", ToggleTimer, D.Timer.Running ? "Pause" : "Start", Brushes.Black, 28);
        play.Background = Brushes.White;
        play.Width = 66;
        play.Height = 66;
        play.Padding = new Thickness(18);
        play.Margin = new Thickness(14, 0, 14, 0);
        var reset = IconButton("refresh", () => SetTimerMode(D.Timer.Mode), "Reset timer");
        reset.Background = Surface;
        reset.Width = 46;
        reset.Height = 46;
        var next = IconButton("next", () => SetTimerMode(D.Timer.Mode == "Focus" ? (D.Today.FocusSessions % 4 == 3 ? "Long break" : "Short break") : "Focus"), "Next session");
        next.Background = Surface;
        next.Width = 46;
        next.Height = 46;
        controls.Children.Add(Row(reset, play, next));
        Gap(controls, 18);
        var dots = Row();
        for (int i = 0; i < 4; i++)
            dots.Children.Add(new Ellipse { Width = 11, Height = 11, Fill = i < D.Today.FocusSessions % 4 ? Accent : CardBorder, Margin = new Thickness(0, 0, 8, 0) });
        controls.Children.Add(dots);
        Put(grid, controls, 1);
        var stats = Col();
        stats.VerticalAlignment = VerticalAlignment.Center;
        stats.Children.Add(PanelCard(Col(Row(Icon("Pomodoro", 16, Muted), Space(6), T("TODAY", 12, Muted, true)), T(D.Today.FocusSessions.ToString(), 34, Fg, true), T($"sessions · {D.Today.FocusSeconds / 60:0}m focused", 12, Muted)), 17));
        Gap(stats, 14);
        timerState = T(D.Timer.Running ? "In progress" : "Paused", 13, Muted);
        stats.Children.Add(timerState);
        Put(grid, stats, 2);
    }
    void Health()
    {
        var grid = Columns(1, 3);
        body.Children.Add(grid);
        var day = D.Today;
        var ring = new Grid { Width = 180, Height = 180 };
        ring.Children.Add(ActivityRing(180, day.Movement / 6d, Brush("#27CF63")));
        ring.Children.Add(ActivityRing(138, day.Water / (double)S.WaterGoal, Brush("#52CEF2")));
        ring.Children.Add(ActivityRing(96, day.Breathing / 5d, Brush("#C453EB")));
        var left = Col(ring);
        Gap(left, 18);
        var counts = Row(T($"{day.Movement}/6", 12, Brush("#27CF63")), Space(14), T($"{day.Water}/{S.WaterGoal}", 12, Brush("#52CEF2")), Space(14), T($"{day.Breathing}/5", 12, Brush("#C453EB")));
        counts.HorizontalAlignment = HorizontalAlignment.Center;
        left.Children.Add(counts);
        left.VerticalAlignment = VerticalAlignment.Center;
        Put(grid, left, 0);
        var panel = new DockPanel();
        var top = Col();
        if (healthEnd != null)
        {
            healthText = T(healthMode + " · starting…", 14, Accent, true);
            top.Children.Add(Row(healthText, Pill("Cancel", "close", () => { healthEnd = null; RenderTab(); })));
        }
        else
            top.Children.Add(T(D.Today.LastMovement == null ? "No movement breaks recorded yet" : "Last movement break " + FormatElapsed(DateTime.Now - D.Today.LastMovement.Value) + " ago", 12, Muted));
        top.Margin = new Thickness(0, 0, 0, 10);
        DockPanel.SetDock(top, Dock.Top);
        panel.Children.Add(top);
        var cards = new UniformGrid { Columns = 2 };
        var water = Col(Row(Icon("water", 22, Brush("#52CEF2")), Space(9), T("Water", 16, Fg, true)));
        Gap(water, 14);
        var amount = Columns(1, 2, 1);
        Put(amount, IconButton("minus", () => { day.Water = Math.Max(0, day.Water - 1); Save(); RenderTab(); }, "Remove glass"), 0);
        var waterCount = Row(T(day.Water.ToString(), 28, Fg, true), T(" / " + S.WaterGoal, 12, Muted, true));
        waterCount.HorizontalAlignment = HorizontalAlignment.Center;
        Put(amount, waterCount, 1);
        Put(amount, IconButton("plus", () => { day.Water++; lastWater = DateTime.Now; Save(); RenderTab(); }, "Add glass"), 2);
        water.Children.Add(amount);
        cards.Children.Add(HealthCard(water));
        foreach (var x in new[] { ("Movement", "Movement break", "6 desk stretches · 2 min", "walk", "#27CF63", 120), ("Breathing", "Breathing", "Calm 4–4–6 breathing · 1 min", "leaf", "#C453EB", 60), ("Eyes", "Eye break", "20–20–20: look 20 ft away for 20 s", "eye", "#FFD51F", 20) })
        {
            var card = Col(Row(Icon(x.Item4, 22, Brush(x.Item5)), Space(9), T(x.Item2, 16, Fg, true)));
            Gap(card, 10);
            card.Children.Add(T(x.Item3, 12, Muted));
            Gap(card, 9);
            var start = Pill(x.Item1 == "Eyes" ? "Now" : "Start", "play", () => StartHealth(x.Item1, x.Item6));
            start.HorizontalContentAlignment = HorizontalAlignment.Left;
            start.Padding = new Thickness(10, 4, 10, 4);
            start.Margin = new Thickness(0);
            card.Children.Add(start);
            cards.Children.Add(HealthCard(card));
        }
        panel.Children.Add(cards);
        Put(grid, panel, 1);
    }
    static string FormatElapsed(TimeSpan span) => span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes}m" : $"{span.Minutes}m";
}
