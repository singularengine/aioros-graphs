using System.Globalization;
using System.Text;
using Aioros.Graphs.Time;

namespace Aioros.Graphs.Formatting;

internal sealed class TimeFormatProgram
{
    private const string Directives = "aAbBdefgGHIjLmMpqQsSuUVwWyYZ%";
    private const int NumberLength = 20;

    private readonly Segment[] segments;
    private readonly TimeLocale locale;

    private TimeFormatProgram(Segment[] segments, TimeLocale locale, int maxLength)
    {
        this.segments = segments;
        this.locale = locale;
        MaxLength = maxLength;
    }

    public int MaxLength { get; }

    public static TimeFormatProgram Compile(string specifier, TimeLocale locale)
    {
        ArgumentNullException.ThrowIfNull(specifier);
        ArgumentNullException.ThrowIfNull(locale);
        var segments = new List<Segment>();
        var literal = new StringBuilder();
        Append(specifier, locale, segments, literal, 0);
        Flush(literal, segments);
        var maxLength = 0;
        foreach (var segment in segments)
        {
            maxLength += segment.Literal?.Length ?? (segment.Code is 'a' or 'A' or 'b' or 'B' or 'p' ? locale.LongestName : NumberLength);
        }

        return new TimeFormatProgram([.. segments], locale, maxLength);
    }

    public string Format(double t, long wall, double zoneOffset)
    {
        var buffer = MaxLength <= 256 ? stackalloc char[256] : new char[MaxLength];
        return new string(buffer[..Write(buffer, t, wall, zoneOffset)]);
    }

    public bool TryFormat(double t, long wall, double zoneOffset, Span<char> destination, out int written)
    {
        if (destination.Length >= MaxLength)
        {
            written = Write(destination, t, wall, zoneOffset);
            return true;
        }

        var buffer = MaxLength <= 256 ? stackalloc char[256] : new char[MaxLength];
        var length = Write(buffer, t, wall, zoneOffset);
        written = length <= destination.Length ? length : 0;
        return length <= destination.Length && buffer[..length].TryCopyTo(destination);
    }

    private int Write(Span<char> destination, double t, long wall, double zoneOffset)
    {
        var p = 0;
        var valid = !double.IsNaN(t);
        var fields = valid ? UtcCalendar.FieldsOf(wall) : default;
        foreach (var segment in segments)
        {
            if (segment.Literal is { } literal)
            {
                literal.CopyTo(destination[p..]);
                p += literal.Length;
            }
            else if (valid)
            {
                WriteField(destination, ref p, segment, t, fields, zoneOffset);
            }
            else
            {
                WriteInvalid(destination, ref p, segment, zoneOffset);
            }
        }

        return p;
    }

    private void WriteField(Span<char> d, ref int p, Segment segment, double t, CalendarFields f, double zoneOffset)
    {
        var fill = segment.Fill;
        switch (segment.Code)
        {
            case 'a': Text(d, ref p, locale.ShortDayNames[f.Weekday]); break;
            case 'A': Text(d, ref p, locale.DayNames[f.Weekday]); break;
            case 'b': Text(d, ref p, locale.ShortMonthNames[f.Month]); break;
            case 'B': Text(d, ref p, locale.MonthNames[f.Month]); break;
            case 'd' or 'e': Pad(d, ref p, f.Day, fill, 2); break;
            case 'f': Pad(d, ref p, f.Milliseconds, fill, 3); Text(d, ref p, "000"); break;
            case 'H': Pad(d, ref p, f.Hours, fill, 2); break;
            case 'I': Pad(d, ref p, f.Hours % 12 == 0 ? 12 : f.Hours % 12, fill, 2); break;
            case 'j': PadOrNaN(d, ref p, DayOfYear(f), fill, 3); break;
            case 'L': Pad(d, ref p, f.Milliseconds, fill, 3); break;
            case 'm': Pad(d, ref p, f.Month + 1, fill, 2); break;
            case 'M': Pad(d, ref p, f.Minutes, fill, 2); break;
            case 'S': Pad(d, ref p, f.Seconds, fill, 2); break;
            case 'y': Pad(d, ref p, f.Year % 100, fill, 2); break;
            case 'Y': Pad(d, ref p, f.Year % 10000, fill, 4); break;
            default: WriteOther(d, ref p, segment, t, f, zoneOffset); break;
        }
    }

