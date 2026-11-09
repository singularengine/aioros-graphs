using System.Text;
using Aioros.Graphs.Colors;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Interpolation;

/// <summary>
/// d3's generic <c>interpolate(a, b)</c> over <see cref="InterpolationValue"/>s: the interpolator is chosen by the kind
/// of <c>b</c> (a constant for undefined, null and booleans; numbers; colors in RGB, from a color or a string d3-color
/// parses; strings; dates; number arrays; arrays and objects, recursively), and <c>a</c> is coerced as JavaScript
/// coerces it. Built once; each call returns a new value.
/// </summary>
public sealed class ValueInterpolator : IInterpolator<InterpolationValue>
{
    private readonly INode node;

    internal ValueInterpolator(InterpolationValue a, InterpolationValue b)
    {
        node = Create(a, b);
    }

    private interface INode
    {
        InterpolationValue At(double t);
    }

    /// <inheritdoc />
    public InterpolationValue Interpolate(double t) => node.At(t);

    private static INode Create(InterpolationValue a, InterpolationValue b) => b.Kind switch
    {
        InterpolationKind.Number => new NumberNode(new NumberInterpolator(ToNumber(a), b.Number)),
        InterpolationKind.Text => Color.TryParse(b.Text, out var parsed) ? new RgbNode(new RgbInterpolator(ColorOf(a), parsed)) : new StringNode(new StringInterpolator(Text(a, true), b.Text!)),
        InterpolationKind.Color => new RgbNode(new RgbInterpolator(ColorOf(a), b.Color)),
        InterpolationKind.Date => new DateNode(new DateInterpolator(ToNumber(a), b.Number)),
        InterpolationKind.NumberArray => new NumberArrayNode(a, b),
        InterpolationKind.Array => new ArrayNode(a, b),
        InterpolationKind.Record => new ObjectNode(a, b),
        _ => new ConstantNode(b),
    };

    private static double ToNumber(InterpolationValue a) => a.Kind switch
    {
        InterpolationKind.Undefined or InterpolationKind.Color or InterpolationKind.Record => double.NaN,
        InterpolationKind.Null => 0,
        InterpolationKind.Boolean => a.Boolean ? 1 : 0,
        InterpolationKind.Number or InterpolationKind.Date => a.Number,
        _ => JsText.ToNumber(Text(a, false)),
    };

    private static Color ColorOf(InterpolationValue a) => a.Kind switch
    {
        InterpolationKind.Color => a.Color,
        InterpolationKind.Date => Color.Parse(null),
        _ => Color.Parse(Text(a, false)),
    };

    private static string Text(InterpolationValue a, bool strict)
    {
        switch (a.Kind)
        {
            case InterpolationKind.Undefined:
                return "undefined";
            case InterpolationKind.Null:
                return "null";
            case InterpolationKind.Boolean:
                return a.Boolean ? "true" : "false";
            case InterpolationKind.Number:
                return JsNumber.ToShortestString(a.Number);
            case InterpolationKind.Text:
                return a.Text!;
            case InterpolationKind.Color:
                return a.Color.FormatRgb();
            case InterpolationKind.Date:
                return strict ? throw new NotSupportedException("A date's text depends on the host's time zone names; interpolate its time value instead.") : "\0";
            case InterpolationKind.Record:
                return "[object Object]";
            default:
                var text = new StringBuilder();
                for (var i = 0; i < Length(a); i++)
                {
                    var item = Element(a, i);
                    text.Append(i > 0 ? "," : "").Append(item.Kind is InterpolationKind.Undefined or InterpolationKind.Null ? "" : Text(item, strict));
                }

                return text.ToString();
        }
    }

    private static bool IsTruthy(InterpolationValue a) => a.Kind switch
    {
        InterpolationKind.Undefined or InterpolationKind.Null => false,
        InterpolationKind.Boolean => a.Boolean,
        InterpolationKind.Number => a.Number != 0 && !double.IsNaN(a.Number),
        InterpolationKind.Text => a.Text!.Length > 0,
        _ => true,
    };

    private static double Length(InterpolationValue a) => a.Kind switch
    {
        InterpolationKind.Text => a.Text!.Length,
        InterpolationKind.Array => a.Items.Count,
        InterpolationKind.NumberArray => a.Numbers.Count,
        InterpolationKind.Record => TryMember(a, "length", out var length) ? ToNumber(length) : double.NaN,
        _ => double.NaN,
    };

    private static InterpolationValue Element(InterpolationValue a, int i) => a.Kind switch
    {
        InterpolationKind.Text => i < a.Text!.Length ? InterpolationValue.FromString(a.Text[i].ToString()) : default,
        InterpolationKind.Array => i < a.Items.Count ? a.Items[i] : default,
        InterpolationKind.NumberArray => i < a.Numbers.Count ? InterpolationValue.FromNumber(a.Numbers[i]) : default,
        InterpolationKind.Record => TryMember(a, i.ToString(System.Globalization.CultureInfo.InvariantCulture), out var member) ? member : default,
        _ => default,
    };

