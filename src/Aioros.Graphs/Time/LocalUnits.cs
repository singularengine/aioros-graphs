namespace Aioros.Graphs.Time;

internal static class LocalUnits
{
    public static double Floor(UtcUnit unit, double t, TimeZoneInfo zone)
    {
        if (double.IsNaN(t))
        {
            return double.NaN;
        }

        switch (unit)
        {
            case UtcUnit.Millisecond:
                return t;
            case UtcUnit.Second:
                return UtcCalendar.Clip(t - Modulo(LocalCalendar.LocalTime(t, zone), 1000));
            case UtcUnit.Minute:
                return UtcCalendar.Clip(t - Modulo(LocalCalendar.LocalTime(t, zone), 60_000));
            case UtcUnit.Hour:
                return UtcCalendar.Clip(t - Modulo(LocalCalendar.LocalTime(t, zone), 3_600_000));
            case UtcUnit.Day or UtcUnit.UnixDay:
                return Midnight(t, zone);
            case UtcUnit.Month:
                return Midnight(LocalCalendar.SetDate(t, zone, 1), zone);
            case UtcUnit.Year:
                return Midnight(LocalCalendar.SetMonth(t, zone, 0, 1), zone);
            default:
                var f = LocalCalendar.WallFields(LocalCalendar.LocalTime(t, zone));
                var back = (f.Weekday + 7 - UtcUnits.Weekday(unit)) % 7;
                return Midnight(LocalCalendar.SetDate(t, zone, f.Day - back), zone);
        }
    }

    public static double YearStepFloor(double t, double step, TimeZoneInfo zone)
    {
        var year = LocalCalendar.TryFields(t, zone, out var f) ? f.Year : double.NaN;
        var start = LocalCalendar.SetFullYear(t, zone, Math.Floor(year / step) * step);
        return Midnight(LocalCalendar.SetMonth(start, zone, 0, 1), zone);
    }

    public static double Offset(UtcUnit unit, double t, double step, TimeZoneInfo zone) => unit switch
    {
        UtcUnit.Millisecond => UtcCalendar.Clip(t + step),
        UtcUnit.Second => UtcCalendar.Clip(t + step * UtcUnits.Second),
        UtcUnit.Minute => UtcCalendar.Clip(t + step * UtcUnits.Minute),
        UtcUnit.Hour => UtcCalendar.Clip(t + step * UtcUnits.Hour),
        UtcUnit.Day or UtcUnit.UnixDay => LocalCalendar.SetDate(t, zone, LocalCalendar.Day(t, zone) + step),
        UtcUnit.Month => LocalCalendar.SetMonth(t, zone, LocalCalendar.Month(t, zone) + step),
        UtcUnit.Year => LocalCalendar.SetFullYear(t, zone, LocalCalendar.Year(t, zone) + step),
        _ => LocalCalendar.SetDate(t, zone, LocalCalendar.Day(t, zone) + step * 7),
    };

    public static double Count(UtcUnit unit, double t0, double t1, TimeZoneInfo zone) => unit switch
    {
        UtcUnit.Millisecond => t1 - t0,
        UtcUnit.Second => (t1 - t0) / UtcUnits.Second,
        UtcUnit.Minute => (t1 - t0) / UtcUnits.Minute,
        UtcUnit.Hour => (t1 - t0) / UtcUnits.Hour,
        UtcUnit.Day or UtcUnit.UnixDay => Shifted(t0, t1, zone) / UtcUnits.Day,
        UtcUnit.Month => LocalCalendar.Month(t1, zone) - LocalCalendar.Month(t0, zone) + (LocalCalendar.Year(t1, zone) - LocalCalendar.Year(t0, zone)) * 12,
        UtcUnit.Year => LocalCalendar.Year(t1, zone) - LocalCalendar.Year(t0, zone),
        _ => Shifted(t0, t1, zone) / UtcUnits.Week,
    };

    public static double Field(UtcUnit unit, double t, TimeZoneInfo zone) => unit switch
    {
        UtcUnit.Second => UtcCalendar.Seconds(t),
        UtcUnit.Minute => LocalCalendar.Minutes(t, zone),
        UtcUnit.Hour => LocalCalendar.Hours(t, zone),
        UtcUnit.Day or UtcUnit.UnixDay => LocalCalendar.Day(t, zone) - 1,
        UtcUnit.Month => LocalCalendar.Month(t, zone),
        UtcUnit.Year => LocalCalendar.Year(t, zone),
        _ => Math.Floor(Count(unit, Floor(unit, 0, zone), Floor(unit, t, zone), zone)),
    };

    private static double Shifted(double t0, double t1, TimeZoneInfo zone) =>
        t1 - t0 - (LocalCalendar.TimezoneOffset(t1, zone) - LocalCalendar.TimezoneOffset(t0, zone)) * UtcUnits.Minute;

    private static double Midnight(double t, TimeZoneInfo zone) => LocalCalendar.SetHours(t, zone, 0, 0, 0, 0);

    private static double Modulo(double value, double divisor)
    {
        var r = value % divisor;
        return r < 0 ? r + divisor : r + 0.0;
    }
}
