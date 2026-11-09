using System.Text.Json;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Interpolation;
using Aioros.Graphs.Scales;

namespace Aioros.Graphs.Tests.Scales;

public sealed class SequentialScaleTests
{
    [Fact]
    [Trait("AC", "SCALE-25")]
    public async Task SequentialScalesMapAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-sequential", "sequential"))
        {
            var scale = Sequential(item);
            var inputs = item.Numbers("inputs");
            var batch = new double[inputs.Length];
            scale.Map(inputs, batch);

            GoldenFixture.Same(item.Numbers("outputs"), [.. inputs.Select(scale.Map)], item.ToString());
            GoldenFixture.Same(item.Numbers("outputs"), batch, item.ToString());
            GoldenFixture.Same(item.Numbers("ends"), [scale.Range.Start, scale.Range.End], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "SCALE-26")]
    public async Task SequentialTicksFormatsAndNiceMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-sequential", "sequential"))
        {
            var scale = Sequential(item);

            GoldenFixture.Same(item.Numbers("niceDomain"), scale.Domain.ToArray(), item.ToString());
            AssertTicks(item, scale.Ticks, (count, specifier) => scale.TickFormat(count, specifier).Format);
        }
    }

    [Fact]
    [Trait("AC", "SCALE-27")]
    public async Task DivergingScalesMapAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-sequential", "diverging"))
        {
            var scale = Diverging(item);
            var inputs = item.Numbers("inputs");
            var batch = new double[inputs.Length];
            scale.Map(inputs, batch);

            GoldenFixture.Same(item.Numbers("outputs"), [.. inputs.Select(scale.Map)], item.ToString());
            GoldenFixture.Same(item.Numbers("outputs"), batch, item.ToString());
            GoldenFixture.Same(item.Numbers("ends"), [scale.Range.Start, scale.Range.Middle, scale.Range.End], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "SCALE-28")]
    public async Task DivergingTicksFormatsAndNiceMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-sequential", "diverging"))
        {
            var scale = Diverging(item);

            GoldenFixture.Same(item.Numbers("niceDomain"), scale.Domain.ToArray(), item.ToString());
            AssertTicks(item, scale.Ticks, (count, specifier) => scale.TickFormat(count, specifier).Format);
        }
    }

    [Fact]
    [Trait("AC", "SCALE-29")]
    public async Task SequentialQuantileScalesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-sequential", "quantile"))
        {
            var scale = SequentialQuantileScale<double, TestInterpolator>.Create(item.Numbers("domain"), TestInterpolator.Identity).WithUnknown(double.NaN);

            GoldenFixture.Same(item.Numbers("sorted"), scale.Domain.ToArray(), item.ToString());
            GoldenFixture.Same(item.Numbers("outputs"), [.. item.Numbers("inputs").Select(scale.Map)], item.ToString());
            GoldenFixture.Same(item.Numbers("range"), scale.Range(), item.ToString());
            foreach (var quantiles in item.GetProperty("quantiles").EnumerateArray())
            {
                GoldenFixture.Same(quantiles.Numbers("quantiles"), scale.Quantiles((int)quantiles.Number("n")), item.ToString());
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-30")]
    public void DomainsNeedTheirStopsAndNaNMapsToTheUnknownValue()
    {
        Assert.Throws<ArgumentException>(() => SequentialScale<double, Counting>.Create([0, 1, 2], default));
        Assert.Throws<ArgumentException>(() => DivergingScale<double, Counting>.Create([0, 1], default));
        Assert.Throws<ArgumentException>(() => SequentialScale<double, Counting>.CreateLog([1], default));
        Assert.Throws<ArgumentException>(() => DivergingScale<double, Counting>.CreateSymlog([0, 1, 2, 3], default));

        Assert.Equal(-7, SequentialScale<double, Counting>.Create([0, 1], default).WithUnknown(-7).Map(double.NaN));
        Assert.Equal(-8, DivergingScale<double, Counting>.CreatePow([0, 1, 2], default, 2).WithUnknown(-8).Map(double.NaN));
        Assert.Equal(-9, SequentialQuantileScale<double, Counting>.Create([1, 2, 3], default).WithUnknown(-9).Map(double.NaN));
    }

    private static SequentialScale<double, TestInterpolator> Sequential(JsonElement item)
    {
        var domain = item.Numbers("domain");
        var interpolator = item.TryGetProperty("range", out var range) ? TestInterpolator.Two(range.Numbers(), item.TryGetProperty("round", out _)) : TestInterpolator.Identity;
        var scale = item.Text("kind") switch
        {
            "log" => SequentialScale<double, TestInterpolator>.CreateLog(domain, interpolator, item.OptionalNumber("base") ?? 10),
            "pow" => SequentialScale<double, TestInterpolator>.CreatePow(domain, interpolator, item.Number("exponent")),
            "sqrt" => SequentialScale<double, TestInterpolator>.CreateSqrt(domain, interpolator),
            "symlog" => SequentialScale<double, TestInterpolator>.CreateSymlog(domain, interpolator, item.OptionalNumber("constant") ?? 1),
            _ => SequentialScale<double, TestInterpolator>.Create(domain, interpolator),
        };
        scale = scale.WithUnknown(double.NaN).WithClamp(item.TryGetProperty("clamp", out _));
        return item.TryGetProperty("nice", out var nice) ? scale.Nice(nice.ValueKind == JsonValueKind.Number ? nice.GetDouble() : 10) : scale;
    }

    private static DivergingScale<double, TestInterpolator> Diverging(JsonElement item)
    {
        var domain = item.Numbers("domain");
        var interpolator = item.TryGetProperty("range", out var range) ? TestInterpolator.Three(range.Numbers(), item.TryGetProperty("round", out _)) : TestInterpolator.Identity;
        var scale = item.Text("kind") switch
        {
            "log" => DivergingScale<double, TestInterpolator>.CreateLog(domain, interpolator, item.OptionalNumber("base") ?? 10),
            "pow" => DivergingScale<double, TestInterpolator>.CreatePow(domain, interpolator, item.Number("exponent")),
            "sqrt" => DivergingScale<double, TestInterpolator>.CreateSqrt(domain, interpolator),
            "symlog" => DivergingScale<double, TestInterpolator>.CreateSymlog(domain, interpolator, item.OptionalNumber("constant") ?? 1),
            _ => DivergingScale<double, TestInterpolator>.Create(domain, interpolator),
        };
        scale = scale.WithUnknown(double.NaN).WithClamp(item.TryGetProperty("clamp", out _));
        return item.TryGetProperty("nice", out var nice) ? scale.Nice(nice.ValueKind == JsonValueKind.Number ? nice.GetDouble() : 10) : scale;
    }

    private static void AssertTicks(JsonElement item, Func<double, double[]> ticks, Func<double, string?, Func<double, string>> format)
    {
        foreach (var expected in item.GetProperty("ticks").EnumerateArray())
        {
            GoldenFixture.Same(expected.Numbers("ticks"), ticks(expected.Number("count")), $"{item} count {expected.Number("count")}");
        }

        foreach (var labels in item.GetProperty("formats").EnumerateArray())
        {
            var label = format(labels.Number("count"), labels.OptionalText("specifier"));
            var values = labels.Numbers("ticks");
            var expected = labels.GetProperty("labels").EnumerateArray().Select(e => e.GetString()).ToList();
            for (var i = 0; i < values.Length; i++)
            {
                Assert.True(expected[i] == label(values[i]), $"{item} {labels}: tick {values[i]:R} expected \"{expected[i]}\", got \"{label(values[i])}\"");
            }
        }
    }

    private readonly struct Counting : IInterpolator<double>
    {
        public double Interpolate(double t) => throw new InvalidOperationException("The interpolator was called.");
    }

    private readonly struct TestInterpolator : IInterpolator<double>
    {
        private readonly double[]? stops;
        private readonly bool round;

        private TestInterpolator(double[]? stops, bool round)
        {
            this.stops = stops;
            this.round = round;
        }

        public static TestInterpolator Identity => default;

        public static TestInterpolator Two(double[] stops, bool round) => new(stops, round);

        public static TestInterpolator Three(double[] stops, bool round) => new(stops, round);

        public double Interpolate(double t)
        {
            if (stops is null)
            {
                return t;
            }

            if (stops.Length == 2)
            {
                return Number(stops[0], stops[1], t);
            }

            t *= 2;
            var i = Math.Max(0, Math.Min(1, Math.Floor(t)));
            return Number(stops[(int)i], stops[(int)i + 1], t - i);
        }

        private double Number(double a, double b, double t)
        {
            var value = a * (1 - t) + b * t;
            return round ? JsNumber.Round(value) : value;
        }
    }
}
