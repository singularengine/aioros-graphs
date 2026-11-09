using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Interpolation;

/// <summary>The kinds of value d3's generic <c>interpolate</c> tells apart.</summary>
public enum InterpolationKind
{
    /// <summary>JavaScript's <c>undefined</c>.</summary>
    Undefined,

    /// <summary>JavaScript's <c>null</c>.</summary>
    Null,

    /// <summary>A boolean.</summary>
    Boolean,

    /// <summary>A number.</summary>
    Number,

    /// <summary>A string.</summary>
    Text,

    /// <summary>A d3-color object.</summary>
    Color,

    /// <summary>A <c>Date</c>, held as its time value.</summary>
    Date,

    /// <summary>A <c>Float64Array</c>.</summary>
    NumberArray,

    /// <summary>An array of values.</summary>
    Array,

    /// <summary>A plain object: named members in JavaScript's property order.</summary>
    Record,
}

/// <summary>
/// A value as d3's generic <c>interpolate(a, b)</c> sees it: the typed union of the JavaScript values it tells apart
/// (<see cref="InterpolationKind"/>). Arrays and objects hold values, so nested data interpolates as in d3.
/// </summary>
public readonly struct InterpolationValue : IEquatable<InterpolationValue>
{
    private readonly object? reference;
    private readonly double number;
    private readonly Color color;

    private InterpolationValue(InterpolationKind kind, double number = 0, object? reference = null, Color color = default)
    {
        Kind = kind;
        this.number = number;
        this.reference = reference;
        this.color = color;
    }

    /// <summary>Gets JavaScript's <c>undefined</c>, the default value.</summary>
    public static InterpolationValue Undefined => default;

    /// <summary>Gets JavaScript's <c>null</c>.</summary>
    public static InterpolationValue Null => new(InterpolationKind.Null);

    /// <summary>Gets the kind of value.</summary>
    public InterpolationKind Kind { get; }

    /// <summary>Gets the boolean of a <see cref="InterpolationKind.Boolean"/> value.</summary>
    public bool Boolean => Kind == InterpolationKind.Boolean && number != 0;

    /// <summary>Gets the number of a <see cref="InterpolationKind.Number"/> value or the time value of a <see cref="InterpolationKind.Date"/>, NaN otherwise.</summary>
    public double Number => Kind is InterpolationKind.Number or InterpolationKind.Date ? number : double.NaN;

    /// <summary>Gets the text of a <see cref="InterpolationKind.Text"/> value, null otherwise.</summary>
    public string? Text => reference as string;

    /// <summary>Gets the color of a <see cref="InterpolationKind.Color"/> value.</summary>
    public Color Color => color;

    /// <summary>Gets the numbers of a <see cref="InterpolationKind.NumberArray"/> value, empty otherwise.</summary>
    public IReadOnlyList<double> Numbers => reference as double[] ?? [];

    /// <summary>Gets the elements of an <see cref="InterpolationKind.Array"/> value, empty otherwise.</summary>
    public IReadOnlyList<InterpolationValue> Items => reference as InterpolationValue[] ?? [];

    /// <summary>Gets the members of an <see cref="InterpolationKind.Record"/> value in property order, empty otherwise.</summary>
    public IReadOnlyList<KeyValuePair<string, InterpolationValue>> Members => reference as KeyValuePair<string, InterpolationValue>[] ?? [];

    /// <summary>Returns a boolean.</summary>
    /// <param name="value">The boolean.</param>
    /// <returns>The value.</returns>
    public static InterpolationValue FromBoolean(bool value) => new(InterpolationKind.Boolean, value ? 1 : 0);

    /// <summary>Returns a number.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The value.</returns>
    public static InterpolationValue FromNumber(double value) => new(InterpolationKind.Number, value);

    /// <summary>Returns a string.</summary>
    /// <param name="value">The text.</param>
    /// <returns>The value.</returns>
    public static InterpolationValue FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(InterpolationKind.Text, reference: value);
    }

    /// <summary>Returns a d3-color object.</summary>
    /// <param name="value">The color.</param>
    /// <returns>The value.</returns>
    public static InterpolationValue FromColor(Color value) => new(InterpolationKind.Color, color: value);

    /// <summary>Returns a date.</summary>
    /// <param name="milliseconds">The time value, clipped as <c>new Date(milliseconds)</c>.</param>
    /// <returns>The value.</returns>
    public static InterpolationValue FromDate(double milliseconds) => new(InterpolationKind.Date, Time.UtcCalendar.Clip(milliseconds));

    /// <summary>Returns a <c>Float64Array</c>.</summary>
    /// <param name="values">The numbers, copied.</param>
    /// <returns>The value.</returns>
    public static InterpolationValue FromNumbers(ReadOnlySpan<double> values) => new(InterpolationKind.NumberArray, reference: values.ToArray());

    /// <summary>Returns an array.</summary>
    /// <param name="items">The elements, copied.</param>
    /// <returns>The value.</returns>
    public static InterpolationValue FromArray(ReadOnlySpan<InterpolationValue> items) => new(InterpolationKind.Array, reference: items.ToArray());

    /// <summary>Returns a plain object, its members in JavaScript's property order (integer keys ascending, then the others in order).</summary>
    /// <param name="members">The members, with distinct names.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">A name appears twice.</exception>
    public static InterpolationValue FromObject(IEnumerable<KeyValuePair<string, InterpolationValue>> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        var list = new List<KeyValuePair<string, InterpolationValue>>(members);
        if (new HashSet<string>(list.Select(m => m.Key), StringComparer.Ordinal).Count != list.Count)
        {
            throw new ArgumentException("Member names must be distinct.", nameof(members));
        }

        return new(InterpolationKind.Record, reference: PropertyOrder.Sort(list));
    }

    /// <summary>Returns whether the two values are equal: the same kind and equal contents, NaN equal to NaN.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>Whether they are equal.</returns>
    public static bool operator ==(InterpolationValue left, InterpolationValue right) => left.Equals(right);

    /// <summary>Returns whether the two values differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>Whether they differ.</returns>
    public static bool operator !=(InterpolationValue left, InterpolationValue right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(InterpolationValue other) => Kind == other.Kind && Kind switch
    {
        InterpolationKind.Boolean or InterpolationKind.Number or InterpolationKind.Date => number.Equals(other.number),
        InterpolationKind.Text => string.Equals(Text, other.Text, StringComparison.Ordinal),
        InterpolationKind.Color => color.Equals(other.color),
        InterpolationKind.NumberArray => Numbers.SequenceEqual(other.Numbers),
        InterpolationKind.Array => Items.SequenceEqual(other.Items),
        InterpolationKind.Record => Members.SequenceEqual(other.Members),
        _ => true,
    };

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is InterpolationValue other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Kind, number, reference is string s ? s.GetHashCode(StringComparison.Ordinal) : 0, color);

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        InterpolationKind.Text => Text!,
        InterpolationKind.Number or InterpolationKind.Date => Formatting.JsNumber.ToShortestString(number),
        _ => Kind.ToString(),
    };
}

