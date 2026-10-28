using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace OrbitIsland;

public class Preferences
{
    public string Name { get; set; } = "Orbit Island"; public string Accent { get; set; } = "#1988FF";
    public bool AttachToTop { get; set; } = true; public bool Light
    {
        get; set;
    }
    public double Width { get; set; } = 1080; public double Opacity { get; set; } = .98; public double Top { get; set; } = 10; public int Monitor
    {
        get; set;
    }
    public bool AlwaysOnTop { get; set; } = true; public bool AutoCollapse { get; set; } = true; public string IslandColor { get; set; } = "#000000"; public double TextScale { get; set; } = 1; public double IconSize { get; set; } = 18; public string AiEndpoint { get; set; } = "http://localhost:11434/api/chat"; public string AiModel { get; set; } = "llama3.2"; public bool SystemNotifications
    {
        get; set;
    }
    public bool OpenOnHover { get; set; } = true; public bool TrackApps
    {
        get; set;
    }
    public bool Reminders { get; set; } = true;
    public int FocusMinutes { get; set; } = 25; public int ShortMinutes { get; set; } = 5; public int LongMinutes { get; set; } = 15; public int WaterGoal { get; set; } = 8;
    public int WaterReminder { get; set; } = 60; public int EyeReminder { get; set; } = 20; public int MovementReminder { get; set; } = 60; public int WakeHour { get; set; } = 7; public int BedHour { get; set; } = 22;
    public string City { get; set; } = ""; public double? Latitude
    {
        get; set;
    }
    public double? Longitude
    {
        get; set;
    }
    public List<string> Modules { get; set; } = new() { "Home", "Dashboard", "Control", "Shelf", "Todo", "Notes", "Pomodoro", "Health", "Weather", "Screen Time", "Day Progress", "Connections", "System", "AI Chat", "Calendar" };
    public List<string> Hidden { get; set; } = new();
}
public class TodoItem
{
    public string Text { get; set; } = ""; public DateTime? Due
    {
        get; set;
    }
    public bool Done
    {
        get; set;
    }
    public bool Trashed
    {
        get; set;
    }
    public bool Alerted
    {
        get; set;
    }
}
public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); public string Body { get; set; } = ""; public DateTime Updated { get; set; } = DateTime.Now; public string Title => string.IsNullOrWhiteSpace(Body) ? "Untitled note" : Body.Split('\n')[0].Trim();
}
public class CalendarItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); public string Title { get; set; } = ""; public DateTime Start
    {
        get; set;
    }
    public bool Alerted
    {
        get; set;
    }
}
public class Notice
{
    public DateTime Time { get; set; } = DateTime.Now; public string Title { get; set; } = ""; public string Detail { get; set; } = ""; public bool Read
    {
        get; set;
    }
}
public class DayRecord
{
    public Dictionary<int, double> Hourly { get; set; } = new(); public int AppSwitches
    {
        get; set;
    }
    public DateTime? LastMovement
    {
        get; set;
    }
    public int Water
    {
        get; set;
    }
    public int Movement
    {
        get; set;
    }
    public int Breathing
    {
        get; set;
    }
    public int Eyes
    {
        get; set;
    }
    public int FocusSessions
    {
        get; set;
    }
    public double FocusSeconds
    {
        get; set;
    }
    public Dictionary<string, double> Apps { get; set; } = new();
}
public class TimerRecord
{
    public string Mode { get; set; } = "Focus"; public bool Running
    {
        get; set;
    }
    public double Remaining { get; set; } = 1500; public DateTimeOffset? Deadline
    {
        get; set;
    }
    public DateTimeOffset? StopwatchStart
    {
        get; set;
    }
    public double StopwatchSeconds
    {
        get; set;
    }
    public double SessionDuration { get; set; } = 1500;
}
public class AppData
{
    public Preferences Settings { get; set; } = new(); public List<TodoItem> Todos { get; set; } = new(); public List<Note> Notes { get; set; } = new(); public List<CalendarItem> Events { get; set; } = new(); public List<string> Shelf { get; set; } = new(); public List<Notice> Notices { get; set; } = new(); public List<string> SeenWindowsNotices { get; set; } = new(); public Dictionary<string, DayRecord> Days { get; set; } = new(); public TimerRecord Timer { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore] public DayRecord Today => Day(DateTime.Today); public DayRecord Day(DateTime date)
    {
        var key = date.ToString("yyyy-MM-dd");
        if (!Days.ContainsKey(key))
            Days[key] = new();
        return Days[key];
    }
}
public class Store
{
    public string DirectoryPath
    {
        get;
    }
    public string FilePath => Path.Combine(DirectoryPath, "island.json"); public AppData Data
    {
        get; set;
    }
    public string? RecoveryWarning
    {
        get;
    }
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public Store(string directory)
    {
        DirectoryPath = directory;
        Directory.CreateDirectory(directory);
        try
        {
            Data = File.Exists(FilePath) ? JsonSerializer.Deserialize<AppData>(File.ReadAllText(FilePath), JsonOptions) ?? new() : new();
        }
        catch { string recovery = FilePath + ".unreadable-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"); File.Copy(FilePath, recovery, true); Data = new(); RecoveryWarning = "Unreadable data preserved at " + recovery; try { Data = JsonSerializer.Deserialize<AppData>(File.ReadAllText(FilePath + ".bak"), JsonOptions) ?? new(); } catch { } }
        Normalize();
    }
    public void Normalize()
    {
        var s = Data.Settings;
        s.Width = Math.Clamp(s.Width, 800, 1500);
        s.Opacity = Math.Clamp(s.Opacity, .65, 1);
        s.Top = Math.Clamp(s.Top, 6, 150);
        s.TextScale = Math.Clamp(s.TextScale, .85, 1.3);
        s.IconSize = Math.Clamp(s.IconSize, 14, 24);
        s.FocusMinutes = Math.Clamp(s.FocusMinutes, 1, 180);
        s.ShortMinutes = Math.Clamp(s.ShortMinutes, 1, 60);
        s.LongMinutes = Math.Clamp(s.LongMinutes, 1, 120);
        s.WaterGoal = Math.Clamp(s.WaterGoal, 1, 30);
        s.WakeHour = Math.Clamp(s.WakeHour, 0, 22);
        s.BedHour = Math.Clamp(s.BedHour, s.WakeHour + 1, 24);
        var names = new Preferences().Modules;
        s.Modules = s.Modules.Where(names.Contains).Distinct().Concat(names.Where(n => !s.Modules.Contains(n))).ToList();
        s.Hidden = s.Hidden.Where(names.Contains).Distinct().ToList();
        if (s.Modules.All(s.Hidden.Contains))
            s.Hidden.Remove("Home");
        s.Name = string.IsNullOrWhiteSpace(s.Name) ? "Orbit Island" : s.Name[..Math.Min(s.Name.Length, 40)];
    }
    public void Save()
    {
        string temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(Data, JsonOptions));
        if (File.Exists(FilePath))
            File.Copy(FilePath, FilePath + ".bak", true);
        File.Move(temp, FilePath, true);
    }
    public static double DayProgress(DateTime now, int wake, int bed) => Math.Clamp((now.TimeOfDay.TotalHours - wake) / (bed - wake), 0, 1);
}


