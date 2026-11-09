using Aioros.Graphs.Time;

namespace Aioros.Graphs.Formatting;

internal sealed class TimeParseProgram
{
    private const char Literal = '\0';
    private const char Fail = '!';
    private const char Guard = '?';
    private const int MaxDepth = 8;
    private const string Directives = "aAbBdefgGHIjLmMpqQsSuUVwWyYZ%";

    private readonly Op[] ops;
    private readonly Names periods;
    private readonly Names days;
    private readonly Names shortDays;
    private readonly Names months;
    private readonly Names shortMonths;

    private TimeParseProgram(Op[] ops, TimeLocale locale)
    {
        this.ops = ops;
        periods = new Names(locale.PeriodNames);
        days = new Names(locale.DayNames);
        shortDays = new Names(locale.ShortDayNames);
        months = new Names(locale.MonthNames);
        shortMonths = new Names(locale.ShortMonthNames);
    }

    [Flags]
    private enum Present : ushort
    {
        None = 0,
        Timestamp = 1,
        Seconds = 2,
        Zone = 4,
        Period = 8,
        Quarter = 16,
        IsoWeek = 32,
        MondayWeek = 64,
        SundayWeek = 128,
        Weekday = 256,
        MondayWeekday = 512,
    }

    public static TimeParseProgram Compile(string specifier, TimeLocale locale)
    {
        var ops = new List<Op>();
        Compile(specifier, locale, 0, ops);
        return new TimeParseProgram([.. ops], locale);
    }

    public bool TryParse(ReadOnlySpan<char> text, bool utc, TimeZoneInfo zone, out double time)
    {
        var d = new Fields { Year = 1900, Month = double.NaN, Day = 1 };
        var j = 0;
        foreach (var op in ops)
        {
            if (j >= text.Length)
            {
                time = double.NaN;
                return false;
            }

            j = op.Kind switch
            {
                Literal => text[j] == op.Char ? j + 1 : -1,
                Guard => j,
                Fail => -1,
                _ => Directive(op.Kind, ref d, text, j),
            };
            if (j < 0)
            {
                time = double.NaN;
                return false;
            }
        }

        if (j != text.Length)
        {
            time = double.NaN;
            return false;
        }

        return Resolve(ref d, utc, zone, out time);
    }

    private static void Compile(string specifier, TimeLocale locale, int depth, List<Op> ops)
    {
        var n = specifier.Length;
        var i = 0;
        while (i < n)
        {
            var c = specifier[i++];
            if (c != '%')
            {
                ops.Add(new Op(Literal, c));
                continue;
            }

            var directive = At(specifier, i++);
            if (directive is '-' or '_' or '0')
            {
                directive = At(specifier, i++);
            }

            AddDirective(directive, locale, depth, ops);
        }
    }

    private static char At(string specifier, int i) => i < specifier.Length ? specifier[i] : '\0';

    private static void AddDirective(char directive, TimeLocale locale, int depth, List<Op> ops)
    {
        var nested = directive switch
        {
            'c' => locale.DateTime,
            'x' => locale.Date,
            'X' => locale.Time,
            _ => null,
        };
        if (nested is not null && depth < MaxDepth)
        {
            ops.Add(new Op(Guard, '\0'));
            Compile(nested, locale, depth + 1, ops);
            return;
        }

        var known = nested is null && directive != '\0' && Directives.Contains(directive, StringComparison.Ordinal);
        ops.Add(new Op(known ? directive : Fail, '\0'));
    }

    private int Directive(char kind, ref Fields d, ReadOnlySpan<char> text, int j) => kind switch
    {
        'a' => Name(shortDays, ref d.Weekday, ref d.Present, Present.Weekday, text, j),
        'A' => Name(days, ref d.Weekday, ref d.Present, Present.Weekday, text, j),
        'b' => Name(shortMonths, ref d.Month, ref d.Present, Present.None, text, j),
        'B' => Name(months, ref d.Month, ref d.Present, Present.None, text, j),
        'p' => Name(periods, ref d.Period, ref d.Present, Present.Period, text, j),
        _ => Numeric(kind, ref d, text, j),
    };

    private static int Numeric(char kind, ref Fields d, ReadOnlySpan<char> text, int j)
    {
        if (kind == 'Z')
        {
            return ZoneOffset(ref d, text, j);
        }

        if (kind == '%')
        {
            return text[j] == '%' ? j + 1 : -1;
        }

        var width = kind switch
        {
            'u' or 'w' or 'q' => 1,
            'Y' or 'G' => 4,
            'j' or 'L' => 3,
            'f' => 6,
            'Q' or 's' => int.MaxValue,
            _ => 2,
        };
        var end = (int)Math.Min(text.Length, (long)j + width);
        var p = j;
        while (p < end && JsText.IsWhitespace(text[p]))
        {
            p++;
        }

        var start = p;
        while (p < end && JsText.IsDigit(text[p]))
        {
            p++;
        }

        if (p == start)
        {
            return -1;
        }

        Assign(kind, ref d, JsText.ParseDigits(text[start..p]));
        return p;
    }

