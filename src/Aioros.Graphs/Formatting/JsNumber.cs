using System.Globalization;
using System.Numerics;

namespace Aioros.Graphs.Formatting;

/// <summary>
/// Formats numbers exactly as JavaScript does: <c>Number.prototype.toString</c>, <c>toFixed</c>,
/// <c>toExponential</c> and <c>toPrecision</c>, and rounds as <c>Math.round</c>.
/// </summary>
public static class JsNumber
{
    /// <summary>The longest text <see cref="WriteShortest"/> and <see cref="WriteRounded"/> can write.</summary>
    public const int MaxShortestLength = 25;

    private const int MaxFastDigits = 6;

    private static ReadOnlySpan<double> DoublePowersOfTen =>
        [1, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15];

    /// <summary>Rounds as JavaScript's <c>Math.round</c>: to the nearest integer, halves toward positive infinity.</summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The rounded value; negative values that round to zero give negative zero.</returns>
    public static double Round(double value)
    {
        if (!double.IsFinite(value))
        {
            return value;
        }

        var floor = Math.Floor(value);
        var rounded = value - floor >= 0.5 ? floor + 1 : floor;
        return rounded == 0 && (value < 0 || double.IsNegative(value)) ? -0.0 : rounded;
    }

    /// <summary>Writes <paramref name="value"/> as UTF-8 the way JavaScript's <c>String(value)</c> does.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="destination">At least <see cref="MaxShortestLength"/> bytes.</param>
    /// <returns>The number of bytes written.</returns>
    public static int WriteShortest(double value, Span<byte> destination)
    {
        if (double.IsNaN(value))
        {
            return Copy("NaN"u8, destination);
        }

        if (value == 0)
        {
            destination[0] = (byte)'0';
            return 1;
        }

        if (double.IsFinite(value) && value.TryFormat(destination, out var formatted, default, CultureInfo.InvariantCulture) && destination[..formatted].IndexOf((byte)'E') < 0)
        {
            return formatted;
        }

        var written = 0;
        if (value < 0)
        {
            destination[written++] = (byte)'-';
            value = -value;
        }

        if (double.IsInfinity(value))
        {
            return written + Copy("Infinity"u8, destination[written..]);
        }

        Span<byte> digits = stackalloc byte[24];
        var count = Shortest(value, digits, out var point);
        return written + Layout(digits[..count], point, destination[written..]);
    }

    /// <summary>
    /// Writes <c>Math.round(value * 10 ** digits) / 10 ** digits</c> as UTF-8 the way JavaScript's <c>String</c> does,
    /// which is how d3-path writes coordinates rounded to <paramref name="digits"/> fraction digits.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="digits">The fraction digits, from 0 to 15.</param>
    /// <param name="destination">At least <see cref="MaxShortestLength"/> bytes.</param>
    /// <returns>The number of bytes written.</returns>
    public static int WriteRounded(double value, int digits, Span<byte> destination)
    {
        var scale = DoublePowersOfTen[digits];
        var rounded = Round(value * scale);
        if (digits > MaxFastDigits || !(Math.Abs(rounded) < 1e15))
        {
            return WriteShortest(rounded / scale, destination);
        }

        if (rounded == 0)
        {
            destination[0] = (byte)'0';
            return 1;
        }

        var written = 0;
        if (rounded < 0)
        {
            destination[written++] = (byte)'-';
            rounded = -rounded;
        }

        Span<byte> text = stackalloc byte[24];
        ((ulong)rounded).TryFormat(text, out var length, default, CultureInfo.InvariantCulture);
        var end = length;
        while (end > length - digits && end > 0 && text[end - 1] == '0')
        {
            end--;
        }

        var whole = length - digits;
        if (whole > 0)
        {
            text[..whole].CopyTo(destination[written..]);
            written += whole;
        }
        else
        {
            destination[written++] = (byte)'0';
        }

        if (end <= whole)
        {
            return written;
        }

        destination[written++] = (byte)'.';
        for (var zeros = whole; zeros < 0; zeros++)
        {
            destination[written++] = (byte)'0';
        }

        var fraction = text[Math.Max(whole, 0)..end];
        fraction.CopyTo(destination[written..]);
        return written + fraction.Length;
    }

    /// <summary>Formats <paramref name="value"/> as JavaScript's <c>String(value)</c>.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The text.</returns>
    public static string ToShortestString(double value)
    {
        Span<byte> buffer = stackalloc byte[MaxShortestLength];
        return Ascii(buffer[..WriteShortest(value, buffer)]);
    }

