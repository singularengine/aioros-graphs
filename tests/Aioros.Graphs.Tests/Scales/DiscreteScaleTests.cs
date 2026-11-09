using System.Text.Json;
using Aioros.Graphs.Scales;

namespace Aioros.Graphs.Tests.Scales;

public sealed class DiscreteScaleTests
{
    [Fact]
    [Trait("AC", "SCALE-31")]
    public async Task OrdinalScalesMapAndGrowAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-discrete", "ordinal"))
        {
            var scale = OrdinalScale<string?, string?>.Create(Strings(item, "domain"), Strings(item, "range"));
            if (item.OptionalText("unknown") is { } unknown)
            {
                scale = scale.WithUnknown(unknown);
            }

            var outputs = new List<string?>();
            foreach (var probe in Strings(item, "probes"))
            {
                if (!scale.HasUnknown && !scale.TryMap(probe, out _))
                {
                    scale = scale.Extend([probe]);
                }

                outputs.Add(scale.Map(probe));
            }

            Assert.Equal(Strings(item, "outputs"), outputs);
            Assert.Equal(Strings(item, "finalDomain"), scale.Domain.ToArray());
        }
    }

    [Fact]
    [Trait("AC", "SCALE-32")]
    public void UnknownKeysThrowUnlessTheScaleHasAnUnknownValue()
    {
        var scale = OrdinalScale<string, int>.Create(["a", "b"], [1, 2, 3]);

        Assert.Throws<KeyNotFoundException>(() => scale.Map("c"));
        Assert.False(scale.TryMap("c", out _));
        Assert.Equal(-1, scale.WithUnknown(-1).Map("c"));
        Assert.Equal(7, OrdinalScale<string, int>.Create(["a"], []).WithUnknown(7).Map("a"));
    }

    [Fact]
    [Trait("AC", "SCALE-33")]
    public async Task QuantizeScalesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-discrete", "quantize"))
        {
            var domain = item.Numbers("domain");
            var range = Strings(item, "range");
            var scale = QuantizeScale<string?>.Create(domain[0], domain[1], range).WithUnknown(null);
            if (item.TryGetProperty("nice", out var nice))
            {
                scale = scale.Nice(nice.GetDouble());
            }

            GoldenFixture.Same(item.Numbers("niceDomain"), [scale.Domain.Start, scale.Domain.End], item.ToString());
            GoldenFixture.Same(item.Numbers("thresholds"), scale.Thresholds.ToArray(), item.ToString());
            Assert.Equal(Strings(item, "outputs"), item.Numbers("inputs").Select(scale.Map));
            AssertExtents(item, range, y => scale.InvertExtent(y));
            foreach (var ticks in item.GetProperty("ticks").EnumerateArray())
            {
                var count = ticks.Number("count");
                var values = scale.Ticks(count);
                GoldenFixture.Same(ticks.Numbers("ticks"), values, item.ToString());
                Assert.Equal(Strings(ticks, "labels"), values.Select(scale.TickFormat(count).Format));
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-34")]
    public async Task QuantileScalesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-discrete", "quantile"))
        {
            var range = Strings(item, "range");
            var scale = QuantileScale<string?>.Create(item.Numbers("domain"), range).WithUnknown(null);

            GoldenFixture.Same(item.Numbers("sorted"), scale.Domain.ToArray(), item.ToString());
            GoldenFixture.Same(item.Numbers("quantiles"), scale.Quantiles.ToArray(), item.ToString());
            Assert.Equal(Strings(item, "outputs"), item.Numbers("inputs").Select(scale.Map));
            AssertExtents(item, range, y => scale.InvertExtent(y));
        }
    }

    [Fact]
    [Trait("AC", "SCALE-35")]
    public async Task ThresholdScalesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-discrete", "threshold"))
        {
            var range = Strings(item, "range");
            var scale = ThresholdScale<string?>.Create(item.Numbers("domain"), range).WithUnknown(null);

            Assert.Equal(Strings(item, "outputs"), item.Numbers("inputs").Select(scale.Map));
            AssertExtents(item, range, y => scale.InvertExtent(y));
        }
    }

    [Fact]
    [Trait("AC", "SCALE-36")]
    public void QuantizeNeedsARangeAndNaNMapsToTheUnknownValue()
    {
        Assert.Throws<ArgumentException>(() => QuantizeScale<int>.Create(0, 1, []));
        Assert.Equal(-1, QuantizeScale<int>.Create(0, 1, [1, 2]).WithUnknown(-1).Map(double.NaN));
        Assert.Equal(-2, QuantileScale<int>.Create([1, 2, 3], [1, 2]).WithUnknown(-2).Map(double.NaN));
        Assert.Equal(-3, ThresholdScale<int>.Create([1], [1, 2]).WithUnknown(-3).Map(double.NaN));
    }

    private static string?[] Strings(JsonElement item, string property) =>
        [.. item.GetProperty(property).EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Null ? null : e.GetString())];

    private static void AssertExtents(JsonElement item, string?[] range, Func<string?, (double Low, double High)> invert)
    {
        var expected = item.GetProperty("extents").EnumerateArray().ToList();
        var probes = range.Append("missing").ToList();
        for (var i = 0; i < probes.Count; i++)
        {
            var (low, high) = invert(probes[i]);
            GoldenFixture.Same(expected[i].Numbers(), [low, high], $"{item} extent of {probes[i]}");
        }
    }
}
