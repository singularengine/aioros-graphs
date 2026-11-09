using System.Text.Json;
using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Tests.Arrays;

public sealed class HistogramTests
{
    [Fact]
    [Trait("AC", "ARRAY-09")]
    public async Task CountThresholdsAndTheDefaultDomainBinAsD3Async()
    {
        foreach (var item in (await GoldenFixture.CasesAsync("array", "bin")).Where(c => !c.TryGetProperty("domain", out _) && c.GetProperty("thresholds").ValueKind != JsonValueKind.Array))
        {
            AssertBins(item);
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-10")]
    public async Task ThresholdValuesAndExplicitDomainsBinAsD3Async()
    {
        foreach (var item in (await GoldenFixture.CasesAsync("array", "bin")).Where(c => c.TryGetProperty("domain", out _) || c.GetProperty("thresholds").ValueKind == JsonValueKind.Array))
        {
            AssertBins(item);
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-11")]
    public void BinIndicesNeedASpanAsLongAsTheValues()
    {
        var bins = new int[3];

        Histogram.Compute([1, 50, 2], bins, ThresholdRule.Sturges, (0, 10));

        Assert.Equal(-1, bins[1]);
        Assert.Throws<ArgumentException>(() => Histogram.Compute([1, 2, 3], new int[2], ThresholdRule.Sturges));
    }

    private static void AssertBins(JsonElement item)
    {
        var values = item.Numbers("values");
        var thresholds = Thresholds(item.GetProperty("thresholds"));
        (double, double)? domain = item.TryGetProperty("domain", out var d) ? (d[0].Number(), d[1].Number()) : null;
        var expected = item.GetProperty("bins").EnumerateArray().ToList();
        var indices = new int[values.Length];

        var bins = Histogram.Compute(values, indices, thresholds, domain);

        Assert.True(expected.Count == bins.Length, $"{item}: got {bins.Length} bins");
        for (var i = 0; i < bins.Length; i++)
        {
            GoldenFixture.Same(expected[i].Number("x0"), bins[i].X0, $"{item} bin {i} x0");
            GoldenFixture.Same(expected[i].Number("x1"), bins[i].X1, $"{item} bin {i} x1");
            var members = expected[i].GetProperty("members").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            Assert.Equal(members.Length, bins[i].Count);
            Assert.Equal(members, Enumerable.Range(0, values.Length).Where(k => indices[k] == i));
        }
    }

    private static ThresholdRule Thresholds(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => ThresholdRule.Sturges,
        JsonValueKind.Array => ThresholdRule.At(element.Numbers()),
        JsonValueKind.Number => ThresholdRule.Uniform(element.GetDouble()),
        _ => element.GetString() switch
        {
            "scott" => ThresholdRule.Scott,
            "freedmanDiaconis" => ThresholdRule.FreedmanDiaconis,
            _ => ThresholdRule.Sturges,
        },
    };
}
