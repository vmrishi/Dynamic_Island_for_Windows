namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32;
public partial class Island
{

    void AddShelf()
    {
        var f = new OpenFileDialog { Multiselect = true, Title = "Pin applications, shortcuts, or files", Filter = "All files|*.*" };
        if (f.ShowDialog() == true)
        {
            D.Shelf = D.Shelf.Concat(f.FileNames).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Save();
            RenderTab();
        }
    }

    string noteDraft = "";

    void SetTimerMode(string mode)
    {
        D.Timer = new TimerRecord { Mode = mode, Remaining = Duration(mode), SessionDuration = Duration(mode) };
        Save();
        RenderTab();
    }
    double Duration(string mode) => 60 * (mode == "Focus" ? S.FocusMinutes : mode == "Short break" ? S.ShortMinutes : S.LongMinutes);
    void ToggleTimer()
    {
        var t = D.Timer;
        if (t.Running)
        {
            if (t.Mode == "Stopwatch")
                t.StopwatchSeconds = TimerSeconds();
            else
                t.Remaining = TimerSeconds();
            t.Running = false;
        }
        else
        {
            if (t.Mode == "Stopwatch")
                t.StopwatchStart = DateTimeOffset.Now;
            else
            {
                if (t.Remaining <= 0)
                {
                    t.Remaining = Duration(t.Mode);
                    t.SessionDuration = t.Remaining;
                }
                t.Deadline = DateTimeOffset.Now.AddSeconds(t.Remaining);
            }
            t.Running = true;
        }
        Save();
        RenderTab();
    }

    void StartHealth(string mode, int duration)
    {
        healthMode = mode;
        healthLength = duration;
        healthEnd = DateTimeOffset.Now.AddSeconds(duration);
        RenderTab();
    }

    Border HealthCard(UIElement content)
    {
        var c = Card(content, 14);
        c.Margin = new Thickness(0, 0, 10, 10);
        return c;
    }
    bool usageWeek;

    void ExportScreenTime()
    {
        var f = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "Orbit-screen-time.csv" };
        if (f.ShowDialog() != true)
            return;
        var sb = new StringBuilder("Date,Application,Seconds\r\n");
        foreach (var d in D.Days)
            foreach (var a in d.Value.Apps)
                sb.AppendLine($"{d.Key},\"{a.Key.Replace("\"", "\"\"")}\",{a.Value.ToString("0.0", CultureInfo.InvariantCulture)}");
        File.WriteAllText(f.FileName, sb.ToString());
    }


    void AddEvent() => EditEvent();
    void EditEvent(CalendarItem? existing = null)
    {
        var title = Input(existing?.Title ?? "", "Event title");
        var start = existing?.Start ?? selectedDate.Date.AddHours(9);
        var date = Input(start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "YYYY-MM-DD");
        var time = Input(start.ToString("HH:mm", CultureInfo.InvariantCulture), "HH:mm");
        var minutes = Input(existing?.End is { } end ? Math.Max(1, (int)(end - start).TotalMinutes).ToString() : "60", "Duration in minutes");
        var form = Col(T("Title", 12, Muted), title, T("Date · YYYY-MM-DD", 12, Muted), date, T("Time · 24 hour", 12, Muted), time, T("Duration · minutes", 12, Muted), minutes);
        ShowOverlay(existing == null ? "New event" : "Edit event", form, () => { if (string.IsNullOrWhiteSpace(title.Text)) throw new Exception("Enter an event title."); if (!int.TryParse(minutes.Text, out var duration)) throw new Exception("Enter a whole-number duration."); var times = CalendarRules.EventTime(date.Text, time.Text, duration); var item = existing ?? new CalendarItem(); item.Title = title.Text.Trim(); item.Start = times.Start; item.End = times.End; item.Alerted = false; if (existing == null) D.Events.Add(item); selectedDate = item.Start.Date; displayedMonth = CalendarRules.Month(selectedDate); Save(); }, "Save event");
    }
    void ImportIcs()
    {
        var f = new OpenFileDialog { Filter = "iCalendar|*.ics" };
        if (f.ShowDialog() != true)
            return;
        int count = 0, skipped = 0;
        var text = Regex.Replace(File.ReadAllText(f.FileName), "\\r?\\n[ \\t]", "");
        foreach (Match m in Regex.Matches(text, "BEGIN:VEVENT(.*?)END:VEVENT", RegexOptions.Singleline))
        {
            var block = m.Groups[1].Value;
            if (Regex.IsMatch(block, @"(?m)^RRULE:"))
            {
                skipped++;
                continue;
            }
            var title = Regex.Match(block, @"(?m)^SUMMARY:(.*)").Groups[1].Value.Trim().Replace(@"\n", " ").Replace(@"\,", ",");
            var dt = Regex.Match(block, @"(?m)^DTSTART([^:]*):([^\r\n]+)");
            string raw = dt.Groups[2].Value.Trim();
            if (dt.Groups[1].Value.Contains("TZID="))
            {
                skipped++;
                continue;
            }
            if (!DateTime.TryParseExact(raw, new[] { "yyyyMMdd'T'HHmmss'Z'", "yyyyMMdd'T'HHmmss", "yyyyMMdd" }, CultureInfo.InvariantCulture, raw.EndsWith('Z') ? DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal : DateTimeStyles.None, out var start))
            {
                skipped++;
                continue;
            }
            if (raw.EndsWith('Z'))
                start = start.ToLocalTime();
            if (title != "" && !D.Events.Any(x => x.Title == title && x.Start == start))
            {
                D.Events.Add(new()
                {
                    Title = title,
                    Start = start
                });
                count++;
            }
        }
        Save();
        RenderTab();
        Notify("Calendar imported", $"{count} events added. {skipped} recurring, timezone-tagged, or unsupported events skipped.", false);
    }
    void ExportIcs()
    {
        var f = new SaveFileDialog { Filter = "iCalendar|*.ics", FileName = "Orbit-calendar.ics" };
        if (f.ShowDialog() != true)
            return;
        var sb = new StringBuilder("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Orbit Island//Local Calendar//EN\r\n");
        foreach (var ev in D.Events)
        {
            string summary = ev.Title.Replace("\\", "\\\\").Replace("\n", @"\n").Replace(",", @"\,").Replace(";", @"\;");
            sb.Append($"BEGIN:VEVENT\r\nUID:{ev.Id}@orbit.local\r\nDTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}\r\nDTSTART:{ev.Start.ToUniversalTime():yyyyMMdd'T'HHmmss'Z'}\r\nSUMMARY:{summary}\r\n");
            if (ev.End is { } finish)
                sb.Append($"DTEND:{finish.ToUniversalTime():yyyyMMdd'T'HHmmss'Z'}\r\n");
            sb.Append("END:VEVENT\r\n");
        }
        sb.Append("END:VCALENDAR\r\n");
        File.WriteAllText(f.FileName, sb.ToString());
    }

    void ShowDialog(string title, StackPanel content, Action save) => ShowOverlay(title, content, save);
}