    /// <summary>Formats <paramref name="value"/> as JavaScript's <c>value.toFixed(fractionDigits)</c>.</summary>
    /// <param name="value">The value to format.</param>
    /// <param name="fractionDigits">The digits after the decimal point, from 0 to 100.</param>
    /// <returns>The text.</returns>
    public static string ToFixed(double value, int fractionDigits)
    {
        if (!double.IsFinite(value) || Math.Abs(value) >= 1e21)
        {
            return ToShortestString(value);
        }

        if (TryScaled(Math.Abs(value), fractionDigits, out var scaled))
        {
            Span<char> text = stackalloc char[48];
            var length = 0;
            if (value < 0)
            {
                text[length++] = '-';
            }

            Span<char> digits = stackalloc char[24];
            scaled.TryFormat(digits, out var count, default, CultureInfo.InvariantCulture);
            var whole = count - fractionDigits;
            if (whole <= 0)
            {
                text[length++] = '0';
            }
            else
            {
                digits[..whole].CopyTo(text[length..]);
                length += whole;
            }

            if (fractionDigits > 0)
            {
                text[length++] = '.';
                for (var zeros = whole; zeros < 0; zeros++)
                {
                    text[length++] = '0';
                }

                var fraction = digits[Math.Max(whole, 0)..count];
                fraction.CopyTo(text[length..]);
                length += fraction.Length;
            }

            return new string(text[..length]);
        }

        var digitsText = Scaled(Math.Abs(value), fractionDigits).ToString(CultureInfo.InvariantCulture);
        if (fractionDigits > 0)
        {
            digitsText = digitsText.PadLeft(fractionDigits + 1, '0');
            digitsText = digitsText[..^fractionDigits] + "." + digitsText[^fractionDigits..];
        }

        return value < 0 ? "-" + digitsText : digitsText;
    }

    /// <summary>Formats <paramref name="value"/> as JavaScript's <c>value.toExponential(fractionDigits)</c>.</summary>
    /// <param name="value">The value to format.</param>
    /// <param name="fractionDigits">The digits after the decimal point, or null for as many as needed to round-trip.</param>
    /// <returns>The text.</returns>
    public static string ToExponential(double value, int? fractionDigits)
    {
        if (!double.IsFinite(value))
        {
            return ToShortestString(value);
        }

        var sign = value < 0 ? "-" : "";
        value = Math.Abs(value);
        Span<byte> digits = stackalloc byte[128];
        int count;
        int exponent;
        if (fractionDigits is not { } fraction)
        {
            if (value == 0)
            {
                return sign + "0e+0";
            }

            count = Shortest(value, digits, out var point);
            exponent = point - 1;
        }
        else
        {
            count = Exact(value, fraction + 1, digits, out exponent);
        }

        return sign + Exponential(digits[..count], exponent);
    }

    /// <summary>Formats <paramref name="value"/> as JavaScript's <c>value.toPrecision(precision)</c>.</summary>
    /// <param name="value">The value to format.</param>
    /// <param name="precision">The significant digits, from 1 to 100.</param>
    /// <returns>The text.</returns>
    public static string ToPrecision(double value, int precision)
    {
        if (!double.IsFinite(value))
        {
            return ToShortestString(value);
        }

        var sign = value < 0 ? "-" : "";
        value = Math.Abs(value);
        Span<byte> digits = stackalloc byte[128];
        var count = Exact(value, precision, digits, out var exponent);
        var span = digits[..count];
        if (exponent < -6 || exponent >= precision)
        {
            return sign + Exponential(span, exponent);
        }

        if (exponent == precision - 1)
        {
            return sign + Ascii(span);
        }

        return exponent >= 0
            ? sign + Ascii(span[..(exponent + 1)]) + "." + Ascii(span[(exponent + 1)..])
            : sign + "0." + new string('0', -(exponent + 1)) + Ascii(span);
    }

    internal static int Shortest(double value, Span<byte> digits, out int point)
    {
        Span<char> text = stackalloc char[32];
        value.TryFormat(text, out var length, "R", CultureInfo.InvariantCulture);
        return Parse(text[..length], digits, out point);
    }

    internal static int Exponent(double value)
    {
        Span<byte> digits = stackalloc byte[24];
        Shortest(value, digits, out var point);
        return point - 1;
    }

    private static int Exact(double value, int precision, Span<byte> digits, out int exponent)
    {
        if (value == 0)
        {
            digits[..precision].Fill((byte)'0');
            exponent = 0;
            return precision;
        }

        exponent = (int)Math.Floor(Math.Log10(value));
        if (precision <= 15 && TryExact(value, precision, ref exponent, out var fast))
        {
            fast.TryFormat(digits, out var written, default, CultureInfo.InvariantCulture);
            return written;
        }

        var lower = BigInteger.Pow(10, precision - 1);
        var upper = lower * 10;
        var n = Scaled(value, precision - 1 - exponent);
        while (n >= upper)
        {
            exponent++;
            n = Scaled(value, precision - 1 - exponent);
        }

        while (n < lower)
        {
            exponent--;
            n = Scaled(value, precision - 1 - exponent);
        }

        var text = n.ToString(CultureInfo.InvariantCulture);
        for (var i = 0; i < text.Length; i++)
        {
            digits[i] = (byte)text[i];
        }

        return text.Length;
    }