    private static bool TryMember(InterpolationValue a, string key, out InterpolationValue value)
    {
        switch (a.Kind)
        {
            case InterpolationKind.Record:
                foreach (var member in a.Members)
                {
                    if (member.Key == key)
                    {
                        value = member.Value;
                        return true;
                    }
                }

                break;
            case InterpolationKind.Array or InterpolationKind.NumberArray:
                if (key == "length")
                {
                    value = InterpolationValue.FromNumber(Length(a));
                    return true;
                }

                if (PropertyOrder.IsIndex(key, out var index) && index < Length(a))
                {
                    value = Element(a, (int)index);
                    return true;
                }

                break;
            case InterpolationKind.Color:
                return TryChannel(a.Color, key, out value);
        }

        value = default;
        return false;
    }

    private static bool TryChannel(Color color, string key, out InterpolationValue value)
    {
        var names = color.Space switch
        {
            ColorSpace.Rgb => "rgb",
            ColorSpace.Lab => "lab",
            ColorSpace.Hcl => "hcl",
            _ => "hsl",
        };
        var channel = key == "opacity" ? 3 : key.Length == 1 ? names.IndexOf(key[0], StringComparison.Ordinal) : -1;
        value = channel switch
        {
            0 => InterpolationValue.FromNumber(color.Channel0),
            1 => InterpolationValue.FromNumber(color.Channel1),
            2 => InterpolationValue.FromNumber(color.Channel2),
            3 => InterpolationValue.FromNumber(color.Opacity),
            _ => default,
        };
        return channel >= 0;
    }

    private sealed class ConstantNode(InterpolationValue value) : INode
    {
        public InterpolationValue At(double t) => value;
    }

    private sealed class NumberNode(NumberInterpolator interpolator) : INode
    {
        public InterpolationValue At(double t) => InterpolationValue.FromNumber(interpolator.Interpolate(t));
    }

    private sealed class RgbNode(RgbInterpolator interpolator) : INode
    {
        public InterpolationValue At(double t) => InterpolationValue.FromString(interpolator.Interpolate(t).FormatRgb());
    }

    private sealed class StringNode(StringInterpolator interpolator) : INode
    {
        public InterpolationValue At(double t) => InterpolationValue.FromString(interpolator.Interpolate(t));
    }

    private sealed class DateNode(DateInterpolator interpolator) : INode
    {
        public InterpolationValue At(double t) => InterpolationValue.FromDate(interpolator.Interpolate(t));
    }

    private sealed class NumberArrayNode : INode
    {
        private readonly double[] start;
        private readonly double[] end;

        public NumberArrayNode(InterpolationValue a, InterpolationValue b)
        {
            end = [.. b.Numbers];
            var n = IsTruthy(a) ? Math.Min(end.Length, Length(a)) : 0;
            start = new double[n > 0 ? (int)Math.Ceiling(n) : 0];
            for (var i = 0; i < start.Length; i++)
            {
                start[i] = ToNumber(Element(a, i));
            }
        }

        public InterpolationValue At(double t)
        {
            var c = end.ToArray();
            for (var i = 0; i < start.Length; i++)
            {
                c[i] = start[i] * (1 - t) + end[i] * t;
            }

            return InterpolationValue.FromNumbers(c);
        }
    }

    private sealed class ArrayNode : INode
    {
        private readonly INode[] interpolated;
        private readonly InterpolationValue[] end;

        public ArrayNode(InterpolationValue a, InterpolationValue b)
        {
            end = [.. b.Items];
            var na = IsTruthy(a) ? Math.Min(end.Length, Length(a)) : 0;
            if (!(na >= 0) || na != Math.Floor(na))
            {
                throw new ArgumentException("Invalid array length, as d3 throws a RangeError: the start has no length.", nameof(a));
            }

            interpolated = new INode[(int)na];
            for (var i = 0; i < interpolated.Length; i++)
            {
                interpolated[i] = Create(Element(a, i), end[i]);
            }
        }

        public InterpolationValue At(double t)
        {
            var c = end.ToArray();
            for (var i = 0; i < interpolated.Length; i++)
            {
                c[i] = interpolated[i].At(t);
            }

            return InterpolationValue.FromArray(c);
        }
    }

    private sealed class ObjectNode : INode
    {
        private readonly List<KeyValuePair<string, InterpolationValue>> constant = [];
        private readonly List<KeyValuePair<string, INode>> interpolated = [];

        public ObjectNode(InterpolationValue a, InterpolationValue b)
        {
            var source = IsObject(a) ? a : default;
            foreach (var member in b.Members)
            {
                if (TryMember(source, member.Key, out var start))
                {
                    interpolated.Add(new(member.Key, Create(start, member.Value)));
                }
                else
                {
                    constant.Add(member);
                }
            }
        }

        private static bool IsObject(InterpolationValue a) =>
            a.Kind is InterpolationKind.Array or InterpolationKind.NumberArray or InterpolationKind.Date or InterpolationKind.Color or InterpolationKind.Record;

        public InterpolationValue At(double t)
        {
            var members = new List<KeyValuePair<string, InterpolationValue>>(constant.Count + interpolated.Count);
            members.AddRange(constant);
            foreach (var member in interpolated)
            {
                members.Add(new(member.Key, member.Value.At(t)));
            }

            return InterpolationValue.FromObject(members);
        }
    }
}
