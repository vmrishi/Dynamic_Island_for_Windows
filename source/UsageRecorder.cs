namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
public sealed class UsageRecorder
{
    DateTimeOffset? previousTime; string? previousApp; string? lastTrackedApp;
    public string? CurrentApp
    {
        get; private set;
    }
    public void Sample(AppData data, DateTimeOffset now, string? app, bool enabled)
    {
        if (!enabled)
        {
            previousTime = null;
            previousApp = null;
            lastTrackedApp = null;
            CurrentApp = null;
            return;
        }
        if (previousTime is { } checkpoint && now <= checkpoint)
            return;
        if (previousTime is { } previous)
        {
            double elapsed = (now - previous).TotalSeconds;
            if (elapsed > 0 && elapsed <= 10 && previous.Offset == now.Offset && previousApp != null)
                RecordInterval(data, previousApp, previous.DateTime, elapsed);
        }
        if (app != null && lastTrackedApp != null && !StringComparer.OrdinalIgnoreCase.Equals(app, lastTrackedApp))
            data.Day(now.Date).AppSwitches++;
        if (app != null)
            lastTrackedApp = app;
        previousTime = now;
        previousApp = app;
        CurrentApp = app;
    }
    public static void RecordInterval(AppData data, string app, DateTime start, double seconds)
    {
        if (string.IsNullOrWhiteSpace(app) || !double.IsFinite(seconds) || seconds <= 0)
            return;
        var cursor = start;
        double remaining = seconds;
        while (remaining > 0.000001)
        {
            var boundary = cursor.Date.AddHours(cursor.Hour + 1);
            double part = Math.Min(remaining, (boundary - cursor).TotalSeconds);
            if (part <= 0)
                break;
            var day = data.Day(cursor.Date);
            day.Apps.TryGetValue(app, out var old);
            day.Apps[app] = Math.Max(0, old) + part;
            day.Hourly.TryGetValue(cursor.Hour, out var hour);
            day.Hourly[cursor.Hour] = Math.Max(0, hour) + part;
            cursor = cursor.AddSeconds(part);
            remaining -= part;
        }
    }
    public static Dictionary<string, double> Aggregate(AppData data, DateTime today, int days) => data.Days.Where(x => DateTime.TryParseExact(x.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && d.Date >= today.Date.AddDays(1 - days) && d.Date <= today.Date).SelectMany(x => x.Value.Apps).Where(x => double.IsFinite(x.Value) && x.Value > 0).GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Sum(v => v.Value), StringComparer.OrdinalIgnoreCase);
    public static Dictionary<int, double> Hours(AppData data, DateTime today, int days) => data.Days.Where(x => DateTime.TryParseExact(x.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && d.Date >= today.Date.AddDays(1 - days) && d.Date <= today.Date).SelectMany(x => x.Value.Hourly).Where(x => x.Key >= 0 && x.Key < 24 && double.IsFinite(x.Value) && x.Value > 0).GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Sum(v => v.Value));
    public static int Switches(AppData data, DateTime today, int days) => data.Days.Where(x => DateTime.TryParseExact(x.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && d.Date >= today.Date.AddDays(1 - days) && d.Date <= today.Date).Sum(x => Math.Max(0, x.Value.AppSwitches));
    public static bool MatchesPlayingSource(string process, string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;
        if (source.Contains(process, StringComparison.OrdinalIgnoreCase))
            return true;
        string s = source.ToLowerInvariant(), p = process.ToLowerInvariant();
        return ((s.Contains("zunemusic") || s.Contains("zunevideo")) && (p is "microsoft.media.player" or "music.ui" or "video.ui")) || (s.Contains("windowsmediaplayer") && p == "wmplayer") || (s.Contains("windows.photos") && (p is "microsoft.photos" or "photosapp"));
    }
    public static string Duration(double seconds) => seconds >= 3600 ? $"{(int)(seconds / 3600)}h {(int)(seconds % 3600 / 60)}m" : seconds >= 60 ? $"{(int)(seconds / 60)}m" : $"{(int)Math.Max(0, seconds)}s";
}
