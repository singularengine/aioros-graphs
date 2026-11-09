using Aioros.Graphs.Scales;

namespace Aioros.Graphs.Tests.Scales;

public sealed class OrdinalScaleTests
{
    [Fact]
    [Trait("AC", "SCALE-08")]
    public async Task BandsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "band"))
        {
            var range = item.Numbers("range");
            var scale = BandScale.Create((int)item.Number("count"), range[0], range[1]);
            if (item.TryGetProperty("round", out _))
            {
                scale = scale.WithRangeRound(range[0], range[1]);
            }

            if (item.OptionalNumber("padding") is { } padding)
            {
                scale = scale.WithPadding(padding);
            }

            if (item.OptionalNumber("paddingInner") is { } inner)
            {
                scale = scale.WithPaddingInner(inner);
            }

            if (item.OptionalNumber("paddingOuter") is { } outer)
            {
                scale = scale.WithPaddingOuter(outer);
            }

            if (item.OptionalNumber("align") is { } align)
            {
                scale = scale.WithAlign(align);
            }

            GoldenFixture.Same(item.Number("step"), scale.Step, $"{item} step");
            GoldenFixture.Same(item.Number("bandwidth"), scale.Bandwidth, $"{item} bandwidth");
            GoldenFixture.Same(item.Numbers("values"), [.. Enumerable.Range(0, scale.Count).Select(scale.Map)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "SCALE-09")]
    public void UnknownIndicesMapToNaN()
    {
        var band = BandScale.Create(3, 0, 300);
        var point = PointScale.Create(3, 0, 300);

        Assert.True(double.IsNaN(band.Map(-1)));
        Assert.True(double.IsNaN(band.Map(3)));
        Assert.True(double.IsNaN(point.Map(-1)));
        Assert.True(double.IsNaN(point.Map(3)));
        Assert.Equal(100, band.Map(1));
    }

    [Fact]
    [Trait("AC", "SCALE-10")]
    public async Task PointsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "point"))
        {
            var range = item.Numbers("range");
            var scale = PointScale.Create((int)item.Number("count"), range[0], range[1]);
            if (item.TryGetProperty("round", out _))
            {
                scale = scale.WithRangeRound(range[0], range[1]);
            }

            if (item.OptionalNumber("padding") is { } padding)
            {
                scale = scale.WithPadding(padding);
            }

            if (item.OptionalNumber("align") is { } align)
            {
                scale = scale.WithAlign(align);
            }

            GoldenFixture.Same(item.Number("step"), scale.Step, $"{item} step");
            Assert.Equal(0, scale.Bandwidth);
            GoldenFixture.Same(item.Numbers("values"), [.. Enumerable.Range(0, scale.Count).Select(scale.Map)], item.ToString());
        }
    }
}
