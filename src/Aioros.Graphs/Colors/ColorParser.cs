using System.Buffers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Aioros.Graphs.Colors;

internal static partial class ColorParser
{
    private const string Space = @"[\t\n\v\f\r \u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000\ufeff]*";
    private const string Integer = Space + "([+-]?[0-9]+)" + Space;
    private const string Number = Space + @"([+-]?(?:[0-9]*\.)?[0-9]+(?:[eE][+-]?[0-9]+)?)" + Space;
    private const string Percent = Space + @"([+-]?(?:[0-9]*\.)?[0-9]+(?:[eE][+-]?[0-9]+)?)%" + Space;

    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdef");

    public static bool TryParse(string? specifier, out Color color)
    {
        color = default;
        if (specifier is null)
        {
            return false;
        }

        var format = Lower(specifier);
        if (format.StartsWith('#'))
        {
            return TryHex(format.AsSpan(1), out color);
        }

        if (format.StartsWith("rgba(", StringComparison.Ordinal))
        {
            return TryRgba(format, out color);
        }

        if (format.StartsWith("rgb(", StringComparison.Ordinal))
        {
            return TryRgb(format, out color);
        }

        if (format.StartsWith("hsl", StringComparison.Ordinal))
        {
            return TryHsl(format, out color);
        }

        return TryName(format, out color);
    }

    private static bool TryRgb(string format, out Color color)
    {
        color = default;
        if (RgbInteger().Match(format) is { Success: true } integer)
        {
            color = new Rgb(Value(integer, 1), Value(integer, 2), Value(integer, 3));
        }
        else if (RgbPercent().Match(format) is { Success: true } percent)
        {
            color = new Rgb(Value(percent, 1) * 255 / 100, Value(percent, 2) * 255 / 100, Value(percent, 3) * 255 / 100);
        }
        else
        {
            return false;
        }

        return true;
    }

    private static bool TryRgba(string format, out Color color)
    {
        color = default;
        if (RgbaInteger().Match(format) is { Success: true } rgba)
        {
            color = Rgba(Value(rgba, 1), Value(rgba, 2), Value(rgba, 3), Value(rgba, 4));
        }
        else if (RgbaPercent().Match(format) is { Success: true } rgbaPercent)
        {
            color = Rgba(Value(rgbaPercent, 1) * 255 / 100, Value(rgbaPercent, 2) * 255 / 100, Value(rgbaPercent, 3) * 255 / 100, Value(rgbaPercent, 4));
        }
        else
        {
            return false;
        }

        return true;
    }

    private static bool TryHsl(string format, out Color color)
    {
        color = default;
        if (HslPercent().Match(format) is { Success: true } hsl)
        {
            color = Hsla(Value(hsl, 1), Value(hsl, 2) / 100, Value(hsl, 3) / 100, 1);
        }
        else if (HslaPercent().Match(format) is { Success: true } hsla)
        {
            color = Hsla(Value(hsla, 1), Value(hsla, 2) / 100, Value(hsla, 3) / 100, Value(hsla, 4));
        }
        else
        {
            return false;
        }

        return true;
    }

    private static bool TryName(string format, out Color color)
    {
        if (NamedColors.Find(format) is >= 0 and var n)
        {
            color = Rgbn(n);
            return true;
        }

        color = new Rgb(double.NaN, double.NaN, double.NaN, 0);
        return format == "transparent";
    }

    private static bool TryHex(ReadOnlySpan<char> digits, out Color color)
    {
        color = default;
        if (digits.Length is < 3 or > 8 || digits.ContainsAnyExcept(HexDigits))
        {
            return false;
        }

        var m = uint.Parse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        color = digits.Length switch
        {
            6 => Rgbn((int)m),
            3 => new Rgb(((m >> 8) & 0xf) | ((m >> 4) & 0xf0), ((m >> 4) & 0xf) | (m & 0xf0), ((m & 0xf) << 4) | (m & 0xf)),
            8 => Rgba((m >> 24) & 0xff, (m >> 16) & 0xff, (m >> 8) & 0xff, (m & 0xff) / 255.0),
            4 => Rgba(((m >> 12) & 0xf) | ((m >> 8) & 0xf0), ((m >> 8) & 0xf) | ((m >> 4) & 0xf0), ((m >> 4) & 0xf) | (m & 0xf0), (((m & 0xf) << 4) | (m & 0xf)) / 255.0),
            _ => default,
        };
        return digits.Length is 3 or 4 or 6 or 8;
    }

    private static Rgb Rgbn(int n) => new((n >> 16) & 0xff, (n >> 8) & 0xff, n & 0xff);

    private static Rgb Rgba(double r, double g, double b, double a) =>
        a <= 0 ? new Rgb(double.NaN, double.NaN, double.NaN, a) : new Rgb(r, g, b, a);

    private static Hsl Hsla(double h, double s, double l, double a)
    {
        if (a <= 0)
        {
            return new Hsl(double.NaN, double.NaN, double.NaN, a);
        }

        if (l <= 0 || l >= 1)
        {
            return new Hsl(double.NaN, double.NaN, l, a);
        }

        return s <= 0 ? new Hsl(double.NaN, s, l, a) : new Hsl(h, s, l, a);
    }

    private static double Value(Match match, int group) =>
        double.Parse(match.Groups[group].ValueSpan, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static string Lower(string specifier)
    {
        var text = specifier.AsSpan().Trim("\t\n\v\f\r \u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff");
        if (text.Length == specifier.Length && !text.ContainsAnyInRange('A', 'Z') && !text.Contains('\u212a'))
        {
            return specifier;
        }

        return string.Create(text.Length, text, static (destination, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                destination[i] = c switch
                {
                    >= 'A' and <= 'Z' => (char)(c + 32),
                    '\u212a' => 'k',
                    _ => c,
                };
            }
        });
    }

    [GeneratedRegex(@"^rgb\(" + Integer + "," + Integer + "," + Integer + @"\)\z", RegexOptions.CultureInvariant)]
    private static partial Regex RgbInteger();

    [GeneratedRegex(@"^rgb\(" + Percent + "," + Percent + "," + Percent + @"\)\z", RegexOptions.CultureInvariant)]
    private static partial Regex RgbPercent();

    [GeneratedRegex(@"^rgba\(" + Integer + "," + Integer + "," + Integer + "," + Number + @"\)\z", RegexOptions.CultureInvariant)]
    private static partial Regex RgbaInteger();

    [GeneratedRegex(@"^rgba\(" + Percent + "," + Percent + "," + Percent + "," + Number + @"\)\z", RegexOptions.CultureInvariant)]
    private static partial Regex RgbaPercent();

    [GeneratedRegex(@"^hsl\(" + Number + "," + Percent + "," + Percent + @"\)\z", RegexOptions.CultureInvariant)]
    private static partial Regex HslPercent();

    [GeneratedRegex(@"^hsla\(" + Number + "," + Percent + "," + Percent + "," + Number + @"\)\z", RegexOptions.CultureInvariant)]
    private static partial Regex HslaPercent();
}
