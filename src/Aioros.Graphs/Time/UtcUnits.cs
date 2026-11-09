namespace Aioros.Graphs.Time;

internal static class UtcUnits
{
    public const double Second = 1000;
    public const double Minute = 60 * Second;
    public const double Hour = 60 * Minute;
    public const double Day = 24 * Hour;
    public const double Week = 7 * Day;
    public const double Month = 30 * Day;
    public const double Year = 365 * Day;

    public static double Floor(UtcUnit unit, double t)
    {
        if (!UtcCalendar.TryTime(t, out var ms))
        {
            return double.NaN;
        }

        var time = UtcCalendar.TimeOf(ms);
        return unit switch
        {
            UtcUnit.Millisecond => t,
            UtcUnit.Second => ms - time % 1000,
            UtcUnit.Minute => ms - time % 60_000,
            UtcUnit.Hour => ms - time % 3_600_000,
            UtcUnit.Day or UtcUnit.UnixDay => (ms - time),
            UtcUnit.Month => MonthStart(ms),
            UtcUnit.Year => YearStart(ms),
            _ => (double)WeekdayFloor(UtcCalendar.DayOf(ms), Weekday(unit)) * UtcCalendar.DayLength,
        };
    }

    public static int Weekday(UtcUnit unit) => unit - UtcUnit.Week;

    public static long WeekdayFloor(long day, int weekday) => day - (UtcCalendar.WeekdayOf(day) + 7 - weekday) % 7;

    public static double Offset(UtcUnit unit, double t, double step) => unit switch
    {
        UtcUnit.Millisecond => UtcCalendar.Clip(t + step),
        UtcUnit.Second => UtcCalendar.Clip(t + step * Second),
        UtcUnit.Minute => UtcCalendar.Clip(t + step * Minute),
        UtcUnit.Hour => UtcCalendar.Clip(t + step * Hour),
        UtcUnit.Day or UtcUnit.UnixDay => UtcCalendar.Clip(t + step * Day),
        UtcUnit.Month => WithFields(t, 0, step),
        UtcUnit.Year => WithFields(t, step, 0),
        _ => UtcCalendar.Clip(t + step * Week),
    };

    public static double Count(UtcUnit unit, double t0, double t1) => unit switch
    {
        UtcUnit.Millisecond => t1 - t0,
        UtcUnit.Second => (t1 - t0) / Second,
        UtcUnit.Minute => (t1 - t0) / Minute,
        UtcUnit.Hour => (t1 - t0) / Hour,
        UtcUnit.Day or UtcUnit.UnixDay => (t1 - t0) / Day,
        UtcUnit.Month => UtcCalendar.Month(t1) - UtcCalendar.Month(t0) + (UtcCalendar.Year(t1) - UtcCalendar.Year(t0)) * 12,
        UtcUnit.Year => UtcCalendar.Year(t1) - UtcCalendar.Year(t0),
        _ => (t1 - t0) / Week,
    };

    public static double Field(UtcUnit unit, double t)
    {
        if (!UtcCalendar.TryTime(t, out var ms))
        {
            return double.NaN;
        }

        return unit switch
        {
            UtcUnit.Second => UtcCalendar.TimeOf(ms) / 1000 % 60,
            UtcUnit.Minute => UtcCalendar.TimeOf(ms) / 60_000 % 60,
            UtcUnit.Hour => UtcCalendar.TimeOf(ms) / 3_600_000,
            UtcUnit.Day => UtcCalendar.FieldsOf(ms).Day - 1,
            UtcUnit.UnixDay => UtcCalendar.DayOf(ms),
            UtcUnit.Month => UtcCalendar.FieldsOf(ms).Month,
            _ => Math.Floor(Count(unit, Floor(unit, 0), Floor(unit, t))),
        };
    }

    public static double WithFields(double t, double years, double months)
    {
        if (!UtcCalendar.TryTime(t, out var ms))
        {
            return double.NaN;
        }

        var f = UtcCalendar.FieldsOf(ms);
        if (Math.Abs(years) < 1e9 && Math.Abs(months) < 1e9)
        {
            return UtcCalendar.ComposeFields(f.Year + (long)years, f.Month + (long)months, f.Day, f.Time);
        }

        return UtcCalendar.Compose(f.Year + years, f.Month + months, f.Day, f.Hours, f.Minutes, f.Seconds, f.Milliseconds);
    }

    private static double MonthStart(long ms)
    {
        var f = UtcCalendar.FieldsOf(ms);
        return UtcCalendar.FromDays(UtcCalendar.DaysFromCivil(f.Year, f.Month + 1));
    }

    private static double YearStart(long ms)
    {
        var f = UtcCalendar.FieldsOf(ms);
        return UtcCalendar.FromDays(UtcCalendar.DaysFromCivil(f.Year, 1));
    }
}