    private static void Assign(char kind, ref Fields d, double v)
    {
        switch (kind)
        {
            case 'w': d.Weekday = v; d.Present |= Present.Weekday; break;
            case 'u': d.MondayWeekday = v; d.Present |= Present.MondayWeekday; break;
            case 'U': d.SundayWeek = v; d.Present |= Present.SundayWeek; break;
            case 'V': d.IsoWeek = v; d.Present |= Present.IsoWeek; break;
            case 'W': d.MondayWeek = v; d.Present |= Present.MondayWeek; break;
            case 'Y' or 'G': d.Year = v; break;
            case 'y' or 'g': d.Year = v + (v > 68 ? 1900 : 2000); break;
            case 'q': d.Quarter = v * 3 - 3; d.Present |= Present.Quarter; break;
            case 'm': d.Month = v - 1; break;
            case 'd' or 'e': d.Day = v; break;
            case 'j': d.Month = 0; d.Day = v; break;
            case 'H' or 'I': d.Hours = v; break;
            case 'M': d.Minutes = v; break;
            case 'S': d.Seconds = v; break;
            case 'L': d.Milliseconds = v; break;
            case 'f': d.Milliseconds = Math.Floor(v / 1000); break;
            case 'Q': d.Timestamp = v; d.Present |= Present.Timestamp; break;
            default: d.UnixSeconds = v; d.Present |= Present.Seconds; break;
        }
    }

    private static int Name(Names names, ref double field, ref Present present, Present flag, ReadOnlySpan<char> text, int j)
    {
        var rest = text[j..];
        foreach (var name in names.Values)
        {
            if (JsText.StartsWithIgnoreCase(rest, name))
            {
                field = names.Lookup(rest[..name.Length]);
                present |= flag;
                return j + name.Length;
            }
        }

        return -1;
    }

    private static int ZoneOffset(ref Fields d, ReadOnlySpan<char> text, int j)
    {
        var window = text[j..Math.Min(text.Length, j + 6)];
        if (window[0] == 'Z')
        {
            d.Zone = 0;
            d.Present |= Present.Zone;
            return j + 1;
        }

        for (var p = 0; p + 3 <= window.Length; p++)
        {
            if (window[p] is not ('+' or '-') || !JsText.IsDigit(window[p + 1]) || !JsText.IsDigit(window[p + 2]))
            {
                continue;
            }

            var rest = window[(p + 3)..];
            var (length, minutes) = rest.Length >= 3 && rest[0] == ':' && JsText.IsDigit(rest[1]) && JsText.IsDigit(rest[2])
                ? (6, (rest[1] - '0') * 10 + (rest[2] - '0'))
                : rest.Length >= 2 && JsText.IsDigit(rest[0]) && JsText.IsDigit(rest[1])
                    ? (5, (rest[0] - '0') * 10 + (rest[1] - '0'))
                    : (3, 0);
            double value = ((window[p + 1] - '0') * 10 + (window[p + 2] - '0')) * 100 + minutes;
            d.Zone = -(window[p] == '-' ? -value : value);
            d.Present |= Present.Zone;
            return j + length;
        }

        return -1;
    }

    private static bool Resolve(ref Fields d, bool utc, TimeZoneInfo zone, out double time)
    {
        if (d.Has(Present.Timestamp))
        {
            time = UtcCalendar.Clip(d.Timestamp);
            return true;
        }

        if (d.Has(Present.Seconds))
        {
            time = UtcCalendar.Clip(d.UnixSeconds * 1000 + d.Milliseconds);
            return true;
        }

        if (utc && !d.Has(Present.Zone))
        {
            d.Zone = 0;
            d.Present |= Present.Zone;
        }

        if (d.Has(Present.Period))
        {
            d.Hours = d.Hours % 12 + d.Period * 12;
        }

        if (double.IsNaN(d.Month))
        {
            d.Month = d.Has(Present.Quarter) ? d.Quarter : 0;
        }

        if (d.Has(Present.IsoWeek))
        {
            if (d.IsoWeek < 1 || d.IsoWeek > 53)
            {
                time = double.NaN;
                return false;
            }

            IsoWeekDate(ref d, zone);
        }
        else if (d.Has(Present.MondayWeek) || d.Has(Present.SundayWeek))
        {
            WeekDate(ref d, zone);
        }

        if (d.Has(Present.Zone))
        {
            d.Hours += Math.Truncate(d.Zone / 100);
            d.Minutes += d.Zone % 100;
            time = UtcDate(d);
            return true;
        }

        time = LocalDate(d, zone);
        return true;
    }

