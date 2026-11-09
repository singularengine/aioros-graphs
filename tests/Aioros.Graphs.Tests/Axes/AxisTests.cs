using System.Buffers;
using System.Text;
using System.Text.Json;
using Aioros.Graphs.Axes;
using Aioros.Graphs.Scales;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Axes;

public sealed class AxisTests
{
    [Fact]
    [Trait("AC", "AXIS-01")]
    public async Task LinearAxesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("axis", "linear"))
        {
            var (count, specifier) = TickArguments(item);
            AssertSame(item, Axis.Create(Orientation(item), Linear(item), count, specifier, Options(item)));
        }
    }

    [Fact]
    [Trait("AC", "AXIS-02")]
    public async Task ExplicitTickValuesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("axis", "values"))
        {
            var scale = Linear(item);
            var (count, specifier) = TickArguments(item);
            AssertSame(item, Axis.Create(Orientation(item), scale, item.Numbers("tickValues"), scale.TickFormat(count, specifier), Options(item)));
        }
    }

    [Fact]
    [Trait("AC", "AXIS-03")]
    public async Task BandAxesCenterTicksOnBandsAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("axis", "band"))
        {
            var keys = Keys(item);
            var range = item.Numbers("range");
            var scale = BandScale.Create(keys.Length, range[0], range[1]).WithRound(item.GetProperty("round").GetBoolean()).WithAlign(item.OptionalNumber("align") ?? 0.5);
            scale = item.OptionalNumber("padding") is { } padding
                ? scale.WithPadding(padding)
                : scale.WithPaddingInner(item.Number("paddingInner")).WithPaddingOuter(item.Number("paddingOuter"));
            AssertSame(item, Axis.Create(Orientation(item), scale, keys, Options(item)));
        }
    }

    [Fact]
    [Trait("AC", "AXIS-04")]
    public async Task PointAxesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("axis", "point"))
        {
            var keys = Keys(item);
            var range = item.Numbers("range");
            var scale = PointScale.Create(keys.Length, range[0], range[1]).WithPadding(item.Number("padding"));
            if (item.GetProperty("round").GetBoolean())
            {
                scale = scale.WithRangeRound(range[0], range[1]);
            }

            AssertSame(item, Axis.Create(Orientation(item), scale, keys, Options(item)));
        }
    }

    [Fact]
    [Trait("AC", "AXIS-05")]
    public async Task UtcAxesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("axis", "utc"))
        {
            var (count, specifier) = TickArguments(item);
            var scale = UtcScale.Create(item.Numbers("domain"), item.Numbers("range"));
            AssertSame(item, Axis.Create(Orientation(item), scale, count, specifier, Options(item)));
        }
    }

    [Fact]
    [Trait("AC", "AXIS-06")]
    public async Task LinesAreTheDomainPathAndOneLinePerTickAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("axis", "linear"))
        {
            var (count, specifier) = TickArguments(item);
            var axis = Axis.Create(Orientation(item), Linear(item), count, specifier, Options(item));
            var vertical = axis.Orientation is AxisOrientation.Left or AxisOrientation.Right;
            var expected = new StringBuilder(Lines(item.Text("domainPath")));
            foreach (var tick in axis.Ticks)
            {
                var p = Js(tick.Position);
                var end = Js(axis.TickLineEnd);
                expected.Append(vertical ? $"M0,{p}L{end},{p}" : $"M{p},0L{p},{end}");
            }

            Assert.Equal(expected.ToString(), GoldenFixture.Svg((ref writer) => axis.WriteLines(ref writer), null));
        }
    }

    [Fact]
    [Trait("AC", "AXIS-07")]
    public void MismatchedValuesPositionsOrLabelsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Axis.Create(AxisOrientation.Bottom, [0, 1], [0, 1], [0], ["a", "b"]));
        Assert.Throws<ArgumentException>(() => Axis.Create(AxisOrientation.Bottom, [0, 1], [0, 1], [0, 1], ["a"]));
        Assert.Throws<ArgumentException>(() => Axis.Create(AxisOrientation.Bottom, [], [0], [0], ["a"]));
    }

    [Fact]
    [Trait("AC", "AXIS-08")]
    public async Task LogPowAndSymlogAxesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("axis", "continuous"))
        {
            var (count, specifier) = TickArguments(item);
            var (domain, range, orientation) = (item.Numbers("domain"), item.Numbers("range"), Orientation(item));
            var axis = item.Text("kind") switch
            {
                "log" => Axis.Create(orientation, LogScale.Create(domain, range), count, specifier),
                "log2" => Axis.Create(orientation, LogScale.Create(domain, range).WithBase(2), count, specifier),
                "sqrt" => Axis.Create(orientation, PowScale.Sqrt(domain, range), count, specifier),
                "pow3" => Axis.Create(orientation, PowScale.Create(domain, range, 3), count, specifier),
                _ => Axis.Create(orientation, SymlogScale.Create(domain, range), count, specifier),
            };
            AssertSame(item, axis);
        }
    }

    [Fact]
    [Trait("AC", "AXIS-09")]
    public async Task LocalTimeAxesMatchD3InEachZoneAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("axis", "time"))
        {
            var (count, specifier) = TickArguments(item);
            var zone = TimeZoneInfo.FindSystemTimeZoneById(item.Text("zone"));
            var scale = TimeScale.Create(item.Numbers("domain"), item.Numbers("range"), zone);
            AssertSame(item, Axis.Create(Orientation(item), scale, count, specifier));
        }
    }

    private static void AssertSame(JsonElement item, AxisLayout axis)
    {
        var because = item.ToString()[..Math.Min(300, item.ToString().Length)];
        Assert.True(item.Text("domainPath") == axis.DomainPath(), $"{because}: domain {axis.DomainPath()}");
        var transforms = item.GetProperty("transforms").EnumerateArray().Select(e => e.GetString()).ToList();
        var labels = item.GetProperty("labels").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.True(transforms.Count == axis.Ticks.Count, $"{because}: {axis.Ticks.Count} ticks");
        for (var i = 0; i < transforms.Count; i++)
        {
            Assert.True(transforms[i] == axis.TickTransform(i), $"{because}: transform {i} {axis.TickTransform(i)}");
            Assert.True(labels[i] == axis.Ticks[i].Label, $"{because}: label {i} {axis.Ticks[i].Label}");
        }

        var buffer = new ArrayBufferWriter<byte>();
        SvgAxisWriter.Write(buffer, axis);
        Assert.Equal(item.Text("markup"), Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    private static LinearScale Linear(JsonElement item) => LinearScale.Create(item.Numbers("domain"), item.Numbers("range"));

    private static AxisOrientation Orientation(JsonElement item) => item.Text("orient") switch
    {
        "top" => AxisOrientation.Top,
        "right" => AxisOrientation.Right,
        "left" => AxisOrientation.Left,
        _ => AxisOrientation.Bottom,
    };

    private static (double Count, string? Specifier) TickArguments(JsonElement item)
    {
        var arguments = item.GetProperty("tickArguments").EnumerateArray().ToList();
        return (arguments.Count > 0 ? arguments[0].Number() : 10, arguments.Count > 1 ? arguments[1].GetString() : null);
    }

    private static AxisOptions Options(JsonElement item)
    {
        var options = item.GetProperty("options");
        var result = new AxisOptions();
        if (options.OptionalNumber("tickSize") is { } size)
        {
            result = result with { TickSizeInner = size, TickSizeOuter = size };
        }

        return result with
        {
            TickSizeInner = options.OptionalNumber("tickSizeInner") ?? result.TickSizeInner,
            TickSizeOuter = options.OptionalNumber("tickSizeOuter") ?? result.TickSizeOuter,
            TickPadding = options.OptionalNumber("tickPadding") ?? result.TickPadding,
            Offset = options.OptionalNumber("offset") ?? result.Offset,
        };
    }

    private static string[] Keys(JsonElement item) => [.. item.GetProperty("keys").EnumerateArray().Select(e => e.GetString()!)];

    private static string Js(double value) => Aioros.Graphs.Formatting.JsNumber.ToShortestString(value);

    private static string Lines(string domain)
    {
        var text = new StringBuilder();
        var (x, y) = (0.0, 0.0);
        foreach (var command in System.Text.RegularExpressions.Regex.Matches(domain, "[MHV][^MHV]*").Select(m => m.Value))
        {
            var numbers = command[1..].Split(',').Select(n => double.Parse(n, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            (x, y) = command[0] switch
            {
                'M' => (numbers[0], numbers[1]),
                'H' => (numbers[0], y),
                _ => (x, numbers[0]),
            };
            text.Append(command[0] == 'M' ? 'M' : 'L').Append(Js(x)).Append(',').Append(Js(y));
        }

        return text.ToString();
    }
}
