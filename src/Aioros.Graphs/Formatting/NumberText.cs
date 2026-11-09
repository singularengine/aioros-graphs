using System.Globalization;
using System.Numerics;
using System.Text;

namespace Aioros.Graphs.Formatting;

internal static class NumberText
{
    public static readonly string[] Prefixes = ["y", "z", "a", "f", "p", "n", "µ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y"];

    public static (string Coefficient, int Exponent)? Parts(double x, int precision)
    {
        if (!double.IsFinite(x) || x == 0)
        {
            return null;
        }

        var text = JsNumber.ToExponential(x, precision > 0 ? precision - 1 : null);
        var e = text.IndexOf('e', StringComparison.Ordinal);
        var coefficient = text[..e];
        return (coefficient.Length > 1 ? coefficient[0] + coefficient[2..] : coefficient, int.Parse(text[(e + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
    }

    public static double Exponent(double x) => double.IsFinite(x) && x != 0 ? JsNumber.Exponent(Math.Abs(x)) : double.NaN;

    public static string Rounded(double x, int precision)
    {
        if (Parts(x, precision) is not var (coefficient, exponent))
        {
            return JsNumber.ToShortestString(x);
        }

        if (exponent < 0)
        {
            return "0." + new string('0', -exponent - 1) + coefficient;
        }

        return coefficient.Length > exponent + 1
            ? coefficient[..(exponent + 1)] + "." + coefficient[(exponent + 1)..]
            : coefficient + new string('0', exponent - coefficient.Length + 1);
    }

    public static string PrefixAuto(double x, int precision, out int? prefixExponent)
    {
        if (Parts(x, precision) is not var (coefficient, exponent))
        {
            prefixExponent = null;
            return JsNumber.ToPrecision(x, precision);
        }

        var prefix = Math.Max(-8, Math.Min(8, (int)Math.Floor(exponent / 3.0))) * 3;
        prefixExponent = prefix;
        var i = exponent - prefix + 1;
        var n = coefficient.Length;
        if (i == n)
        {
            return coefficient;
        }

        if (i > n)
        {
            return coefficient + new string('0', i - n);
        }

        return i > 0
            ? coefficient[..i] + "." + coefficient[i..]
            : "0." + new string('0', -i) + Parts(x, Math.Max(0, precision + i - 1))!.Value.Coefficient;
    }

    public static string Decimal(double x)
    {
        x = JsNumber.Round(x);
        if (!(Math.Abs(x) >= 1e21))
        {
            return JsNumber.ToShortestString(x);
        }

        if (double.IsInfinity(x))
        {
            return x < 0 ? "-∞" : "∞";
        }

        var digits = JsNumber.ToShortestString(Math.Abs(x));
        var e = digits.IndexOf('e', StringComparison.Ordinal);
        var mantissa = digits[..e].Replace(".", "", StringComparison.Ordinal);
        var exponent = int.Parse(digits[(e + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        return (x < 0 ? "-" : "") + mantissa + new string('0', exponent + 1 - mantissa.Length);
    }

    public static string Radix(double x, int radix, bool upper)
    {
        x = JsNumber.Round(x);
        if (!double.IsFinite(x))
        {
            return upper ? JsNumber.ToShortestString(x).ToUpperInvariant() : JsNumber.ToShortestString(x);
        }

        var value = BigInteger.Abs(new BigInteger(x));
        if (value.IsZero)
        {
            return "0";
        }

        var text = new StringBuilder();
        while (!value.IsZero)
        {
            var digit = (int)(value % radix);
            text.Insert(0, (char)(digit < 10 ? '0' + digit : (upper ? 'A' : 'a') + digit - 10));
            value /= radix;
        }

        return (x < 0 ? "-" : "") + text;
    }

    public static string Trim(string s)
    {
        var i0 = -1;
        var i1 = -1;
        for (var i = 1; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '.')
            {
                i0 = i1 = i;
            }
            else if (c == '0')
            {
                i0 = i0 == 0 ? i : i0;
                i1 = i;
            }
            else if (c is >= '1' and <= '9')
            {
                i0 = i0 > 0 ? 0 : i0;
            }
            else
            {
                break;
            }
        }

        return i0 > 0 ? s[..i0] + s[(i1 + 1)..] : s;
    }

    public static string Group(string value, double width)
    {
        var parts = new List<string>();
        var i = value.Length;
        var g = 3;
        var length = 0;
        while (i > 0 && g > 0)
        {
            if (length + g + 1 > width)
            {
                g = (int)Math.Max(1, width - length);
            }

            i -= g;
            parts.Add(value[Math.Max(0, i)..(i + g)]);
            length += g + 1;
            if (length > width)
            {
                break;
            }

            g = 3;
        }

        parts.Reverse();
        return string.Join(',', parts);
    }

    public static bool IsZero(string text) =>
        text.Length == 0 || (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value == 0);
}