    private static void IsoWeekDate(ref Fields d, TimeZoneInfo zone)
    {
        if (!d.Has(Present.Weekday))
        {
            d.Weekday = 1;
        }

        double week;
        if (d.Has(Present.Zone))
        {
            week = UtcDate(Fields.Date(d.Year, 0, 1));
            var day = UtcCalendar.Weekday(week);
            week = day > 4 || day == 0 ? UtcInterval.Monday.Ceil(week) : UtcInterval.Monday.Floor(week);
            week = UtcInterval.Day.Offset(week, (d.IsoWeek - 1) * 7);
            d.Year = UtcCalendar.Year(week);
            d.Month = UtcCalendar.Month(week);
            d.Day = UtcCalendar.Day(week) + (d.Weekday + 6) % 7;
            return;
        }

        week = LocalDate(Fields.Date(d.Year, 0, 1), zone);
        var localDay = LocalCalendar.Weekday(week, zone);
        var monday = TimeInterval.Monday.WithZone(zone);
        week = localDay > 4 || localDay == 0 ? monday.Ceil(week) : monday.Floor(week);
        week = TimeInterval.Day.WithZone(zone).Offset(week, (d.IsoWeek - 1) * 7);
        d.Year = LocalCalendar.Year(week, zone);
        d.Month = LocalCalendar.Month(week, zone);
        d.Day = LocalCalendar.Day(week, zone) + (d.Weekday + 6) % 7;
    }

    private static void WeekDate(ref Fields d, TimeZoneInfo zone)
    {
        if (!d.Has(Present.Weekday))
        {
            d.Weekday = d.Has(Present.MondayWeekday) ? d.MondayWeekday % 7 : d.Has(Present.MondayWeek) ? 1 : 0;
        }

        var day = d.Has(Present.Zone)
            ? UtcCalendar.Weekday(UtcDate(Fields.Date(d.Year, 0, 1)))
            : LocalCalendar.Weekday(LocalDate(Fields.Date(d.Year, 0, 1), zone), zone);
        d.Month = 0;
        d.Day = d.Has(Present.MondayWeek)
            ? (d.Weekday + 6) % 7 + d.MondayWeek * 7 - (day + 5) % 7
            : d.Weekday + d.SundayWeek * 7 - (day + 6) % 7;
    }

    private static double UtcDate(in Fields d)
    {
        if (d.Year is >= 0 and < 100)
        {
            var t = UtcCalendar.Compose(-1, d.Month, d.Day, d.Hours, d.Minutes, d.Seconds, d.Milliseconds);
            if (double.IsNaN(t))
            {
                t = 0;
            }

            var f = UtcCalendar.FieldsOf((long)t);
            return UtcCalendar.Clip(UtcCalendar.MakeDay(d.Year, f.Month, f.Day) * UtcCalendar.MillisecondsPerDay + f.Time);
        }

        return UtcCalendar.Compose(d.Year, d.Month, d.Day, d.Hours, d.Minutes, d.Seconds, d.Milliseconds);
    }

    private static double LocalDate(in Fields d, TimeZoneInfo zone)
    {
        if (d.Year is >= 0 and < 100)
        {
            var t = LocalCalendar.Compose(zone, -1, d.Month, d.Day, d.Hours, d.Minutes, d.Seconds, d.Milliseconds);
            return LocalCalendar.SetFullYear(t, zone, d.Year);
        }

        return LocalCalendar.Compose(zone, d.Year, d.Month, d.Day, d.Hours, d.Minutes, d.Seconds, d.Milliseconds);
    }

    private readonly record struct Op(char Kind, char Char);

    private sealed class Names
    {
        private readonly string[] lowered;

        public Names(string[] values)
        {
            Values = values;
            lowered = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                lowered[i] = JsText.ToLower(values[i]);
            }
        }

        public string[] Values { get; }

        public double Lookup(ReadOnlySpan<char> matched)
        {
            for (var i = lowered.Length - 1; i >= 0; i--)
            {
                if (JsText.LowerEquals(matched, lowered[i]))
                {
                    return i;
                }
            }

            return double.NaN;
        }
    }

    private struct Fields
    {
        public double Year;
        public double Month;
        public double Day;
        public double Hours;
        public double Minutes;
        public double Seconds;
        public double Milliseconds;
        public double Timestamp;
        public double UnixSeconds;
        public double Zone;
        public double Period;
        public double Quarter;
        public double IsoWeek;
        public double MondayWeek;
        public double SundayWeek;
        public double Weekday;
        public double MondayWeekday;
        public Present Present;

        public static Fields Date(double year, double month, double day) => new() { Year = year, Month = month, Day = day };

        public readonly bool Has(Present flag) => (Present & flag) != 0;
    }
}