    private static bool TryExact(double value, int precision, ref int exponent, out ulong n)
    {
        var lower = (ulong)DoublePowersOfTen[precision - 1];
        var upper = lower * 10;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            if (!TryScaled(value, precision - 1 - exponent, out n))
            {
                return false;
            }

            if (n >= upper)
            {
                exponent++;
            }
            else if (n < lower)
            {
                exponent--;
            }
            else
            {
                return true;
            }
        }

        n = 0;
        return false;
    }

    private static bool TryScaled(double value, int scale, out ulong n)
    {
        n = 0;
        if (value == 0)
        {
            return true;
        }

        if (scale < 0)
        {
            if (scale < -15 || !(value < 9007199254740992.0) || value != Math.Floor(value))
            {
                return false;
            }

            var whole = (ulong)value;
            var divisor = (ulong)DoublePowersOfTen[-scale];
            var remainder = whole % divisor;
            n = whole / divisor + (remainder * 2 >= divisor ? 1UL : 0UL);
            return true;
        }

        if (scale > 15)
        {
            return false;
        }

        var factor = DoublePowersOfTen[scale];
        var product = value * factor;
        if (!(product < 4503599627370496.0))
        {
            return false;
        }

        var error = Math.FusedMultiplyAdd(value, factor, -product);
        var floor = Math.Floor(product);
        var fraction = product - floor;
        n = (ulong)floor;
        if (fraction >= 0.25 && error >= 0.5 - fraction)
        {
            n++;
        }

        return true;
    }

    private static BigInteger Scaled(double value, int scale)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        var biased = (int)((bits >> 52) & 0x7FF);
        var mantissa = bits & 0xF_FFFF_FFFF_FFFFL;
        if (biased != 0)
        {
            mantissa |= 1L << 52;
        }

        var binary = (biased == 0 ? 1 : biased) - 1075;
        var numerator = new BigInteger(mantissa);
        var denominator = BigInteger.One;
        if (binary >= 0)
        {
            numerator <<= binary;
        }
        else
        {
            denominator <<= -binary;
        }

        if (scale >= 0)
        {
            numerator *= BigInteger.Pow(10, scale);
        }
        else
        {
            denominator *= BigInteger.Pow(10, -scale);
        }

        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        return remainder * 2 >= denominator ? quotient + 1 : quotient;
    }

    private static int Parse(ReadOnlySpan<char> text, Span<byte> digits, out int point)
    {
        var count = 0;
        var beforePoint = -1;
        var exponent = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '.')
            {
                beforePoint = count;
            }
            else if (c is 'E' or 'e')
            {
                exponent = int.Parse(text[(i + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                break;
            }
            else if (char.IsAsciiDigit(c))
            {
                digits[count++] = (byte)c;
            }
        }

        point = (beforePoint < 0 ? count : beforePoint) + exponent;
        var start = 0;
        while (start < count - 1 && digits[start] == '0')
        {
            start++;
            point--;
        }

        while (count > start + 1 && digits[count - 1] == '0')
        {
            count--;
        }

        digits[start..count].CopyTo(digits);
        return count - start;
    }

    private static int Layout(ReadOnlySpan<byte> digits, int point, Span<byte> destination)
    {
        var count = digits.Length;
        if (count <= point && point <= 21)
        {
            digits.CopyTo(destination);
            destination[count..point].Fill((byte)'0');
            return point;
        }

        if (0 < point && point <= 21)
        {
            digits[..point].CopyTo(destination);
            destination[point] = (byte)'.';
            digits[point..].CopyTo(destination[(point + 1)..]);
            return count + 1;
        }

        if (-6 < point && point <= 0)
        {
            destination[0] = (byte)'0';
            destination[1] = (byte)'.';
            destination[2..(2 - point)].Fill((byte)'0');
            digits.CopyTo(destination[(2 - point)..]);
            return 2 - point + count;
        }

        var written = 0;
        destination[written++] = digits[0];
        if (count > 1)
        {
            destination[written++] = (byte)'.';
            digits[1..].CopyTo(destination[written..]);
            written += count - 1;
        }

        destination[written++] = (byte)'e';
        var exponent = point - 1;
        destination[written++] = exponent < 0 ? (byte)'-' : (byte)'+';
        return written + WriteInteger(Math.Abs(exponent), destination[written..]);
    }

    private static string Exponential(ReadOnlySpan<byte> digits, int exponent)
    {
        var mantissa = digits.Length > 1 ? Ascii(digits[..1]) + "." + Ascii(digits[1..]) : Ascii(digits);
        return mantissa + (exponent < 0 ? "e-" : "e+") + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
    }

    private static int WriteInteger(long value, Span<byte> destination)
    {
        var length = 1;
        for (var rest = value / 10; rest > 0; rest /= 10)
        {
            length++;
        }

        for (var i = length - 1; i >= 0; i--)
        {
            destination[i] = (byte)('0' + value % 10);
            value /= 10;
        }

        return length;
    }

    private static int Copy(ReadOnlySpan<byte> text, Span<byte> destination)
    {
        text.CopyTo(destination);
        return text.Length;
    }

    private static string Ascii(ReadOnlySpan<byte> bytes) => System.Text.Encoding.ASCII.GetString(bytes);
}
