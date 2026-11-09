using System.Collections.Immutable;

namespace Aioros.Graphs.Formatting;

/// <summary>
/// A d3-time-format locale definition (<c>timeFormatLocale</c>): the formats of <c>%c</c>, <c>%x</c> and <c>%X</c>
/// and the names of periods, weekdays and months.
/// </summary>
public sealed class TimeLocale
{
    private const int CachedPrograms = 256;

    private static ImmutableDictionary<string, TimeLocale> loaded = ImmutableDictionary.Create<string, TimeLocale>(StringComparer.Ordinal);

    private ImmutableDictionary<string, TimeFormatProgram> programs = ImmutableDictionary.Create<string, TimeFormatProgram>(StringComparer.Ordinal);

    /// <summary>Creates a locale from d3's definition.</summary>
    /// <param name="dateTime">The format of <c>%c</c>.</param>
    /// <param name="date">The format of <c>%x</c>.</param>
    /// <param name="time">The format of <c>%X</c>.</param>
    /// <param name="periods">The AM and PM names.</param>
    /// <param name="days">The weekday names from Sunday.</param>
    /// <param name="shortDays">The abbreviated weekday names from Sunday.</param>
    /// <param name="months">The month names from January.</param>
    /// <param name="shortMonths">The abbreviated month names from January.</param>
    /// <exception cref="ArgumentException">A list does not have 2, 7 or 12 names.</exception>
    public TimeLocale(string dateTime, string date, string time, string[] periods, string[] days, string[] shortDays, string[] months, string[] shortMonths)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        ArgumentNullException.ThrowIfNull(date);
        ArgumentNullException.ThrowIfNull(time);
        DateTime = dateTime;
        Date = date;
        Time = time;
        PeriodNames = Checked(periods, 2, nameof(periods));
        DayNames = Checked(days, 7, nameof(days));
        ShortDayNames = Checked(shortDays, 7, nameof(shortDays));
        MonthNames = Checked(months, 12, nameof(months));
        ShortMonthNames = Checked(shortMonths, 12, nameof(shortMonths));
        LongestName = Longest(PeriodNames, DayNames, ShortDayNames, MonthNames, ShortMonthNames);
        Labels = [Program(".%L"), Program(":%S"), Program("%I:%M"), Program("%I %p"), Program("%a %d"), Program("%b %d"), Program("%B"), Program("%Y")];
    }

    /// <summary>Gets d3's default locale, en-US.</summary>
    public static TimeLocale EnUs { get; } = new(
        "%x, %X",
        "%-m/%-d/%Y",
        "%-I:%M:%S %p",
        ["AM", "PM"],
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"],
        ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"],
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]);

    /// <summary>Gets the names of the locales d3 ships, such as <c>es-MX</c>.</summary>
    public static IReadOnlyList<string> Names => TimeLocales.Names;

    /// <summary>Gets the format of <c>%c</c>.</summary>
    public string DateTime { get; }

    /// <summary>Gets the format of <c>%x</c>.</summary>
    public string Date { get; }

    /// <summary>Gets the format of <c>%X</c>.</summary>
    public string Time { get; }

    /// <summary>Gets the AM and PM names.</summary>
    public IReadOnlyList<string> Periods => PeriodNames;

    /// <summary>Gets the weekday names from Sunday.</summary>
    public IReadOnlyList<string> Days => DayNames;

    /// <summary>Gets the abbreviated weekday names from Sunday.</summary>
    public IReadOnlyList<string> ShortDays => ShortDayNames;

    /// <summary>Gets the month names from January.</summary>
    public IReadOnlyList<string> Months => MonthNames;

    /// <summary>Gets the abbreviated month names from January.</summary>
    public IReadOnlyList<string> ShortMonths => ShortMonthNames;

    internal int LongestName { get; }

    internal TimeFormatProgram[] Labels { get; }

    internal string[] PeriodNames { get; }

    internal string[] DayNames { get; }

    internal string[] ShortDayNames { get; }

    internal string[] MonthNames { get; }

    internal string[] ShortMonthNames { get; }

    /// <summary>Returns the locale d3 ships under <paramref name="name"/>.</summary>
    /// <param name="name">The name, such as <c>fr-FR</c>.</param>
    /// <returns>The locale.</returns>
    /// <exception cref="ArgumentException">d3 ships no locale of that name.</exception>
    public static TimeLocale Get(string name) =>
        TryGet(name, out var locale) ? locale : throw new ArgumentException($"Unknown locale: {name}", nameof(name));

    /// <summary>Looks up the locale d3 ships under <paramref name="name"/>.</summary>
    /// <param name="name">The name, such as <c>fr-FR</c>.</param>
    /// <param name="locale">The locale, when found.</param>
    /// <returns>Whether d3 ships it.</returns>
    public static bool TryGet(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TimeLocale? locale)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (loaded.TryGetValue(name, out locale))
        {
            return true;
        }

        locale = TimeLocales.Create(name);
        if (locale is null)
        {
            return false;
        }

        locale = ImmutableInterlocked.GetOrAdd(ref loaded, name, locale);
        return true;
    }

    internal TimeFormatProgram Program(string specifier)
    {
        ArgumentNullException.ThrowIfNull(specifier);
        if (programs.TryGetValue(specifier, out var program))
        {
            return program;
        }

        program = TimeFormatProgram.Compile(specifier, this);
        if (programs.Count < CachedPrograms)
        {
            ImmutableInterlocked.TryAdd(ref programs, specifier, program);
        }

        return program;
    }

    private static string[] Checked(string[] names, int count, string parameter)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length != count)
        {
            throw new ArgumentException($"Expected {count} names.", parameter);
        }

        return [.. names];
    }

    private static int Longest(params string[][] lists)
    {
        var longest = 0;
        foreach (var list in lists)
        {
            foreach (var name in list)
            {
                longest = Math.Max(longest, name.Length);
            }
        }

        return longest;
    }
}