    private void WriteOther(Span<char> d, ref int p, Segment segment, double t, CalendarFields f, double zoneOffset)
    {
        var fill = segment.Fill;
        switch (segment.Code)
        {
            case 'p': Text(d, ref p, locale.PeriodNames[f.Hours >= 12 ? 1 : 0]); break;
            case 'q': Pad(d, ref p, 1 + f.Month / 3, '\0', 0); break;
            case 'Q': Pad(d, ref p, (long)t, '\0', 0); break;
            case 's': Pad(d, ref p, UtcCalendar.FloorDiv((long)t, 1000), '\0', 0); break;
            case 'u': Pad(d, ref p, f.Weekday == 0 ? 7 : f.Weekday, '\0', 0); break;
            case 'w': Pad(d, ref p, f.Weekday, '\0', 0); break;
            case 'U': PadOrNaN(d, ref p, WeekOfYear(f, 0), fill, 2); break;
            case 'W': PadOrNaN(d, ref p, WeekOfYear(f, 1), fill, 2); break;
            case 'V': PadOrNaN(d, ref p, IsoWeek(f), fill, 2); break;
            case 'g': PadOrNaN(d, ref p, IsoYear(f) % 100, fill, 2); break;
            case 'G': PadOrNaN(d, ref p, IsoYear(f) % 10000, fill, 4); break;
            case 'Z': Zone(d, ref p, zoneOffset); break;
            default: d[p++] = '%'; break;
        }
    }

    private void WriteInvalid(Span<char> d, ref int p, Segment segment, double zoneOffset)
    {
        switch (segment.Code)
        {
            case 'a' or 'A' or 'b' or 'B': break;
            case 'I': Pad(d, ref p, 12, segment.Fill, 2); break;
            case 'f': PadOrNaN(d, ref p, double.NaN, segment.Fill, 3); Text(d, ref p, "000"); break;
            case 'p': Text(d, ref p, locale.PeriodNames[0]); break;
            case 'q': d[p++] = '1'; break;
            case 'Q' or 's' or 'u' or 'w': Text(d, ref p, "NaN"); break;
            case 'Y' or 'G': PadOrNaN(d, ref p, double.NaN, segment.Fill, 4); break;
            case 'j': PadOrNaN(d, ref p, double.NaN, segment.Fill, 3); break;
            case 'Z': Zone(d, ref p, zoneOffset); break;
            case '%': d[p++] = '%'; break;
            default: PadOrNaN(d, ref p, double.NaN, segment.Fill, 2); break;
        }
    }

    private static double DayOfYear(CalendarFields f)
    {
        var start = UtcCalendar.DaysFromCivil(f.Year, 1);
        return double.IsNaN(UtcCalendar.FromDays(start)) ? double.NaN : 1 + f.DayNumber - start;
    }

    private static double WeekOfYear(CalendarFields f, int weekday)
    {
        var yearStart = UtcCalendar.DaysFromCivil(f.Year, 1);
        if (double.IsNaN(UtcCalendar.FromDays(yearStart)))
        {
            return double.NaN;
        }

        var first = WeekdayFloor(yearStart - 1, weekday);
        var current = WeekdayFloor(f.DayNumber, weekday);
        return double.IsNaN(UtcCalendar.FromDays(first)) ? double.NaN : (current - first) / 7;
    }

    private static long WeekdayFloor(long day, int weekday) => day - (UtcCalendar.WeekdayOf(day) + 7 - weekday) % 7;

    private static long IsoThursday(CalendarFields f)
    {
        var day = f.Weekday;
        return day is >= 4 or 0 ? WeekdayFloor(f.DayNumber, 4) : WeekdayFloor(f.DayNumber, 4) + 7;
    }

    private static double IsoWeek(CalendarFields f)
    {
        var thursday = IsoThursday(f);
        if (double.IsNaN(UtcCalendar.FromDays(thursday)))
        {
            return double.NaN;
        }

        var year = UtcCalendar.Civil(thursday).Year;
        var yearStart = UtcCalendar.DaysFromCivil(year, 1);
        if (double.IsNaN(UtcCalendar.FromDays(yearStart)))
        {
            return double.NaN;
        }

        var first = WeekdayFloor(yearStart, 4);
        if (double.IsNaN(UtcCalendar.FromDays(first)))
        {
            return double.NaN;
        }

        return (thursday - first) / 7 + (UtcCalendar.WeekdayOf(yearStart) == 4 ? 1 : 0);
    }

