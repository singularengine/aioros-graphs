using System.Buffers;

namespace Aioros.Graphs.Formatting;

/// <summary>
/// A number format of d3-format, compiled once from its specifier in a locale: d3's default en-US (thousands <c>,</c>
/// grouped by three, <c>$</c> currency, <c>.</c> decimal point, U+2212 minus sign) unless another is given.
/// </summary>
public sealed class NumberFormat
{
    private const int StackLength = 256;

    private readonly FormatSpecifier specifier;
    private readonly NumberLocale locale;
    private readonly char fill;
    private readonly char align;
    private readonly bool zero;
    private readonly bool comma;
    private readonly bool trim;
    private readonly char type;
    private readonly int precision;
    private readonly string prefix;
    private readonly string suffix;
    private readonly bool maybeSuffix;
    private readonly double scale;
    private readonly int width;

    private NumberFormat(FormatSpecifier specifier, NumberLocale locale, string suffixOption, double scale)
    {
        this.specifier = specifier;
        this.locale = locale;
        this.scale = scale;
        (type, comma, trim, var requested) = Normalize(specifier);
        zero = specifier.Zero || (specifier.Fill == '0' && specifier.Align == '=');
        fill = zero ? '0' : specifier.Fill;
        align = zero ? '=' : specifier.Align;
        prefix = SymbolPrefix(specifier.Symbol, type, locale);
        suffix = (specifier.Symbol == "$" ? locale.CurrencySuffix : type is '%' or 'p' ? locale.Percent : "") + suffixOption;
        maybeSuffix = type is 'd' or 'e' or 'f' or 'g' or 'p' or 'r' or 's' or '%';
        precision = requested is not { } p ? 6 : type is 'g' or 'p' or 'r' or 's' ? Math.Clamp(p, 1, 21) : Math.Clamp(p, 0, 20);
        width = specifier.Width ?? -1;
    }

    /// <summary>Compiles the format of <paramref name="specifier"/> in d3's default en-US locale, as d3's <c>format</c>.</summary>
    /// <param name="specifier">The specifier, such as <c>,.2f</c>.</param>
    /// <returns>The format.</returns>
    /// <exception cref="FormatException">The specifier is not valid.</exception>
    public static NumberFormat Create(string specifier) => Create(FormatSpecifier.Parse(specifier), NumberLocale.EnUs);

    /// <summary>Compiles the format of <paramref name="specifier"/> in <paramref name="locale"/>, as d3's <c>formatLocale(locale).format</c>.</summary>
    /// <param name="specifier">The specifier, such as <c>$,.2f</c>.</param>
    /// <param name="locale">The locale.</param>
    /// <returns>The format.</returns>
    /// <exception cref="FormatException">The specifier is not valid.</exception>
    public static NumberFormat Create(string specifier, NumberLocale locale) => Create(FormatSpecifier.Parse(specifier), locale);

    /// <summary>Compiles the format of <paramref name="specifier"/> in d3's default en-US locale.</summary>
    /// <param name="specifier">The parsed specifier.</param>
    /// <returns>The format.</returns>
    public static NumberFormat Create(FormatSpecifier specifier) => Create(specifier, NumberLocale.EnUs);

    /// <summary>Compiles the format of <paramref name="specifier"/> in <paramref name="locale"/>.</summary>
    /// <param name="specifier">The parsed specifier.</param>
    /// <param name="locale">The locale.</param>
    /// <returns>The format.</returns>
    public static NumberFormat Create(FormatSpecifier specifier, NumberLocale locale)
    {
        ArgumentNullException.ThrowIfNull(specifier);
        ArgumentNullException.ThrowIfNull(locale);
        return new NumberFormat(specifier, locale, "", 1);
    }

