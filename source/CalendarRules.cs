namespace OrbitIsland;

using System;
using System.Globalization;
using System.Linq;
public static class CalendarRules
{
    public static DateTime Month(DateTime date) => new(date.Year, date.Month, 1);
    public static DateTime[] Grid(DateTime month)
    {
        var first = Month(month);
        var start = first.AddDays(-((int)first.DayOfWeek + 6) % 7);
        return Enumerable.Range(0, 42).Select(i => start.AddDays(i)).ToArray();
    }
    public static (DateTime Start, DateTime End) EventTime(string date, string time, int duration)
    {
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) || !TimeSpan.TryParseExact(time, new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var hour) || hour < TimeSpan.Zero || hour >= TimeSpan.FromDays(1) || duration < 1 || duration > 1440)
            throw new ArgumentException("Use a valid YYYY-MM-DD date, HH:mm time and duration from 1 to 1440 minutes.");
        var start = day + hour;
        return (start, start.AddMinutes(duration));
    }
}
