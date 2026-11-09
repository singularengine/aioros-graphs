using System.Text.Json;
using Aioros.Graphs.Scales;

namespace Aioros.Graphs.Tests.Scales;

public sealed class LinearScaleTests
{
    [Fact]
    [Trait("AC", "SCALE-01")]
    public async Task MappingMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "linear"))
        {
            var scale = Build(item);
            var inputs = item.Numbers("inputs");
            var expected = item.Numbers("outputs");
            var batch = new double[inputs.Length];
            scale.Map(inputs, batch);

            GoldenFixture.Same(expected, [.. inputs.Select(scale.Map)], item.ToString());
            GoldenFixture.Same(expected, batch, item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "SCALE-02")]
    public void NaNMapsToNaN()
    {
        Assert.True(double.IsNaN(LinearScale.Create([0, 10], [0, 100]).Map(double.NaN)));
        Assert.True(double.IsNaN(LinearScale.Create([0, 5, 10], [0, 50, 100]).WithClamp(true).Map(double.NaN)));
    }

    [Fact]
    [Trait("AC", "SCALE-03")]
    public async Task ClampedScalesStayWithinTheirBoundsAsync()
    {
        var clamped = (await GoldenFixture.CasesAsync("scale", "linear")).Where(c => c.TryGetProperty("clamp", out _)).ToList();
        Assert.NotEmpty(clamped);
        foreach (var item in clamped)
        {
            var scale = Build(item);
            var inputs = item.Numbers("inputs");

            GoldenFixture.Same(item.Numbers("outputs"), [.. inputs.Select(scale.Map)], item.ToString());
            GoldenFixture.Same(item.Numbers("inverted"), [.. inputs.Select(scale.Invert)], item.ToString());
            Assert.True(scale.IsClamped);
        }
    }

    [Fact]
    [Trait("AC", "SCALE-04")]
    public async Task RoundedRangesRoundLikeD3Async()
    {
        var rounded = (await GoldenFixture.CasesAsync("scale", "linear")).Where(c => c.TryGetProperty("round", out _)).ToList();
        Assert.NotEmpty(rounded);
        foreach (var item in rounded)
        {
            var scale = Build(item);

            GoldenFixture.Same(item.Numbers("outputs"), [.. item.Numbers("inputs").Select(scale.Map)], item.ToString());
            Assert.True(scale.IsRounded);
        }
    }

    [Fact]
    [Trait("AC", "SCALE-05")]
    public async Task InversionMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "linear"))
        {
            var scale = Build(item);

            GoldenFixture.Same(item.Numbers("inverted"), [.. item.Numbers("inputs").Select(scale.Invert)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "SCALE-06")]
    public async Task TicksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "linear"))
        {
            var scale = Build(item);
            foreach (var ticks in item.GetProperty("ticks").EnumerateArray())
            {
                var count = ticks.Number("count");
                var expected = ticks.Numbers("ticks");
                var span = new double[expected.Length + 2];

                GoldenFixture.Same(expected, scale.Ticks(count), $"{item} ticks({count})");
                GoldenFixture.Same(expected, span[..scale.Ticks(count, span)], $"{item} ticks({count}) span");
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-07")]
    public async Task NiceDomainsMatchD3Async()
    {
        var niced = (await GoldenFixture.CasesAsync("scale", "linear")).Where(c => c.TryGetProperty("nice", out _)).ToList();
        Assert.NotEmpty(niced);
        foreach (var item in niced)
        {
            var original = LinearScale.Create(item.Numbers("domain"), item.Numbers("range"));
            var nice = original.Nice(item.Number("nice"));

            GoldenFixture.Same(item.Numbers("niceDomain"), nice.Domain.ToArray(), item.ToString());
            GoldenFixture.Same(item.Numbers("domain"), original.Domain.ToArray(), item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "SCALE-11")]
    public void ScalesRejectIncompleteDomainsRangesAndNegativeCounts()
    {
        Assert.Throws<ArgumentException>(() => LinearScale.Create([1], [0, 1]));
        Assert.Throws<ArgumentException>(() => LinearScale.Create([0, 1], []));
        Assert.Throws<ArgumentException>(() => LinearScale.Create([0, 1], [0, 1]).WithDomain([2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => BandScale.Create(-1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PointScale.Create(-1, 0, 1));
    }

    internal static LinearScale Build(JsonElement item)
    {
        var scale = LinearScale.Create(item.Numbers("domain"), [0, 1]);
        scale = item.TryGetProperty("round", out _) ? scale.WithRangeRound(item.Numbers("range")) : scale.WithRange(item.Numbers("range"));
        if (item.TryGetProperty("clamp", out _))
        {
            scale = scale.WithClamp(true);
        }

        return item.OptionalNumber("nice") is { } count ? scale.Nice(count) : scale;
    }
}