    /// <summary>
    /// Compiles a fixed-point format with one SI prefix chosen from <paramref name="value"/>, as d3's
    /// <c>formatPrefix</c>: every number is written in that prefix's unit.
    /// </summary>
    /// <param name="specifier">The specifier; its type is replaced by <c>f</c>.</param>
    /// <param name="value">The reference value the prefix is chosen from.</param>
    /// <returns>The format.</returns>
    /// <exception cref="FormatException">The specifier is not valid.</exception>
    public static NumberFormat CreatePrefix(string specifier, double value) => CreatePrefix(FormatSpecifier.Parse(specifier), value, NumberLocale.EnUs);

    /// <summary>Compiles a fixed-point format with one SI prefix chosen from <paramref name="value"/>, in <paramref name="locale"/>.</summary>
    /// <param name="specifier">The specifier; its type is replaced by <c>f</c>.</param>
    /// <param name="value">The reference value the prefix is chosen from.</param>
    /// <param name="locale">The locale.</param>
    /// <returns>The format.</returns>
    /// <exception cref="FormatException">The specifier is not valid.</exception>
    public static NumberFormat CreatePrefix(string specifier, double value, NumberLocale locale) => CreatePrefix(FormatSpecifier.Parse(specifier), value, locale);

    /// <summary>Compiles a fixed-point format with one SI prefix chosen from <paramref name="value"/>.</summary>
    /// <param name="specifier">The parsed specifier; its type is replaced by <c>f</c>.</param>
    /// <param name="value">The reference value the prefix is chosen from.</param>
    /// <returns>The format.</returns>
    public static NumberFormat CreatePrefix(FormatSpecifier specifier, double value) => CreatePrefix(specifier, value, NumberLocale.EnUs);

    /// <summary>Compiles a fixed-point format with one SI prefix chosen from <paramref name="value"/>, in <paramref name="locale"/>.</summary>
    /// <param name="specifier">The parsed specifier; its type is replaced by <c>f</c>.</param>
    /// <param name="value">The reference value the prefix is chosen from.</param>
    /// <param name="locale">The locale.</param>
    /// <returns>The format.</returns>
    public static NumberFormat CreatePrefix(FormatSpecifier specifier, double value, NumberLocale locale)
    {
        ArgumentNullException.ThrowIfNull(specifier);
        ArgumentNullException.ThrowIfNull(locale);
        var e = Math.Max(-8, Math.Min(8, Math.Floor(NumberText.Exponent(value) / 3))) * 3;
        var unit = double.IsNaN(e) ? "" : NumberText.Prefixes[8 + (int)e / 3];
        return new NumberFormat(specifier with { Type = "f" }, locale, unit, Math.Pow(10, -e));
    }