    private static double IsoYear(CalendarFields f)
    {
        var thursday = IsoThursday(f);
        return double.IsNaN(UtcCalendar.FromDays(thursday)) ? double.NaN : UtcCalendar.Civil(thursday).Year;
    }

    private static void Zone(Span<char> d, ref int p, double offset)
    {
        var z = offset;
        if (z > 0)
        {
            d[p++] = '-';
        }
        else
        {
            z = -z;
            d[p++] = '+';
        }

        var hours = Math.Truncate(z / 60);
        PadOrNaN(d, ref p, double.IsNaN(hours) || hours == 0 ? 0 : hours, '0', 2);
        PadOrNaN(d, ref p, z % 60, '0', 2);
    }

    private static void Text(Span<char> d, ref int p, string text)
    {
        text.CopyTo(d[p..]);
        p += text.Length;
    }

    private static void PadOrNaN(Span<char> d, ref int p, double value, char fill, int width)
    {
        if (double.IsNaN(value) || value != Math.Floor(value) || Math.Abs(value) >= 1e15)
        {
            var text = JsNumber.ToShortestString(value);
            if (value < 0)
            {
                d[p++] = '-';
                text = JsNumber.ToShortestString(-value);
            }

            for (var i = text.Length; i < width && fill != '\0'; i++)
            {
                d[p++] = fill;
            }

            Text(d, ref p, text);
            return;
        }

        Pad(d, ref p, (long)value, fill, width);
    }

    private static void Pad(Span<char> d, ref int p, long value, char fill, int width)
    {
        if (value < 0)
        {
            d[p++] = '-';
            value = -value;
        }

        var digits = value < 10 ? 1 : value < 100 ? 2 : value < 1000 ? 3 : value < 10000 ? 4 : Digits(value);
        for (var i = digits; i < width && fill != '\0'; i++)
        {
            d[p++] = fill;
        }

        ((ulong)value).TryFormat(d[p..], out var written, default, CultureInfo.InvariantCulture);
        p += written;
    }

    private static int Digits(long value)
    {
        var digits = 1;
        while (value >= 10)
        {
            value /= 10;
            digits++;
        }

        return digits;
    }

    private static void Append(string specifier, TimeLocale locale, List<Segment> segments, StringBuilder literal, int depth)
    {
        var i = 0;
        while (i < specifier.Length)
        {
            if (specifier[i] != '%')
            {
                literal.Append(specifier[i++]);
                continue;
            }

            var (code, fill, end) = Directive(specifier, i + 1);
            i = end;
            if (code is not { } c)
            {
                continue;
            }

            if (IsKnown(c))
            {
                Flush(literal, segments);
                segments.Add(new Segment(c, fill, null));
            }
            else if (depth < 4 && Expansion(c, locale) is { } nested)
            {
                Append(nested, locale, segments, literal, depth + 1);
            }
            else
            {
                literal.Append(c);
            }
        }
    }

    private static (char? Code, char Fill, int End) Directive(string specifier, int i)
    {
        if (i >= specifier.Length)
        {
            return (null, '0', i);
        }

        var c = specifier[i];
        if (c is not ('-' or '_' or '0'))
        {
            return (c, c == 'e' ? ' ' : '0', i + 1);
        }

        var fill = c == '-' ? '\0' : c == '_' ? ' ' : '0';
        return i + 1 < specifier.Length ? (specifier[i + 1], fill, i + 2) : (null, fill, i + 1);
    }

    private static string? Expansion(char directive, TimeLocale locale) => directive switch
    {
        'c' => locale.DateTime,
        'x' => locale.Date,
        'X' => locale.Time,
        _ => null,
    };

    private static bool IsKnown(char directive) => Directives.Contains(directive, StringComparison.Ordinal);

    private static void Flush(StringBuilder literal, List<Segment> segments)
    {
        if (literal.Length > 0)
        {
            segments.Add(new Segment('\0', '\0', literal.ToString()));
            literal.Clear();
        }
    }

    private readonly record struct Segment(char Code, char Fill, string? Literal);
}
