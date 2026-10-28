using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace OrbitIsland;

public static class SelfTest
{
    public static void Diagnostics(string dir)
    {
        Directory.CreateDirectory(dir);
        var lines = new List<string>();
        try
        {
            var a = Native.Audio();
            lines.Add($"Speaker volume: {a.Volume:P0}; muted: {a.Muted}");
        }
        catch (Exception e) { lines.Add("Speaker: " + e.Message); }
        try
        {
            var a = Native.Audio(1);
            lines.Add($"Microphone muted: {a.Muted}");
        }
        catch (Exception e) { lines.Add("Microphone: " + e.Message); }
        try
        {
            var m = Native.Metrics();
            lines.Add($"Memory: {m.Memory}% / {m.TotalGb:0.0} GB");
        }
        catch (Exception e) { lines.Add("Metrics: " + e.Message); }
        File.WriteAllLines(Path.Combine(dir, "diagnostics.txt"), lines);
    }
    public static void Run(string dir)
    {
        Directory.CreateDirectory(dir);
        var results = new List<string>();
        void Check(bool ok, string name)
        {
            if (!ok)
                throw new Exception("FAILED: " + name);
            results.Add("PASS: " + name);
        }
        var testdir = Path.Combine(dir, "store-test-" + Guid.NewGuid().ToString("N"));
        var s = new Store(testdir);
        s.Data.Settings.Name = "Test Island";
        s.Data.Todos.Add(new()
        {
            Text = "Finish design",
            Due = new DateTime(2026, 10, 29, 9, 30, 0)
        });
        s.Data.Notes.Add(new()
        {
            Body = "A note\nSecond line"
        });
        s.Data.Shelf.Add(@"C:\Windows\notepad.exe");
        s.Data.Today.Water = 3;
        s.Data.Today.Apps["Editor"] = 92;
        s.Data.Events.Add(new()
        {
            Title = "Planning",
            Start = new DateTime(2026, 10, 29, 11, 0, 0)
        });
        s.Data.Timer = new()
        {
            Running = true,
            Remaining = 60,
            SessionDuration = 60,
            Deadline = DateTimeOffset.Now.AddSeconds(60)
        };
        s.Save();
        var loaded = new Store(testdir);
        Check(loaded.Data.Todos.Single().Text == "Finish design", "Task persists");
        Check(loaded.Data.Notes.Single().Title == "A note", "Note title and content persist");
        Check(loaded.Data.Shelf.Count == 1, "Shelf persists");
        Check(loaded.Data.Today.Water == 3 && loaded.Data.Today.Apps["Editor"] == 92, "Daily health and usage persist");
        Check(loaded.Data.Events.Single().Start.Hour == 11, "Calendar time persists");
        Check(loaded.Data.Timer.Running && loaded.Data.Timer.Deadline > DateTimeOffset.Now, "Timer deadline survives restart");
        Check(Store.DayProgress(new DateTime(2026, 1, 1, 6, 0, 0), 7, 22) == 0, "Day progress clamps before wake");
        Check(Store.DayProgress(new DateTime(2026, 1, 1, 23, 0, 0), 7, 22) == 1, "Day progress clamps after bedtime");
        Check(Math.Abs(Store.DayProgress(new DateTime(2026, 1, 1, 14, 30, 0), 7, 22) - .5) < .001, "Day progress midpoint");
        loaded.Data.Settings.Width = 99999;
        loaded.Data.Settings.WakeHour = 99;
        loaded.Data.Settings.BedHour = -1;
        loaded.Normalize();
        Check(loaded.Data.Settings.Width == 1500 && loaded.Data.Settings.BedHour == 23, "Settings bounds normalize");
        loaded.Save();
        File.WriteAllText(loaded.FilePath, "broken json");
        var recovered = new Store(testdir);
        Check(recovered.RecoveryWarning != null && File.Exists(recovered.FilePath + ".bak"), "Corrupt file preserved and backup recovered");
        Check(recovered.Data.Todos.Count == 1, "Backup contains saved tasks");
        File.WriteAllText(Path.Combine(dir, "self-test-results.txt"), string.Join(Environment.NewLine, results));
    }
}

