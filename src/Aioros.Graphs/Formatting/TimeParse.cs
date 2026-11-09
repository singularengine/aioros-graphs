namespace Aioros.Graphs.Formatting;

/// <summary>
/// A local date parser of d3-time-format (<c>timeParse</c>): the directives of <see cref="UtcTimeParse"/>, read as a
/// wall time in a time zone (<see cref="TimeZoneInfo.Local"/> unless another is given) and resolved as ECMAScript's
/// <c>Date</c> resolves local fields (a time in a daylight-saving gap takes the offset from before it, a repeated time
/// its first occurrence); a time with <c>%Z</c> takes that offset instead.
/// </summary>
public sealed class TimeParse
{
    private readonly TimeParseProgram program;
    private readonly TimeZoneInfo? zone;

    private TimeParse(TimeParseProgram program, string specifier, TimeZoneInfo? zone)
    {
        this.program = program;
        this.zone = zone;
        Specifier = specifier;
    }

    /// <summary>Gets the specifier, as d3's <c>parse.toString()</c>.</summary>
    public string Specifier { get; }

    /// <summary>Gets the time zone the parser reads wall times in.</summary>
    public TimeZoneInfo Zone => zone ?? TimeZoneInfo.Local;

    /// <summary>Compiles <paramref name="specifier"/>, as d3's <c>timeFormatLocale(locale).parse(specifier)</c>.</summary>
    /// <param name="specifier">The format, such as <c>%Y-%m-%d %H:%M</c>.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <param name="locale">The locale; d3's default en-US when null.</param>
    /// <returns>The parser.</returns>
    public static TimeParse Create(string specifier, TimeZoneInfo? zone = null, TimeLocale? locale = null)
    {
        ArgumentNullException.ThrowIfNull(specifier);
        return new(TimeParseProgram.Compile(specifier, locale ?? TimeLocale.EnUs), specifier, zone);
    }

    /// <summary>Parses <paramref name="text"/>.</summary>
    /// <param name="text">The text.</param>
    /// <param name="milliseconds">The time value, NaN where d3 returns an invalid date.</param>
    /// <returns>False where d3's parser returns null.</returns>
    public bool TryParse(ReadOnlySpan<char> text, out double milliseconds) => program.TryParse(text, false, Zone, out milliseconds);

    /// <summary>Parses <paramref name="text"/>, as d3's parser called on it.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The time value (NaN for an invalid date), or null where d3 returns null.</returns>
    public double? Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TryParse(text, out var milliseconds) ? milliseconds : null;
    }
}