internal static class PropertyOrder
{
    public static KeyValuePair<string, InterpolationValue>[] Sort(List<KeyValuePair<string, InterpolationValue>> members)
    {
        var integers = new List<(uint Index, KeyValuePair<string, InterpolationValue> Member)>();
        var others = new List<KeyValuePair<string, InterpolationValue>>();
        foreach (var member in members)
        {
            if (IsIndex(member.Key, out var index))
            {
                integers.Add((index, member));
            }
            else
            {
                others.Add(member);
            }
        }

        integers.Sort((x, y) => x.Index.CompareTo(y.Index));
        var sorted = new KeyValuePair<string, InterpolationValue>[members.Count];
        var k = 0;
        foreach (var (_, member) in integers)
        {
            sorted[k++] = member;
        }

        foreach (var member in others)
        {
            sorted[k++] = member;
        }

        return sorted;
    }

    public static bool IsIndex(string key, out uint index)
    {
        index = 0;
        if (key.Length == 0 || key.Length > 10 || (key[0] == '0' && key.Length > 1))
        {
            return false;
        }

        ulong value = 0;
        foreach (var c in key)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }

            value = value * 10 + (ulong)(c - '0');
        }

        if (value >= uint.MaxValue)
        {
            return false;
        }

        index = (uint)value;
        return true;
    }
}
