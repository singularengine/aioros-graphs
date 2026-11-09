using System.Globalization;

namespace Aioros.Graphs.Formatting;

internal static class JsText
{
    public const string Whitespace = "\t\n\v\f\r \u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff";

    public static bool IsWhitespace(char c) => Whitespace.Contains(c, StringComparison.Ordinal);

    public static bool IsDigit(char c) => c is >= '0' and <= '9';

    public static char Canonicalize(char c)
    {
        var upper = char.ToUpperInvariant(c);
        return c >= 128 && upper < 128 ? c : upper;
    }

    public static bool StartsWithIgnoreCase(ReadOnlySpan<char> text, string prefix)
    {
        if (text.Length < prefix.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (text[i] != prefix[i] && Canonicalize(text[i]) != Canonicalize(prefix[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static string ToLower(ReadOnlySpan<char> text) =>
        text.ToString().Replace("\u0130", "i\u0307", StringComparison.Ordinal).ToLowerInvariant();

    public static bool LowerEquals(ReadOnlySpan<char> text, string lowered)
    {
        var length = 0;
        foreach (var c in text)
        {
            if (c == '\u0130')
            {
                if (length + 2 > lowered.Length || lowered[length] != 'i' || lowered[length + 1] != '\u0307')
                {
                    return false;
                }

                length += 2;
                continue;
            }

            if (length >= lowered.Length || char.ToLowerInvariant(c) != lowered[length])
            {
                return false;
            }

            length++;
        }

        return length == lowered.Length;
    }

    public static double ParseDigits(ReadOnlySpan<char> digits)
    {
        if (digits.Length <= 15)
        {
            long value = 0;
            foreach (var c in digits)
            {
                value = value * 10 + (c - '0');
            }

            return value;
        }

        return double.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    public static double ToNumber(ReadOnlySpan<char> text)
    {
        text = text.Trim(Whitespace);
        if (text.IsEmpty)
        {
            return 0;
        }

        if (text.Length > 2 && text[0] == '0' && Radix(text[1]) is var radix and > 0)
        {
            return Integer(text[2..], radix);
        }

        var body = text[0] is '+' or '-' ? text[1..] : text;
        if (body.SequenceEqual("Infinity"))
        {
            return text[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
        }

        return IsDecimal(body) ? double.Parse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture) : double.NaN;
    }

    private static int Radix(char c) => c switch
    {
        'x' or 'X' => 16,
        'o' or 'O' => 8,
        'b' or 'B' => 2,
        _ => 0,
    };

    private static int DigitValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'z' => c - 'a' + 10,
        >= 'A' and <= 'Z' => c - 'A' + 10,
        _ => 99,
    };

    private static double Integer(ReadOnlySpan<char> digits, int radix)
    {
        var value = 0.0;
        foreach (var c in digits)
        {
            var d = DigitValue(c);
            if (d >= radix)
            {
                return double.NaN;
            }

            value = value * radix + d;
        }

        return value;
    }

    private static int Digits(ReadOnlySpan<char> s, ref int i)
    {
        var start = i;
        while (i < s.Length && IsDigit(s[i]))
        {
            i++;
        }

        return i - start;
    }

    private static bool IsDecimal(ReadOnlySpan<char> s)
    {
        var i = 0;
        var digits = Digits(s, ref i);
        if (i < s.Length && s[i] == '.')
        {
            i++;
            digits += Digits(s, ref i);
        }

        if (digits == 0)
        {
            return false;
        }

        if (i < s.Length && s[i] is 'e' or 'E')
        {
            i++;
            if (i < s.Length && s[i] is '+' or '-')
            {
                i++;
            }

            if (Digits(s, ref i) == 0)
            {
                return false;
            }
        }

        return i == s.Length;
    }
}