    /// <summary>Formats <paramref name="value"/>.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The text.</returns>
    public string Format(double value)
    {
        var parts = Parts(value);
        var capacity = Capacity(parts);
        var rented = capacity <= StackLength ? null : ArrayPool<char>.Shared.Rent(capacity);
        var buffer = rented is null ? stackalloc char[StackLength] : rented;
        try
        {
            return Finish(buffer[..Write(parts, buffer)]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    /// <summary>Returns the specifier as d3 normalizes it.</summary>
    /// <returns>The specifier.</returns>
    public override string ToString() => specifier.ToString();

    private static (char Type, bool Comma, bool Trim, int? Precision) Normalize(FormatSpecifier specifier)
    {
        if (specifier.Type == "n")
        {
            return ('g', true, specifier.Trim, specifier.Precision);
        }

        if (specifier.Type.Length == 0 || !"efgrs%pdxXobc".Contains(specifier.Type[0], StringComparison.Ordinal))
        {
            return ('g', specifier.Comma, true, specifier.Precision ?? 12);
        }

        return (specifier.Type[0], specifier.Comma, specifier.Trim, specifier.Precision);
    }

    private static string SymbolPrefix(string symbol, char type, NumberLocale locale)
    {
        if (symbol == "$")
        {
            return locale.CurrencyPrefix;
        }

        return symbol == "#" && type is 'b' or 'o' or 'x' or 'X' ? "0" + char.ToLowerInvariant(type) : "";
    }

    private Parts Parts(double value)
    {
        value *= scale;
        if (type == 'c')
        {
            return new Parts("", prefix, "", JsNumber.ToShortestString(value), "", suffix, "");
        }

        var negative = value < 0 || (value == 0 && double.IsNegative(value));
        int? prefixExponent = null;
        var text = double.IsNaN(value) ? locale.Nan : Body(Math.Abs(value), out prefixExponent);
        if (trim)
        {
            text = NumberText.Trim(text);
        }

        if (negative && NumberText.IsZero(text) && specifier.Sign != '+')
        {
            negative = false;
        }

        var si = type == 's' && !double.IsNaN(value) && prefixExponent is { } pe ? NumberText.Prefixes[8 + pe / 3] : "";
        var close = negative && specifier.Sign == '(' ? ")" : "";
        var split = maybeSuffix ? Split(text) : text.Length;
        return new Parts(SignText(negative), prefix, text[..split], text[split..], si, suffix, close);
    }

    private static int Split(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (!char.IsAsciiDigit(text[i]))
            {
                return i;
            }
        }

        return text.Length;
    }

    private string SignText(bool negative)
    {
        var sign = specifier.Sign;
        if (negative)
        {
            return sign == '(' ? "(" : locale.Minus;
        }

        return sign switch
        {
            '-' or '(' => "",
            '+' => "+",
            _ => " ",
        };
    }

    private string Body(double x, out int? prefixExponent)
    {
        prefixExponent = null;
        return type switch
        {
            '%' => JsNumber.ToFixed(x * 100, precision),
            'b' => NumberText.Radix(x, 2, false),
            'd' => NumberText.Decimal(x),
            'e' => JsNumber.ToExponential(x, precision),
            'f' => JsNumber.ToFixed(x, precision),
            'g' => JsNumber.ToPrecision(x, precision),
            'o' => NumberText.Radix(x, 8, false),
            'p' => NumberText.Rounded(x * 100, precision),
            'r' => NumberText.Rounded(x, precision),
            's' => NumberText.PrefixAuto(x, precision, out prefixExponent),
            'X' => NumberText.Radix(x, 16, true),
            _ => NumberText.Radix(x, 16, false),
        };
    }

    private int Capacity(Parts parts)
    {
        var separators = comma ? (locale.Thousands?.Length ?? 0) : 0;
        var digits = parts.Digits.Length + Math.Max(0, width);
        return parts.Sign.Length + parts.Prefix.Length + digits * (1 + separators) + parts.Tail.Length + locale.DecimalMark.Length
            + parts.Si.Length + parts.Suffix.Length + parts.Close.Length + Math.Max(0, width) + 4;
    }

    private int Write(Parts parts, Span<char> destination)
    {
        var prefixLength = parts.Sign.Length + parts.Prefix.Length;
        var suffixLength = TailLength(parts.Tail) + parts.Si.Length + parts.Suffix.Length + parts.Close.Length;
        var length = prefixLength + ValueLength(parts.Digits) + suffixLength;
        var padding = width > length ? width - length : 0;
        var p = 0;
        if (align == '>' || align == '^')
        {
            p = Fill(destination, p, align == '^' ? padding >> 1 : padding);
        }

        p = Append(destination, p, parts.Sign);
        p = Append(destination, p, parts.Prefix);
        if (comma && zero)
        {
            p += ZeroGrouped(parts.Digits, padding, suffixLength, destination[p..]);
            padding = 0;
        }
        else
        {
            p = Fill(destination, p, align == '=' ? padding : 0);
            p += comma && locale.Groups ? Group(parts.Digits, double.PositiveInfinity, destination[p..]) : Copy(parts.Digits, destination[p..]);
        }

        p = WriteTail(destination, p, parts.Tail);
        p = Append(destination, p, parts.Si);
        p = Append(destination, p, parts.Suffix);
        p = Append(destination, p, parts.Close);
        return align switch
        {
            '<' => Fill(destination, p, padding),
            '^' => Fill(destination, p, padding - (padding >> 1)),
            _ => p,
        };
    }

    private int ValueLength(string digits) => comma && locale.Groups && !zero ? GroupedLength(digits.Length) : digits.Length;

    private int ZeroGrouped(string digits, int padding, int suffixLength, Span<char> destination)
    {
        var size = padding + digits.Length;
        var rented = size <= StackLength ? null : ArrayPool<char>.Shared.Rent(size);
        var padded = rented is null ? stackalloc char[StackLength] : rented;
        padded[..padding].Fill(fill);
        digits.AsSpan().CopyTo(padded[padding..]);
        var written = locale.Groups ? Group(padded[..size], padding > 0 ? width - suffixLength : double.PositiveInfinity, destination) : Copy(padded[..size], destination);
        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }

        return written;
    }

    private int TailLength(string tail) => tail.Length > 0 && tail[0] == '.' ? locale.DecimalMark.Length + tail.Length - 1 : tail.Length;

    private int WriteTail(Span<char> destination, int p, string tail)
    {
        if (tail.Length > 0 && tail[0] == '.')
        {
            p = Append(destination, p, locale.DecimalMark);
            return Append(destination, p, tail.AsSpan(1));
        }

        return Append(destination, p, tail);
    }

    private int GroupedLength(int length)
    {
        var sizes = locale.GroupSizes;
        var i = length;
        var j = 0;
        var g = sizes[0];
        var chunks = 0;
        while (i > 0 && g > 0)
        {
            i -= g;
            chunks++;
            j = (j + 1) % sizes.Length;
            g = sizes[j];
        }

        return length - Math.Max(i, 0) + Math.Max(0, chunks - 1) * locale.Thousands!.Length;
    }

    private int Group(ReadOnlySpan<char> value, double limit, Span<char> destination)
    {
        var sizes = locale.GroupSizes;
        var thousands = locale.Thousands!;
        Span<int> bounds = stackalloc int[2 * Math.Min(value.Length + 1, 64)];
        int[]? rented = null;
        if (value.Length + 1 > 64)
        {
            rented = ArrayPool<int>.Shared.Rent(2 * (value.Length + 1));
            bounds = rented;
        }

        var i = value.Length;
        var j = 0;
        var g = sizes[0];
        var used = 0;
        var chunks = 0;
        while (i > 0 && g > 0)
        {
            if (used + g + 1 > limit)
            {
                g = (int)Math.Max(1, limit - used);
            }

            i -= g;
            bounds[2 * chunks] = Math.Max(0, i);
            bounds[(2 * chunks) + 1] = Math.Min(value.Length, i + g);
            chunks++;
            used += g + 1;
            if (used > limit)
            {
                break;
            }

            j = (j + 1) % sizes.Length;
            g = sizes[j];
        }

        var p = 0;
        for (var k = chunks - 1; k >= 0; k--)
        {
            var chunk = value[bounds[2 * k]..Math.Max(bounds[2 * k], bounds[(2 * k) + 1])];
            chunk.CopyTo(destination[p..]);
            p += chunk.Length;
            if (k > 0)
            {
                thousands.CopyTo(destination[p..]);
                p += thousands.Length;
            }
        }

        if (rented is not null)
        {
            ArrayPool<int>.Shared.Return(rented);
        }

        return p;
    }

    private string Finish(ReadOnlySpan<char> text)
    {
        if (locale.Digits is not { } numerals)
        {
            return new string(text);
        }

        var result = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            result = char.IsAsciiDigit(c) ? result.Append(numerals[c - '0']) : result.Append(c);
        }

        return result.ToString();
    }

    private int Fill(Span<char> destination, int p, int count)
    {
        destination.Slice(p, count).Fill(fill);
        return p + count;
    }

    private static int Append(Span<char> destination, int p, ReadOnlySpan<char> text)
    {
        text.CopyTo(destination[p..]);
        return p + text.Length;
    }

    private static int Copy(ReadOnlySpan<char> text, Span<char> destination)
    {
        text.CopyTo(destination);
        return text.Length;
    }
}

internal readonly record struct Parts(string Sign, string Prefix, string Digits, string Tail, string Si, string Suffix, string Close);
