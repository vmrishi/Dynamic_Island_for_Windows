using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
namespace OrbitIsland;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        bool portable = File.Exists(System.IO.Path.Combine(AppContext.BaseDirectory, "portable.mode"));
        string dir = portable ? System.IO.Path.Combine(AppContext.BaseDirectory, "Data") : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OrbitDynamicIsland");
        var di = Array.IndexOf(args, "--data-dir");
        if (di >= 0 && args.Length > di + 1)
            dir = System.IO.Path.GetFullPath(args[di + 1]);
        if (args.Contains("--self-test"))
        {
            SelfTest.Run(dir);
            return;
        }
        if (args.Contains("--diagnostics"))
        {
            SelfTest.Diagnostics(dir);
            return;
        }
        if (args.Contains("--visual-test"))
            dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "OrbitVisualTest-" + Guid.NewGuid().ToString("N"));
        using var mutex = new Mutex(true, "Local\\OrbitIsland-v01-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(dir)))[..16], out bool first);
        if (!first)
            return;
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.DispatcherUnhandledException += (s, e) => { try { Directory.CreateDirectory(dir); File.AppendAllText(System.IO.Path.Combine(dir, "errors.log"), DateTime.Now + " " + e.Exception + Environment.NewLine); MessageBox.Show(e.Exception.Message, "Orbit Island"); } catch { } e.Handled = true; };
        if (args.Contains("--visual-test"))
        {
            var testDir = dir;
            var window = new Island(new Store(testDir), true);
            window.Loaded += async (_, _) => { try { await window.CaptureVisualChecks(System.IO.Path.Combine(testDir, "screenshots")); File.WriteAllText(System.IO.Path.Combine(testDir, "result.txt"), "PASS"); } catch (Exception ex) { File.WriteAllText(System.IO.Path.Combine(testDir, "result.txt"), ex.ToString()); } finally { window.Close(); } };
            app.Run(window);
            return;
        }
        app.Run(new Island(new Store(dir), args.Contains("--expanded")));
    }
}
public partial class Island : Window
{
    readonly Store store; AppData D => store.Data; Preferences S => D.Settings;
    Grid? designRoot; Grid? overlay; bool modalOpen; int transitionVersion;
    Grid notchScene = new(); NotchSurface notchSurface = new(); Border shell = new(); DockPanel layout = new(); StackPanel nav = new(); Grid body = new(); TextBlock status = new(); TextBlock clock = new(); Button bell = new();
    DispatcherTimer tick = new() { Interval = TimeSpan.FromSeconds(1) }; DispatcherTimer leave = new() { Interval = TimeSpan.FromMilliseconds(420) }; Forms.NotifyIcon tray = new();
    string tab = "Home", todoFilter = "Active", connectionTab = "Wi-Fi", notesId = ""; bool expanded, keepAwake, busyMedia; int seconds; DateTime lastTick = DateTime.UtcNow, lastWater = DateTime.Now, lastEye = DateTime.Now, lastMovement = DateTime.Now;
    DateTime selectedDate = DateTime.Today; string healthMode = ""; DateTimeOffset? healthEnd; int healthLength;
    JsonElement? media, weather; string mediaError = "", weatherError = "", connectionError = "", brightnessError = ""; int? brightness;
    List<(string Name, string State)> devices = new(); Dictionary<string, string> radios = new();
    (double Cpu, double Memory, double UsedGb, double TotalGb, double Rx, double Tx) metrics;
    List<double> cpuHistory = new(), memHistory = new(), rxHistory = new(); TextBlock? timerText, timerState, miniTimer, healthText; Window? settingsWindow;

