using System.Globalization;
using System.Text;

namespace Aioros.Graphs.Formatting;

/// <summary>
/// A parsed d3-format specifier, <c>[[fill]align][sign][symbol][0][width][,][.precision][~][type]</c>, as d3's
/// <c>formatSpecifier</c>.
/// </summary>
public sealed record FormatSpecifier
{
    /// <summary>Gets the padding character; a space by default.</summary>
    public char Fill { get; init; } = ' ';

    /// <summary>Gets the alignment: <c>&gt;</c> (right, the default), <c>&lt;</c>, <c>^</c> or <c>=</c> (padding after the sign).</summary>
    public char Align { get; init; } = '>';

    /// <summary>Gets the sign: <c>-</c> (the default), <c>+</c>, <c>(</c> or a space.</summary>
    public char Sign { get; init; } = '-';

    /// <summary>Gets the symbol: <c>$</c>, <c>#</c> or empty.</summary>
    public string Symbol { get; init; } = "";

    /// <summary>Gets a value indicating whether zero padding is on.</summary>
    public bool Zero { get; init; }

    /// <summary>Gets the minimum width, if any.</summary>
    public int? Width { get; init; }

    /// <summary>Gets a value indicating whether thousands are grouped.</summary>
    public bool Comma { get; init; }

    /// <summary>Gets the precision, if any.</summary>
    public int? Precision { get; init; }

    /// <summary>Gets a value indicating whether insignificant trailing zeros are trimmed.</summary>
    public bool Trim { get; init; }

    /// <summary>Gets the type, one letter or <c>%</c>, or empty.</summary>
    public string Type { get; init; } = "";

    /// <summary>Parses <paramref name="specifier"/>.</summary>
    /// <param name="specifier">The specifier, such as <c>,.2f</c>.</param>
    /// <returns>The parsed specifier.</returns>
    /// <exception cref="FormatException">The specifier is not valid, as d3 throws "invalid format".</exception>
    public static FormatSpecifier Parse(string specifier)
    {
        ArgumentNullException.ThrowIfNull(specifier);
        var s = specifier.AsSpan();
        var i = 0;
        var fill = ' ';
        var align = '>';
        if (s.Length > 1 && IsAlign(s[1]) && !IsLineTerminator(s[0]))
        {
            (fill, align, i) = (s[0], s[1], 2);
        }
        else if (s.Length > 0 && IsAlign(s[0]))
        {
            (align, i) = (s[0], 1);
        }

        var sign = Take(s, ref i, "+-( ") ?? '-';
        var symbol = Take(s, ref i, "$#");
        var zero = Take(s, ref i, "0") is not null;
        var width = Digits(s, ref i, specifier);
        var comma = Take(s, ref i, ",") is not null;
        int? precision = null;
        if (i < s.Length && s[i] == '.')
        {
            i++;
            precision = Digits(s, ref i, specifier) ?? throw Invalid(specifier);
        }

        var trim = Take(s, ref i, "~") is not null;
        var type = i < s.Length && (char.IsAsciiLetter(s[i]) || s[i] == '%') ? s[i++] : '\0';
        if (i != s.Length)
        {
            throw Invalid(specifier);
        }

        return new FormatSpecifier
        {
            Fill = fill,
            Align = align,
            Sign = sign,
            Symbol = symbol is { } c ? (c == '$' ? "$" : "#") : "",
            Zero = zero,
            Width = width,
            Comma = comma,
            Precision = precision,
            Trim = trim,
            Type = type == '\0' ? "" : TypeText(type),
        };
    }

    private static bool IsAlign(char c) => c is '<' or '>' or '=' or '^';

    private static bool IsLineTerminator(char c) => c is '\n' or '\r' or '\u2028' or '\u2029';

    private static char? Take(ReadOnlySpan<char> s, ref int i, string options)
    {
        if (i < s.Length && options.Contains(s[i], StringComparison.Ordinal))
        {
            return s[i++];
        }

        return null;
    }

    private static int? Digits(ReadOnlySpan<char> s, ref int i, string specifier)
    {
        var start = i;
        while (i < s.Length && char.IsAsciiDigit(s[i]))
        {
            i++;
        }

        if (i == start)
        {
            return null;
        }

        return int.TryParse(s[start..i], NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : throw Invalid(specifier);
    }

    private static string TypeText(char type) => type switch
    {
        'f' => "f",
        's' => "s",
        'd' => "d",
        'e' => "e",
        'g' => "g",
        'r' => "r",
        '%' => "%",
        'p' => "p",
        'x' => "x",
        _ => type.ToString(),
    };

    private static FormatException Invalid(string specifier) => new("invalid format: " + specifier);

    /// <summary>Writes the specifier back as d3 normalizes it, every default included.</summary>
    /// <returns>The specifier.</returns>
    public override string ToString()
    {
        var text = new StringBuilder().Append(Fill).Append(Align).Append(Sign).Append(Symbol);
        if (Zero)
        {
            text.Append('0');
        }

        if (Width is { } width)
        {
            text.Append(Math.Max(1, width).ToString(CultureInfo.InvariantCulture));
        }

        if (Comma)
        {
            text.Append(',');
        }

        if (Precision is { } precision)
        {
            text.Append('.').Append(Math.Max(0, precision).ToString(CultureInfo.InvariantCulture));
        }

        if (Trim)
        {
            text.Append('~');
        }

        return text.Append(Type).ToString();
    }
}
