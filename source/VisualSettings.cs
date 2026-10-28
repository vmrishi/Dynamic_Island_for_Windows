namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
public partial class Island
{
    Action<string>? showSettings;
    void OpenSettingsPage(string name)
    {
        OpenSettings();
        showSettings?.Invoke(name);
    }
    void OpenSettings()
    {
        if (settingsWindow != null)
        {
            settingsWindow.Activate();
            return;
        }
        var w = new Window { Title = "Orbit Settings", Width = 1160, Height = 850, MinWidth = 940, MinHeight = 620, WindowStartupLocation = WindowStartupLocation.CenterScreen, Background = Brush(S.Light ? "#F9F9FB" : "#111113"), Foreground = Fg, FontFamily = FontFamily, FontSize = 14, Resources = Resources };
        settingsWindow = w;
        w.Closed += (_, _) => { settingsWindow = null; showSettings = null; };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(248)
        });
        grid.ColumnDefinitions.Add(new());
        w.Content = grid;
        var sidebar = new DockPanel { Margin = new Thickness(14, 22, 14, 18) };
        var brand = Col(Row(new Border { Width = 39, Height = 39, CornerRadius = new CornerRadius(12), Background = new LinearGradientBrush(Color.FromRgb(64, 40, 109), Color.FromRgb(13, 11, 28), 90), Child = Icon("AI Chat", 23, Brush("#C197FF")) }, Space(13), Col(T("Orbit", 19, Fg, true), T("v0.3 · Windows notch", 11, Muted))));
        Gap(brand, 22);
        var search = Input("", "Search settings");
        search.Height = 37;
        brand.Children.Add(search);
        Gap(brand, 18);
        DockPanel.SetDock(brand, Dock.Top);
        sidebar.Children.Add(brand);
        var navPanel = Col();
        sidebar.Children.Add(Scroll(navPanel));
        Put(grid, new Border { Background = Brush(S.Light ? "#F0F0F4" : "#111113"), BorderBrush = CardBorder, BorderThickness = new Thickness(0, 0, 1, 0), Child = sidebar }, 0);
        var page = new ContentControl { Margin = new Thickness(40, 30, 34, 28) };
        Put(grid, page, 1);
        var buttons = new Dictionary<string, Button>();
        string current = "Welcome";
        void Show(string name)
        {
            current = name;
            foreach (var entry in buttons)
                entry.Value.Background = entry.Key == name ? Brush(S.Light ? "#DCEBFB" : "#152A40") : Brushes.Transparent;
            var content = SettingsContent(name, w);
            page.Content = Scroll(content);
        }
        showSettings = Show;
        void AddGroup(string title, IEnumerable<(string Name, string Icon, string Color)> items)
        {
            var heading = T(title, 10, Muted, true);
            heading.Margin = new Thickness(10, 10, 0, 8);
            navPanel.Children.Add(heading);
            foreach (var item in items)
            {
                var b = Btn("", () => Show(item.Name));
                b.Background = Brushes.Transparent;
                b.Padding = new Thickness(10, 8, 10, 8);
                b.Margin = new Thickness(0, 0, 0, 3);
                b.HorizontalContentAlignment = HorizontalAlignment.Left;
                b.Content = Row(new Border { Width = 26, Height = 26, CornerRadius = new CornerRadius(6), Background = Brush(item.Color), Child = Icon(item.Icon, 16, Brushes.White) }, Space(10), T(item.Name, 13));
                buttons[item.Name] = b;
                navPanel.Children.Add(b);
            }
        }
        AddGroup("ORBIT", new[] { ("Welcome", "Home", "#FF9C28"), ("General", "settings", "#73737C"), ("Modules", "Dashboard", "#6759E8") });
        AddGroup("FEATURES", new[] { ("Dashboard", "Dashboard", "#1687FF"), ("Control", "Control", "#73737C"), ("Shelf", "Shelf", "#FF9C28"), ("Todo", "Todo", "#FF9C28"), ("Notes", "Notes", "#C453EB"), ("Media", "music", "#FF356B"), ("Calendar", "Calendar", "#FF4945"), ("Weather", "Weather", "#43AEDB"), ("Pomodoro", "Pomodoro", "#FF674B"), ("Health", "Health", "#27CF63"), ("Screen Time", "Screen Time", "#6857D7"), ("Day Progress", "Day Progress", "#1687FF"), ("Bluetooth & Wi-Fi", "Connections", "#1687FF"), ("System", "System", "#73737C") });
        AddGroup("INTEGRATIONS & DATA", new[] { ("Notifications", "bell", "#FF4945"), ("AI Chat", "AI Chat", "#BD54E5"), ("Data & startup", "disk", "#73737C"), ("About", "Home", "#73737C") });
        search.TextChanged += (_, _) => { foreach (var b in buttons) b.Value.Visibility = b.Key.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed; };
        Show("Welcome");
        w.Show();
    }
    StackPanel SettingsHeading(string icon, string title, string description)
    {
        var p = Col(Row(Icon(icon, 28, Accent), Space(12), T(title, 29, Fg, true)));
        Gap(p, 18);
        p.Children.Add(T(description, 14, Muted));
        Gap(p, 24);
        return p;
    }
    StackPanel SettingsContent(string name, Window owner)
    {
        if (name == "Welcome")
        {
            var p = SettingsHeading("Home", "Welcome", "Orbit is an interactive notch for your Windows PC. It lives at the top centre of your screen. Hover to open it, drop files onto it, or press Alt+Shift+O.");
            var cards = new UniformGrid { Columns = 3 };
            foreach (var item in new[] { ("Media", "music", "Album art, playback controls and your current track."), ("Calendar", "Calendar", "Your month, local events and meeting reminders."), ("AI Chat", "AI Chat", "Chat with your own Ollama model."), ("Todo", "Todo", "A tidy task list with optional reminders."), ("Notes", "Notes", "Capture a thought and save it with one click."), ("Pomodoro", "Pomodoro", "Focus sessions, breaks and a stopwatch."), ("Weather", "Weather", "Current weather and hourly and daily forecasts."), ("Notifications", "bell", "Orbit reminders and optional Windows notifications."), ("Dashboard", "Dashboard", "Your toggles, weather, apps and day."), ("Control", "Control", "Volume, microphone, brightness and quick actions."), ("Health", "Health", "Water, movement, breathing and eye breaks."), ("Screen Time", "Screen Time", "See where your active time goes."), ("System", "System", "Live CPU, memory, network and disk metrics."), ("Bluetooth & Wi-Fi", "Connections", "Connected devices and available saved networks."), ("Shelf", "Shelf", "Drop files or pin applications for quick access.") })
            {
                var content = Col(Row(Icon(item.Item2, 18, Accent), Space(8), T(item.Item1, 14, Fg, true)));
                Gap(content, 12);
                content.Children.Add(T(item.Item3, 12, Muted));
                var b = Btn("", () => showSettings?.Invoke(item.Item1));
                b.Content = content;
                b.Background = Brush(S.Light ? "#EEEEF3" : "#1B1B1F");
                b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                b.VerticalContentAlignment = VerticalAlignment.Top;
                b.Padding = new Thickness(17);
                b.Height = 118;
                b.Margin = new Thickness(0, 0, 12, 12);
                cards.Children.Add(b);
            }
            p.Children.Add(cards);
            Gap(p, 14);
            p.Children.Add(T("Turn modules on or off and reorder them in Modules. Use General to change the notch, theme, colors and sizes. Your portable app keeps its data beside the executable.", 13, Muted));
            return p;
        }
        if (name == "General")
        {
            var p = SettingsHeading("settings", "General", "Shape, appearance and behavior of your notch.");
            var attached = Check("Attach the notch to the top edge of the screen", S.AttachToTop);
            p.Children.Add(attached);
            var hover = Check("Open when the pointer enters the notch", S.OpenOnHover);
            var collapse = Check("Collapse when the pointer leaves", S.AutoCollapse);
            var pin = Check("Keep the notch above other windows", S.AlwaysOnTop);
            var light = Check("Use the light theme", S.Light);
            foreach (var c in new[] { hover, collapse, pin, light })
                p.Children.Add(c);
            var title = Input(S.Name, "App name");
            var color = Input(S.IslandColor, "#000000");
            var accent = Input(S.Accent, "#1988FF");
            var width = Input(S.Width.ToString("0"), "800–1500");
            var font = Input((S.TextScale * 100).ToString("0"), "85–130");
            var icons = Input(S.IconSize.ToString("0"), "14–24");
            var top = Input(S.Top.ToString("0"), "0–150");
            var opacity = Input((S.Opacity * 100).ToString("0"), "65–100");
            p.Children.Add(Field("Display name", title));
            var colors = Columns(1, 1);
            Put(colors, Field("Notch color (dark theme)", color), 0);
            Put(colors, Field("Accent color", accent), 1);
            p.Children.Add(colors);
            var sizes = Columns(1, 1, 1);
            Put(sizes, Field("Content width", width), 0);
            Put(sizes, Field("Font size (%)", font), 1);
            Put(sizes, Field("Icon size", icons), 2);
            p.Children.Add(sizes);
            var other = Columns(1, 1);
            Put(other, Field("Top offset (when detached)", top), 0);
            Put(other, Field("Opacity (%)", opacity), 1);
            p.Children.Add(other);
            var monitor = new ComboBox { ItemsSource = System.Windows.Forms.Screen.AllScreens.Select((s, i) => $"Display {i + 1} · {s.Bounds.Width}×{s.Bounds.Height}").ToArray(), SelectedIndex = Math.Clamp(S.Monitor, 0, System.Windows.Forms.Screen.AllScreens.Length - 1), Padding = new Thickness(10), Background = Surface, Foreground = Fg };
            p.Children.Add(Field("Display", monitor));
            Gap(p);
            p.Children.Add(Pill("Apply changes", "check", () => { try { foreach (var c in new[] { color.Text, accent.Text }) if (!System.Text.RegularExpressions.Regex.IsMatch(c, "^#[0-9a-fA-F]{6}$")) throw new Exception("Colors must use #RRGGBB format."); var nw = Number(width, 800, 1500); var nf = Number(font, 85, 130); var ni = Number(icons, 14, 24); var nt = Number(top, 0, 150); var no = Number(opacity, 65, 100); S.Name = title.Text.Trim(); S.IslandColor = color.Text; S.Accent = accent.Text; S.Width = nw; S.TextScale = nf / 100; S.IconSize = ni; S.Top = nt; S.Opacity = no / 100; S.AttachToTop = attached.IsChecked == true; S.OpenOnHover = hover.IsChecked == true; S.AutoCollapse = collapse.IsChecked == true; S.AlwaysOnTop = pin.IsChecked == true; S.Light = light.IsChecked == true; S.Monitor = monitor.SelectedIndex; store.Normalize(); Save(); owner.Close(); SetExpanded(expanded); OpenSettingsPage("General"); } catch (Exception ex) { MessageBox.Show(owner, ex.Message, "Appearance"); } }, true));
            return p;
        }
        if (name == "Modules")
        {
            var p = SettingsHeading("Dashboard", "Modules", "Choose which tabs appear and arrange them in your preferred order.");
            foreach (var module in S.Modules.ToArray())
            {
                var row = new DockPanel();
                var controls = Row(IconButton("back", () => MoveModule(module, -1), "Move left"), IconButton("chevron", () => MoveModule(module, 1), "Move right"));
                DockPanel.SetDock(controls, Dock.Right);
                row.Children.Add(controls);
                var c = Check(module, !S.Hidden.Contains(module));
                c.Click += (_, _) => { if (c.IsChecked == true) S.Hidden.Remove(module); else if (S.Modules.Count - S.Hidden.Count > 1) S.Hidden.Add(module); else c.IsChecked = true; if (S.Hidden.Contains(tab)) tab = S.Modules.First(n => !S.Hidden.Contains(n)); Save(); Build(); };
                row.Children.Add(c);
                p.Children.Add(row);
            }
            return p;
        }
        if (name == "Weather")
        {
            var p = SettingsHeading("Weather", "Weather", "Choose a city for the dashboard and weather view. City queries and coordinates are sent to Open-Meteo.");
            var city = Input(S.City, "Search a city or town");
            p.Children.Add(Field("Location", city));
            var result = T(weatherError.Length > 0 ? weatherError : S.City, 13, Muted);
            p.Children.Add(result);
            Gap(p);
            var search = Pill("Find and save location", "pin", async () => { result.Text = "Finding location…"; await SetCity(city.Text); result.Text = weatherError.Length > 0 ? weatherError : "Saved: " + S.City; }, true);
            p.Children.Add(search);
            return p;
        }
        if (name == "AI Chat")
        {
            var p = SettingsHeading("AI Chat", "AI Chat", "Connect to a local Ollama model or a compatible Ollama chat server. Messages are sent to the configured endpoint.");
            var endpoint = Input(S.AiEndpoint, "http://localhost:11434/api/chat");
            var model = Input(S.AiModel, "llama3.2");
            p.Children.Add(Field("Chat endpoint", endpoint));
            p.Children.Add(Field("Model name", model));
            Gap(p);
            p.Children.Add(Pill("Save settings", "check", () => { if (!Uri.TryCreate(endpoint.Text, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) { MessageBox.Show(owner, "Enter a valid HTTP or HTTPS endpoint."); return; } S.AiEndpoint = endpoint.Text.Trim(); S.AiModel = model.Text.Trim(); Save(); }, true));
            Gap(p, 20);
            p.Children.Add(Pill("Get Ollama", "external", () => TryOpen("https://ollama.com/")));
            return p;
        }
        if (name == "Pomodoro" || name == "Health" || name == "Day Progress")
        {
            var p = SettingsHeading(name == "Day Progress" ? "Day Progress" : name, name, name == "Pomodoro" ? "Adjust the length of focus sessions and breaks." : name == "Health" ? "Set your daily goal and reminder intervals." : "Set the hours used to calculate your waking day.");
            var inputs = new List<(TextBox Input, string Key, int Min, int Max)>();
            void FieldInt(string label, string key, int value, int min, int max)
            {
                var input = Input(value.ToString(), label);
                p.Children.Add(Field(label, input));
                inputs.Add((input, key, min, max));
            }
            if (name == "Pomodoro")
            {
                FieldInt("Focus · minutes", "focus", S.FocusMinutes, 1, 180);
                FieldInt("Short break · minutes", "short", S.ShortMinutes, 1, 60);
                FieldInt("Long break · minutes", "long", S.LongMinutes, 1, 120);
            }
            else if (name == "Health")
            {
                FieldInt("Daily water goal · glasses", "water", S.WaterGoal, 1, 30);
                FieldInt("Water reminder · minutes (0 disables)", "wr", S.WaterReminder, 0, 240);
                FieldInt("Eye break reminder · minutes (0 disables)", "er", S.EyeReminder, 0, 240);
                FieldInt("Movement reminder · minutes (0 disables)", "mr", S.MovementReminder, 0, 240);
            }
            else
            {
                FieldInt("Wake hour · 0–22", "wake", S.WakeHour, 0, 22);
                FieldInt("Bed hour · 1–24", "bed", S.BedHour, 1, 24);
            }
            Gap(p);
            p.Children.Add(Pill("Save settings", "check", () => { try { var values = inputs.ToDictionary(x => x.Key, x => (int)Number(x.Input, x.Min, x.Max)); if (name == "Pomodoro") { S.FocusMinutes = values["focus"]; S.ShortMinutes = values["short"]; S.LongMinutes = values["long"]; if (!D.Timer.Running) D.Timer = new TimerRecord { Mode = D.Timer.Mode, Remaining = Duration(D.Timer.Mode), SessionDuration = Duration(D.Timer.Mode) }; } else if (name == "Health") { S.WaterGoal = values["water"]; S.WaterReminder = values["wr"]; S.EyeReminder = values["er"]; S.MovementReminder = values["mr"]; } else { if (values["bed"] <= values["wake"]) throw new Exception("Bed hour must follow wake hour."); S.WakeHour = values["wake"]; S.BedHour = values["bed"]; } Save(); if (expanded) RenderTab(); } catch (Exception ex) { MessageBox.Show(owner, ex.Message, name); } }, true));
            return p;
        }
        if (name == "Notifications")
        {
            var p = SettingsHeading("bell", "Notifications", "Orbit reminders appear in the island. Optional system notification capture depends on Windows permissions and may require packaged app identity.");
            var reminders = Check("Show reminder balloons", S.Reminders);
            reminders.Click += (_, _) => { S.Reminders = reminders.IsChecked == true; Save(); };
            p.Children.Add(reminders);
            var capture = Check("Capture Windows system notifications", S.SystemNotifications);
            capture.Click += async (_, _) => { S.SystemNotifications = capture.IsChecked == true; Save(); if (S.SystemNotifications) await RefreshSystemNotifications(); capture.IsChecked = S.SystemNotifications; };
            p.Children.Add(capture);
            Gap(p, 20);
            p.Children.Add(Pill("Windows notification settings", "external", () => TryOpen("ms-settings:notifications")));
            return p;
        }
        if (name == "Screen Time")
        {
            var p = SettingsHeading("Screen Time", "Screen Time", "Record foreground process names and active time. Tracking begins when enabled; previous Windows usage history is not imported. Idle periods longer than five minutes are excluded.");
            var track = Check("Track app usage on this PC", S.TrackApps);
            track.Click += (_, _) => { S.TrackApps = track.IsChecked == true; Save(); if (tab == "Screen Time") RenderTab(); };
            p.Children.Add(track);
            Gap(p, 20);
            p.Children.Add(Pill("Export usage CSV", "download", ExportScreenTime));
            return p;
        }
        if (name == "Data & startup")
        {
            var p = SettingsHeading("disk", "Data & startup", "Keep a backup or transfer your notes, tasks, calendar and usage history.");
            p.Children.Add(Row(Pill("Export backup", "download", ExportBackup), Pill("Import backup", "folder", () => ImportBackup(owner))));
            Gap(p, 22);
            p.Children.Add(Pill("Open data folder", "folder", () => TryOpen(store.DirectoryPath)));
            Gap(p, 20);
            p.Children.Add(T(store.DirectoryPath, 12, Muted));
            Gap(p, 30);
            p.Children.Add(T("Start with Windows", 19, Fg, true));
            Gap(p, 14);
            p.Children.Add(Row(Pill("Enable startup", "check", () => SetStartup(true)), Pill("Disable startup", "close", () => SetStartup(false))));
            return p;
        }
        if (name == "About")
        {
            var p = SettingsHeading("Home", "Orbit Island", "A native C#/.NET Windows notch app, redesigned using your Poni screenshots as a visual reference.");
            p.Children.Add(T("Version 0.3 · portable Windows x64", 17, Fg, true));
            Gap(p, 20);
            p.Children.Add(Pill("Poni reference website", "external", () => TryOpen("https://poni.web.id/en/")));
            Gap(p, 10);
            p.Children.Add(Pill("Project on GitHub", "external", () => TryOpen("https://github.com/vmrishi/Dynamic_Island_for_Windows")));
            Gap(p, 25);
            p.Children.Add(T("Windows integration limits", 18, Fg, true));
            Gap(p, 12);
            p.Children.Add(T("New Wi-Fi setup and Bluetooth audio switching open Windows Settings. Temperature is unavailable without a compatible sensor provider. Calendar events are local with basic ICS import/export. AI Chat uses Ollama. System notification access may require packaged app identity. This project is independent of Poni.", 13, Muted));
            return p;
        }
        var icon = name == "Media" ? "music" : name == "Bluetooth & Wi-Fi" ? "Connections" : name;
        var destinationModule = name == "Media" ? "Home" : name == "Bluetooth & Wi-Fi" ? "Connections" : name;
        var descriptions = new Dictionary<string, string> { { "Dashboard", "A quick overview of toggles, your forecast, apps and day progress. Choose your weather location in Weather settings." }, { "Control", "Adjust system volume and supported display brightness. Use the split Wi-Fi and Bluetooth tiles to open network and device lists." }, { "Shelf", "Drop desktop shortcuts, files or folders into the shelf. Add applications with the installed-app picker. Remove a shortcut with its × button; original files stay untouched." }, { "Todo", "Press Enter to add a task. Use the clock to set a reminder. Complete tasks with their checkbox, move them to Trash, restore them or delete them permanently." }, { "Notes", "Create a note on the left and write on the right. OK saves it. Drafts are also saved before switching notes or leaving the tab." }, { "Media", "Play media in an app that exposes Windows media sessions. Orbit shows the current track and supports previous, play/pause and next media keys." }, { "Calendar", "Manage local events and import or export basic ICS files. Google and Outlook account synchronization are not implemented." }, { "Bluetooth & Wi-Fi", "Wi-Fi uses saved Windows profiles. New networks, Bluetooth pairing and audio device switching use Windows Settings. Bluetooth connection state is read from paired classic Bluetooth devices." }, { "System", "CPU, memory, network throughput and disk are read from this PC. Temperature requires a supported sensor provider and is shown as unavailable rather than estimated." } };
        var generic = SettingsHeading(icon, name, descriptions.GetValueOrDefault(name, "Customize this module through its view."));
        generic.Children.Add(Pill("Open " + name, "chevron", () => { Go(destinationModule); owner.Close(); }, true));
        Gap(generic, 20);
        generic.Children.Add(Pill("Visibility and order", "Dashboard", () => showSettings?.Invoke("Modules")));
        return generic;
    }
    void MoveModule(string module, int direction)
    {
        int i = S.Modules.IndexOf(module), next = Math.Clamp(i + direction, 0, S.Modules.Count - 1);
        S.Modules.RemoveAt(i);
        S.Modules.Insert(next, module);
        Save();
        Build();
        showSettings?.Invoke("Modules");
    }
}