    Brush Accent => Brush(S.Accent, "#58B9FF"); Brush Fg => Brush(S.Light ? "#172033" : "#F5F6FA"); Brush Muted => Brush(S.Light ? "#657084" : "#9195A3"); Brush Surface => Brush(S.Light ? "#EFF2F7" : "#101010"); Brush CardBorder => Brush(S.Light ? "#DFE4ED" : "#222222");
    public static SolidColorBrush Brush(string color, string fallback = "#58B9FF")
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }
        catch { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback)); }
    }
    public Island(Store s, bool open)
    {
        store = s;
        Title = "Orbit Island";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
        FontSize = 14 * S.TextScale;
        Foreground = Fg;
        Resources = new ResourceDictionary { Source = new Uri("/OrbitIsland;component/Theme.xaml", UriKind.Relative) };
        notchScene.Margin = new Thickness(12, 0, 12, 22);
        Content = notchScene;
        notchScene.Children.Add(notchSurface);
        notchScene.Children.Add(shell);
        shell.Margin = new Thickness(18, 0, 18, 0);
        shell.Background = Brushes.Transparent;
        shell.BorderThickness = new Thickness(0);
        notchSurface.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 22, Opacity = .32, ShadowDepth = 8 };
        notchScene.SizeChanged += (_, _) => { if (notchScene.ActualWidth > 80 && notchScene.ActualHeight > 36) { shell.Clip = NotchSurface.CreateClip(notchScene.ActualWidth, notchScene.ActualHeight, notchSurface.BottomRadius); } };
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
        SourceInitialized += (_, _) => { var h = new WindowInteropHelper(this).Handle; HwndSource.FromHwnd(h)?.AddHook(Hook); if (!Native.RegisterHotKey(h, 1, 0x4005, 0x4F)) Notify("Shortcut unavailable", "Alt+Shift+O is used by another application. Use the tray icon.", false); };
        tray.Icon = System.Drawing.SystemIcons.Application;
        tray.Text = "Orbit Island · Alt+Shift+O";
        tray.Visible = true;
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(() => { Show(); SetExpanded(!expanded); Activate(); });
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open island", null, (_, _) => Dispatcher.Invoke(() => { Show(); SetExpanded(true); Activate(); }));
        menu.Items.Add("Customize", null, (_, _) => Dispatcher.Invoke(OpenSettings));
        menu.Items.Add("Quit", null, (_, _) => Dispatcher.Invoke(Close));
        tray.ContextMenuStrip = menu;
        notchScene.MouseEnter += (_, _) => { leave.Stop(); if (!expanded && S.OpenOnHover) SetExpanded(true); };
        notchScene.MouseLeave += (_, _) => { if (S.AutoCollapse) leave.Start(); };
        leave.Tick += (_, _) => { leave.Stop(); if (!IsMouseOver && settingsWindow == null && OwnedWindows.Count == 0 && !modalOpen) SetExpanded(false); };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { if (modalOpen) CloseOverlay(); else SetExpanded(false); e.Handled = true; } if (e.Key == Key.OemComma && (Keyboard.Modifiers & ModifierKeys.Control) != 0) OpenSettings(); };
        Closed += (_, _) => { RecordUsage(); SaveNoteDraft(); tick.Stop(); leave.Stop(); Native.UnregisterHotKey(new WindowInteropHelper(this).Handle, 1); Native.SetThreadExecutionState(0x80000000); tray.Dispose(); mediaSessions.Dispose(); settingsWindow?.Close(); Save(); };
        mediaSessions.Changed += () => Dispatcher.BeginInvoke(new Action(async () => await RefreshMedia()));
        tick.Tick += Tick;
        tick.Start();
        SetExpanded(open);
        Loaded += async (_, _) => { if (store.RecoveryWarning != null) Notify("Data recovered", store.RecoveryWarning); await RefreshMedia(); await RefreshBrightness(); await RefreshConnections(); if (S.Latitude != null) await RefreshWeather(); };
    }
    IntPtr Hook(IntPtr h, int msg, IntPtr w, IntPtr l, ref bool handled)
    {
        if (msg == 0x312 && w.ToInt32() == 1)
        {
            Show();
            SetExpanded(!expanded);
            Activate();
            handled = true;
        }
        return IntPtr.Zero;
    }
    void Save()
    {
        try
        {
            store.Save();
        }
        catch (Exception e) { status.Text = "Save failed: " + e.Message; }
    }
    void Place()
    {
        var screens = Forms.Screen.AllScreens;
        var scr = screens[Math.Clamp(S.Monitor, 0, screens.Length - 1)];
        var dpi = VisualTreeHelper.GetDpi(this);
        var size = NotchLayout.Size(S.Width, scr.Bounds.Width / dpi.DpiScaleX, tab, S.TextScale, S.IconSize, expanded, modalOpen ? 500 : 0);
        Width = size.Width;
        Height = size.Height;
        Left = scr.Bounds.Left / dpi.DpiScaleX + (scr.Bounds.Width / dpi.DpiScaleX - Width) / 2;
        Top = scr.Bounds.Top / dpi.DpiScaleY + (S.AttachToTop ? 0 : S.Top);
        notchSurface.BottomRadius = expanded ? 38 : 18;
        notchSurface.InvalidateVisual();
    }
    void SetExpanded(bool value)
    {
        if (!value && tab == "Notes")
            SaveNoteDraft();
        bool animate = IsLoaded && expanded != value;
        double oldWidth = ActualWidth, oldHeight = ActualHeight, oldLeft = Left;
        BeginAnimation(WidthProperty, null);
        BeginAnimation(HeightProperty, null);
        BeginAnimation(LeftProperty, null);
        transitionVersion++;
        Title = S.Name;
        tray.Text = (S.Name + " · Alt+Shift+O")[..Math.Min(63, (S.Name + " · Alt+Shift+O").Length)];
        expanded = value;
        Resources["SliderActiveBrush"] = S.Light ? Brush("#29292C") : Brushes.White;
        Resources["SliderTrackBrush"] = S.Light ? Brush("#CDCDD4") : Brush("#363638");
        Topmost = S.AlwaysOnTop;
        Opacity = S.Opacity;
        Foreground = Fg;
        FontSize = 14 * S.TextScale;
        notchSurface.Fill = Brush(S.Light ? "#FBFCFE" : S.IslandColor, "#000000");
        notchSurface.InvalidateVisual();
        Place();
        Build();
        if (animate)
            AnimateBounds(oldWidth, oldHeight, oldLeft);
    }
    void AnimateBounds(double fromWidth, double fromHeight, double fromLeft)
    {
        if (!IsLoaded || fromWidth <= 0 || !SystemParameters.ClientAreaAnimation)
            return;
        var easing = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
        foreach (var pair in new[] { (WidthProperty, fromWidth), (HeightProperty, fromHeight), (LeftProperty, fromLeft) })
        {
            double target = (double)GetAnimationBaseValue(pair.Item1);
            BeginAnimation(pair.Item1, new System.Windows.Media.Animation.DoubleAnimation(pair.Item2, target, TimeSpan.FromMilliseconds(260)) { EasingFunction = easing, FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop });
        }
    }
    void Build()
    {
        timerText = null;
        timerState = null;
        healthText = null;
        miniTimer = null;
        layout = new DockPanel { Margin = expanded ? new Thickness(28, 14, 28, 24) : new Thickness(0) };
        if (expanded)
        {
            designRoot = new Grid { Width = NotchLayout.DesignWidth, Height = Math.Max(NotchLayout.DesignHeight(tab, S.TextScale, S.IconSize), modalOpen ? 500 : 0) };
            designRoot.Children.Add(layout);
            shell.Child = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.Both, Child = designRoot, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };
        }
        else
        {
            designRoot = null;
            shell.Child = layout;
        }
        if (!expanded)
        {
            var mini = new Grid { Background = Brushes.Transparent };
            clock = T(DateTime.Now.ToString("h:mm tt").ToLowerInvariant(), 18, Fg, true);
            clock.HorizontalAlignment = HorizontalAlignment.Center;
            mini.Children.Add(clock);
            mini.MouseLeftButtonUp += (_, _) => SetExpanded(true);
            layout.Children.Add(mini);
            return;
        }
        var header = new Grid { Margin = new Thickness(0, 0, 0, 16), Height = 42 };
        header.ColumnDefinitions.Add(new());
        header.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        nav = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var n in S.Modules.Where(x => !S.Hidden.Contains(x)))
        {
            var active = n == tab;
            var p = Row(Icon(n, 18, active ? Fg : Muted));
            if (active)
            {
                var label = T(n == "Connections" ? "Bluetooth" : n, 14, Fg, true);
                label.Margin = new Thickness(7, 0, 0, 0);
                p.Children.Add(label);
            }
            var button = Btn("", () => Go(n), active, n);
            button.Content = p;
            button.Background = active ? Brush(S.Light ? "#D8D8DD" : "#303030") : Brushes.Transparent;
            button.Padding = new Thickness(9, 7, 9, 7);
            button.Margin = new Thickness(0, 0, 1, 0);
            nav.Children.Add(button);
            if (active)
                button.Loaded += (_, _) => button.BringIntoView();
        }
        var ns = new ScrollViewer { Content = nav, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
        ns.PreviewMouseWheel += (_, e) => { ns.ScrollToHorizontalOffset(ns.HorizontalOffset - e.Delta); e.Handled = true; };
        header.Children.Add(new Border { Background = Brush(S.Light ? "#EEEEF0" : "#0A0A0A"), CornerRadius = new CornerRadius(20), Child = ns, Margin = new Thickness(0, 0, 28, 0), VerticalAlignment = VerticalAlignment.Center });
        var actions = Row(IconButton("eye-off", () => SetExpanded(false), "Collapse · Esc"), IconButton("coffee", ToggleAwake, "Keep awake", keepAwake ? Brush("#FFAD20") : Muted));
        bell = IconButton("bell", () => Go("Notifications"), "Notifications");
        var badge = new Grid();
        badge.Children.Add(bell);
        int unread = D.Notices.Count(x => !x.Read);
        if (unread > 0)
        {
            var count = T(unread > 9 ? "9+" : unread.ToString(), 10, Brushes.White, true);
            badge.Children.Add(new Border { Background = Brush("#FF4945"), CornerRadius = new CornerRadius(10), Padding = new Thickness(4, 1, 4, 1), Child = count, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false, Margin = new Thickness(0, -2, 1, 0) });
        }
        actions.Children.Add(badge);
        actions.Children.Add(IconButton("settings", OpenSettings, "Settings · Ctrl+,"));
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        DockPanel.SetDock(header, Dock.Top);
        layout.Children.Add(header);
        var footer = new DockPanel { Visibility = Visibility.Collapsed };
        clock = T("");
        status = T("");
        footer.Children.Add(clock);
        footer.Children.Add(status);
        DockPanel.SetDock(footer, Dock.Bottom);
        layout.Children.Add(footer);
        body = new Grid();
        layout.Children.Add(body);
        RenderTab();
    }
    async void Go(string name)
    {
        double beforeWidth = ActualWidth, beforeHeight = ActualHeight, beforeLeft = Left;
        if (tab == "Notes")
            SaveNoteDraft();
        if (name == "Calendar" && tab != "Calendar")
            displayedMonth = CalendarRules.Month(selectedDate);
        tab = name;
        expanded = true;
        Place();
        Build();
        AnimateBounds(beforeWidth, beforeHeight, beforeLeft);
        AnimateBody();
        if (name == "Weather")
            await RefreshWeather();
        if (name == "Control")
        {
            await Dispatcher.Yield(DispatcherPriority.Render);
            await RefreshBrightness();
        }
        if (name is "Connections" or "Control" or "Dashboard")
            await RefreshConnections();
    }
    void RenderTab()
    {
        body.Children.Clear();
        try
        {
            switch (tab)
            {
                case "Home":
                    Home();
                    break;
                case "Dashboard":
                    Dashboard();
                    break;
                case "Control":
                    Control();
                    break;
                case "Shelf":
                    Shelf();
                    break;
                case "Todo":
                    Todos();
                    break;
                case "Notes":
                    Notes();
                    break;
                case "Pomodoro":
                    Pomodoro();
                    break;
                case "Health":
                    Health();
                    break;
                case "Weather":
                    Weather();
                    break;
                case "Screen Time":
                    ScreenTime();
                    break;
                case "Day Progress":
                    DayProgress();
                    break;
                case "Connections":
                    Connections();
                    break;
                case "System":
                    SystemPage();
                    break;
                case "AI Chat":
                    AiChat();
                    break;
                case "Calendar":
                    CalendarPage();
                    break;
                case "Notifications":
                    Notifications();
                    break;
            }
        }
        catch (Exception e) { Empty("This view is unavailable", e.Message); }
    }
    TextBlock T(string text, double size = 14, Brush? color = null, bool bold = false) => new() { Text = text, FontSize = size * S.TextScale, Foreground = color ?? Fg, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
    Button Btn(string text, Action action, bool active = false, string? tip = null)
    {
        var b = new Button { Tag = new CornerRadius(15), Content = text, Background = active ? Brush(S.Light ? "#DDE9F5" : "#303030") : Brush(S.Light ? "#E6EBF2" : "#191919"), Foreground = Fg, ToolTip = null };
        AutomationProperties.SetName(b, tip ?? text);
        b.Click += (_, _) => { try { action(); } catch (Exception e) { Notify("Action unavailable", e.Message); } };
        return b;
    }
    Button Primary(string text, Action action)
    {
        var b = Btn(text, action);
        b.Background = Accent;
        b.Foreground = Brush("#08121F");
        b.FontWeight = FontWeights.SemiBold;
        return b;
    }
    TextBox Input(string value = "", string hint = "", bool multiline = false)
    {
        var b = new TextBox { Text = value, Tag = hint, Background = Surface, Foreground = Fg, BorderBrush = CardBorder, BorderThickness = new Thickness(0), Padding = new Thickness(16, 11, 16, 11), ToolTip = null, AcceptsReturn = multiline, TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap, VerticalContentAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(b, hint);
        return b;
    }
    StackPanel Col(params UIElement[] children)
    {
        var p = new StackPanel();
        foreach (var c in children)
            p.Children.Add(c);
        return p;
    }
    StackPanel Row(params UIElement[] children)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (var c in children)
            p.Children.Add(c);
        return p;
    }
    Border Card(UIElement child, int padding = 18) => new() { Background = Surface, BorderBrush = CardBorder, BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(22), ClipToBounds = true, Padding = new Thickness(padding), Child = child, Margin = new Thickness(0, 0, 12, 0) };
    Grid Columns(params double[] widths)
    {
        var g = new Grid();
        foreach (var w in widths)
            g.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(w, GridUnitType.Star)
            });
        return g;
    }
    void Put(Grid grid, UIElement child, int col, int row = 0)
    {
        Grid.SetColumn(child, col);
        Grid.SetRow(child, row);
        grid.Children.Add(child);
    }
    ScrollViewer Scroll(UIElement child) => new() { Content = child, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    void Gap(StackPanel p, double h = 12) => p.Children.Add(new Border { Height = h });
    void Empty(string title, string detail, UIElement? action = null)
    {
        var p = Col(T(title, 25, Fg, true), T(detail, 14, Muted));
        p.HorizontalAlignment = HorizontalAlignment.Center;
        p.VerticalAlignment = VerticalAlignment.Center;
        Gap(p, 20);
        if (action != null)
            p.Children.Add(action);
        body.Children.Add(p);
    }
    void Notify(string title, string detail, bool balloon = true)
    {
        D.Notices.Insert(0, new Notice { Title = title, Detail = detail });
        D.Notices = D.Notices.Take(200).ToList();
        Save();
        if (balloon && S.Reminders)
        {
            tray.BalloonTipTitle = title;
            tray.BalloonTipText = detail;
            tray.ShowBalloonTip(5000);
        }
        if (expanded)
        {
            status.Text = title + " · " + detail;
            bell.Foreground = Accent;
        }
    }
    void TryOpen(string target)
    {
        try
        {
            Native.Open(target);
        }
        catch (Exception e) { Notify("Could not open", e.Message); }
    }
    readonly UsageRecorder usageRecorder = new();
    void RecordUsage()
    {
        if (!S.TrackApps)
        {
            usageRecorder.Sample(D, DateTimeOffset.Now, null, false);
            lastForeground = "Tracking paused";
            return;
        }
        var sample = Native.ForegroundSample(mediaSnapshot?.Status == "Playing" ? mediaSessions.PlayingSourceId : null);
        lastForeground = sample.Label;
        if (sample.Key != null)
        {
            D.AppLabels[sample.Key] = sample.Label;
            if (sample.Path != null)
                D.AppPaths[sample.Key] = sample.Path;
        }
        usageRecorder.Sample(D, DateTimeOffset.Now, sample.Key, S.TrackApps);
    }
    async void Tick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var elapsed = Math.Clamp((now - lastTick).TotalSeconds, 0, 3);
        lastTick = now;
        seconds++;
        clock.Text = expanded ? DateTime.Now.ToString("ddd, dd MMM · HH:mm") : DateTime.Now.ToString("h:mm tt").ToLowerInvariant();
        if (keepAwake)
            Native.SetThreadExecutionState(0x80000001);
        RecordUsage();
        if (D.Timer.Running && D.Timer.Mode != "Stopwatch" && TimerSeconds() <= 0)
        {
            D.Timer.Running = false;
            D.Timer.Remaining = 0;
            if (D.Timer.Mode == "Focus")
            {
                var completed = D.Day(D.Timer.Deadline?.LocalDateTime.Date ?? DateTime.Today);
                completed.FocusSessions++;
                completed.FocusSeconds += D.Timer.SessionDuration;
            }
            Notify(D.Timer.Mode + " complete", D.Timer.Mode == "Focus" ? "Take a break. Your session has been saved." : "Your break has finished.");
            if (tab == "Pomodoro" && expanded)
                RenderTab();
        }
        if (timerText != null)
            timerText.Text = FormatTime(TimerSeconds());
        if (timerState != null)
            timerState.Text = D.Timer.Running ? "In progress · you have this" : "Ready when you are";
        if (miniTimer != null)
            miniTimer.Text = D.Timer.Running ? FormatTime(TimerSeconds()) : "⌄";
        if (healthEnd != null)
        {
            var remaining = (int)Math.Ceiling((healthEnd.Value - DateTimeOffset.Now).TotalSeconds);
            if (remaining <= 0)
            {
                if (healthMode == "Eyes")
                {
                    D.Today.Eyes++;
                    lastEye = DateTime.Now;
                }
                if (healthMode == "Movement")
                {
                    D.Today.Movement++;
                    lastMovement = DateTime.Now;
                    D.Today.LastMovement = lastMovement;
                }
                if (healthMode == "Breathing")
                    D.Today.Breathing++;
                healthEnd = null;
                Notify("Break complete", "A little reset goes a long way.");
                if (tab == "Health" && expanded)
                    RenderTab();
            }
            else if (healthText != null)
                healthText.Text = healthMode == "Breathing" ? BreathCue(healthLength - remaining) + " · " + remaining + "s" : healthMode + " · " + remaining + "s";
        }
        foreach (var t in D.Todos.Where(t => !t.Done && !t.Trashed && !t.Alerted && t.Due != null && t.Due <= DateTime.Now).ToList())
        {
            t.Alerted = true;
            Notify("Task due", t.Text);
        }
        foreach (var ev in D.Events.Where(x => !x.Alerted && x.Start > DateTime.Now.AddMinutes(-5) && x.Start <= DateTime.Now.AddMinutes(5)).ToList())
        {
            ev.Alerted = true;
            Notify("Coming up", ev.Title + " · " + ev.Start.ToString("HH:mm"));
        }
        if (S.Reminders)
        {
            if (S.WaterReminder > 0 && (DateTime.Now - lastWater).TotalMinutes >= S.WaterReminder)
            {
                lastWater = DateTime.Now;
                Notify("Water break", "Time for a glass of water.");
            }
            if (S.EyeReminder > 0 && (DateTime.Now - lastEye).TotalMinutes >= S.EyeReminder)
            {
                lastEye = DateTime.Now;
                Notify("Rest your eyes", "Look at something 20 feet away for 20 seconds.");
            }
            if (S.MovementReminder > 0 && (DateTime.Now - lastMovement).TotalMinutes >= S.MovementReminder)
            {
                lastMovement = DateTime.Now;
                Notify("Time to move", "Stand up and stretch for a moment.");
            }
        }
        if (seconds % 2 == 0)
        {
            metrics = Native.Metrics();
            AddHistory(cpuHistory, metrics.Cpu);
            AddHistory(memHistory, metrics.Memory);
            AddHistory(rxHistory, metrics.Rx);
            if (tab == "System" && expanded)
                RenderTab();
        }
        if (seconds % 2 == 0)
            await RefreshMedia();
        if (expanded && tab == "Screen Time" && seconds % 5 == 0 && !modalOpen)
            RenderTab();
        if (expanded && tab == "Day Progress" && seconds % 30 == 0 && !modalOpen)
            RenderTab();
        if (seconds % 15 == 0)
        {
            if (expanded && tab is "Connections" or "Control" or "Dashboard")
                await RefreshConnections();
            if (S.SystemNotifications)
                await RefreshSystemNotifications();
            Save();
            if (expanded && tab == "Screen Time")
                RenderTab();
        }
        if (seconds % 1800 == 0 && S.Latitude != null)
            await RefreshWeather();
    }
    static void AddHistory(List<double> list, double value)
    {
        list.Add(value);
        if (list.Count > 50)
            list.RemoveAt(0);
    }
    double TimerSeconds()
    {
        var t = D.Timer;
        if (t.Mode == "Stopwatch")
            return t.StopwatchSeconds + (t.Running && t.StopwatchStart != null ? (DateTimeOffset.Now - t.StopwatchStart.Value).TotalSeconds : 0);
        return t.Running && t.Deadline != null ? Math.Max(0, (t.Deadline.Value - DateTimeOffset.Now).TotalSeconds) : t.Remaining;
    }
    static string FormatTime(double secs)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling(secs)));
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
    }
    static string BreathCue(int seconds) => seconds % 14 < 4 ? "Breathe in" : seconds % 14 < 8 ? "Hold gently" : "Breathe out";
    readonly MediaSessions mediaSessions = new(); MediaSnapshot? mediaSnapshot, displayedMedia;
    Slider? mediaTimeline; TextBlock? mediaElapsedText, mediaRemainingText; bool mediaScrubbing, mediaActionBusy;
    async Task RefreshMedia()
    {
        if (busyMedia)
            return;
        busyMedia = true;
        try
        {
            mediaSnapshot = await mediaSessions.Read();
            media = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                available = mediaSnapshot.Available,
                title = mediaSnapshot.Title,
                artist = mediaSnapshot.Artist,
                source = mediaSnapshot.Source,
                status = mediaSnapshot.Status,
                position = mediaSnapshot.Position,
                duration = mediaSnapshot.Duration,
                art = mediaSnapshot.Art
            });
            mediaError = "";
        }
        catch (Exception e) { mediaError = e.Message; mediaSnapshot = null; media = null; }
        finally { busyMedia = false; }
        if (expanded && tab == "Home" && !mediaScrubbing && !modalOpen)
        {
            var a = displayedMedia;
            var b = mediaSnapshot;
            if (a == null || b == null || a.Available != b.Available || a.Title != b.Title || a.Artist != b.Artist || a.SourceKey != b.SourceKey || a.Status != b.Status || a.Art != b.Art || a.CanToggle != b.CanToggle || a.CanSeek != b.CanSeek || a.CanNext != b.CanNext || a.CanPrevious != b.CanPrevious)
                RenderTab();
            else
            {
                if (mediaTimeline != null)
                {
                    mediaTimeline.Maximum = Math.Max(1, b.Duration);
                    mediaTimeline.Value = b.Position;
                }
                if (mediaElapsedText != null)
                    mediaElapsedText.Text = MediaTime(b.Position);
                if (mediaRemainingText != null)
                    mediaRemainingText.Text = b.Duration > 0 ? "-" + MediaTime(b.Duration - b.Position) : "0:00";
            }
        }
    }
    async System.Threading.Tasks.Task MediaCommand(string command, double position = 0, string? token = null)
    {
        if (mediaActionBusy)
            return;
        mediaActionBusy = true;
        try
        {
            await mediaSessions.Execute(command, position, token ?? mediaSnapshot?.Token);
            await RefreshMedia();
        }
        catch (Exception e) { ShowIslandError("Media control", e.Message); }
        finally { mediaActionBusy = false; }
    }
    bool useDdc; string brightnessDisplayId = "";
    async Task RefreshBrightness()
    {
        var window = new WindowInteropHelper(this).Handle;
        try
        {
            brightness = await Task.Run(() => Native.MonitorBrightness(window));
            useDdc = true;
            brightnessError = "";
        }
        catch { try { brightnessDisplayId = Native.MonitorInstanceId(window); var result = await Native.Broker("brightness", 50, brightnessDisplayId); brightness = result.GetProperty("value").GetInt32(); useDdc = false; brightnessError = ""; } catch (Exception e) { brightness = null; brightnessError = e.Message; } }
        if (expanded && tab == "Control")
            RenderTab();
    }
    async Task SetBrightness(int value)
    {
        try
        {
            if (useDdc)
            {
                var window = new WindowInteropHelper(this).Handle;
                brightness = await Task.Run(() => Native.MonitorBrightness(window, value));
            }
            else
            {
                await Native.Broker("setbrightness", value, brightnessDisplayId);
                brightness = value;
            }
        }
        catch (Exception e) { ShowIslandError("Brightness", e.Message); }
    }
    string MediaString(string key, string fallback = "")
    {
        try
        {
            return media?.GetProperty(key).GetString() ?? fallback;
        }
        catch { return fallback; }
    }
    double MediaNumber(string key)
    {
        try
        {
            return media?.GetProperty(key).GetDouble() ?? 0;
        }
        catch { return 0; }
    }
    bool HasMedia()
    {
        try
        {
            return media?.GetProperty("available").GetBoolean() ?? false;
        }
        catch { return false; }
    }

    FrameworkElement Progress(double value, Brush color, double height = 7)
    {
        return new RoundedProgress { Height = height, Value = double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0, Fill = color, Track = CardBorder, Timeline = height > 30, Margin = new Thickness(0, 5, 0, 5) };
    }
}
