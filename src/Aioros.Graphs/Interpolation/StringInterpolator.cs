using System.Buffers;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Interpolates the numbers embedded in two strings, as d3's <c>interpolateString(a, b)</c>: the numbers of
/// <c>a</c> and <c>b</c> are paired in order, those that differ are interpolated and written as JavaScript's
/// <c>String(x)</c>, and every other part of the text is <c>b</c>'s.
/// </summary>
public readonly struct StringInterpolator : IInterpolator<string>
{
    private const int StackLimit = 512;

    private readonly string?[] parts;
    private readonly NumberInterpolator[] numbers;
    private readonly int fixedLength;

    /// <summary>Initializes a new instance of the <see cref="StringInterpolator"/> struct.</summary>
    /// <param name="start">The text at 0.</param>
    /// <param name="end">The text at 1.</param>
    public StringInterpolator(string start, string end)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        var s = new List<string?>();
        var q = new List<NumberInterpolator>();
        int bi = 0, aFrom = 0, bFrom = 0;
        while (Next(start, ref aFrom, out var am) && Next(end, ref bFrom, out var bm))
        {
            if (bm.Start.Value > bi)
            {
                Append(s, end[bi..bm.Start.Value]);
            }

            var aText = start.AsSpan(am);
            var bText = end.AsSpan(bm);
            if (aText.SequenceEqual(bText))
            {
                Append(s, end[bm]);
            }
            else
            {
                s.Add(null);
                q.Add(new NumberInterpolator(JsText.ToNumber(aText), JsText.ToNumber(bText)));
            }

            bi = bm.End.Value;
        }

        if (bi < end.Length)
        {
            Append(s, end[bi..]);
        }

        parts = [.. s];
        numbers = [.. q];
        foreach (var part in parts)
        {
            fixedLength += part?.Length ?? 0;
        }
    }

    /// <summary>Gets the longest text <see cref="TryInterpolate"/> can write.</summary>
    public int MaxLength => fixedLength + (numbers?.Length ?? 0) * JsNumber.MaxShortestLength;

    /// <inheritdoc />
    public string Interpolate(double t)
    {
        var length = MaxLength;
        var rented = length > StackLimit ? ArrayPool<char>.Shared.Rent(length) : null;
        var buffer = rented is null ? stackalloc char[length] : rented.AsSpan(0, length);
        TryInterpolate(t, buffer, out var written);
        var text = new string(buffer[..written]);
        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }

        return text;
    }

    /// <summary>Writes the text at <paramref name="t"/> without allocating.</summary>
    /// <param name="t">The position.</param>
    /// <param name="destination">Where the text goes; <see cref="MaxLength"/> characters are always enough.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryInterpolate(double t, Span<char> destination, out int written)
    {
        written = 0;
        var next = 0;
        Span<byte> digits = stackalloc byte[JsNumber.MaxShortestLength];
        foreach (var part in parts ?? [])
        {
            if (part is not null)
            {
                if (!part.AsSpan().TryCopyTo(destination[written..]))
                {
                    return false;
                }

                written += part.Length;
                continue;
            }

            var n = JsNumber.WriteShortest(numbers[next++].Interpolate(t), digits);
            if (destination.Length - written < n)
            {
                return false;
            }

            for (var i = 0; i < n; i++)
            {
                destination[written++] = (char)digits[i];
            }
        }

        return true;
    }

    private static void Append(List<string?> s, string text)
    {
        if (s.Count > 0 && !string.IsNullOrEmpty(s[^1]))
        {
            s[^1] += text;
        }
        else
        {
            s.Add(text);
        }
    }

    private static bool Next(string text, ref int from, out Range match)
    {
        for (var p = from; p < text.Length; p++)
        {
            var end = MatchAt(text, p);
            if (end > p)
            {
                match = p..end;
                from = end;
                return true;
            }
        }

        match = default;
        return false;
    }

    private static int MatchAt(string s, int p)
    {
        var i = Mantissa(s, p < s.Length && s[p] is '+' or '-' ? p + 1 : p);
        return i < 0 ? p : Exponent(s, i);
    }

    private static int Mantissa(string s, int i)
    {
        if (Digit(s, i))
        {
            i = Digits(s, i);
            return i < s.Length && s[i] == '.' ? Digits(s, i + 1) : i;
        }

        return i < s.Length && s[i] == '.' && Digit(s, i + 1) ? Digits(s, i + 1) : -1;
    }

    private static int Exponent(string s, int i)
    {
        if (i >= s.Length || s[i] is not ('e' or 'E'))
        {
            return i;
        }

        var e = i + 1 < s.Length && s[i + 1] is '+' or '-' ? i + 2 : i + 1;
        return Digit(s, e) ? Digits(s, e) : i;
    }

    private static bool Digit(string s, int i) => i < s.Length && JsText.IsDigit(s[i]);

    private static int Digits(string s, int i)
    {
        while (Digit(s, i))
        {
            i++;
        }

        return i;
    }
}
