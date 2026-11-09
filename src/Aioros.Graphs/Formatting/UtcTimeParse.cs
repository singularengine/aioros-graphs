namespace Aioros.Graphs.Formatting;

/// <summary>
/// A UTC date parser of d3-time-format (<c>utcParse</c>), compiled once from its specifier in a locale (d3's default
/// en-US unless given): <c>%a %A %b %B %c %d %e %f %g %G %H %I %j %L %m %M %p %q %Q %s %S %u %U %V %w %W %x %X %y %Y
/// %Z %%</c>, with the <c>-</c>, <c>_</c> and <c>0</c> modifiers accepted and ignored. Fields are read as d3 reads
/// them (numbers of at most the directive's width, after optional whitespace; names case-insensitively, the first name
/// in the locale's order that matches); a time without <c>%Z</c> is UTC.
/// </summary>
public sealed class UtcTimeParse
{
    private readonly TimeParseProgram program;

    private UtcTimeParse(TimeParseProgram program, string specifier)
    {
        this.program = program;
        Specifier = specifier;
    }

    /// <summary>Gets the specifier, as d3's <c>parse.toString()</c>.</summary>
    public string Specifier { get; }

    /// <summary>Compiles <paramref name="specifier"/>, as d3's <c>timeFormatLocale(locale).utcParse(specifier)</c>.</summary>
    /// <param name="specifier">The format, such as <c>%Y-%m-%dT%H:%M:%S.%LZ</c>.</param>
    /// <param name="locale">The locale; d3's default en-US when null.</param>
    /// <returns>The parser.</returns>
    public static UtcTimeParse Create(string specifier, TimeLocale? locale = null)
    {
        ArgumentNullException.ThrowIfNull(specifier);
        return new(TimeParseProgram.Compile(specifier, locale ?? TimeLocale.EnUs), specifier);
    }

    /// <summary>Parses <paramref name="text"/>.</summary>
    /// <param name="text">The text.</param>
    /// <param name="milliseconds">
    /// The time value: milliseconds since the Unix epoch, NaN where d3 returns an invalid date (a timestamp beyond
    /// ±8.64e15 ms).
    /// </param>
    /// <returns>False where d3's parser returns null: the text does not match the specifier.</returns>
    public bool TryParse(ReadOnlySpan<char> text, out double milliseconds) => program.TryParse(text, true, TimeZoneInfo.Utc, out milliseconds);

    /// <summary>Parses <paramref name="text"/>, as d3's parser called on it.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The time value (NaN for an invalid date), or null where d3 returns null.</returns>
    public double? Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TryParse(text, out var milliseconds) ? milliseconds : null;
    }
}
