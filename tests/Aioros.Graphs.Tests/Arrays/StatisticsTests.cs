using System.Text.Json;
using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Tests.Arrays;

public sealed class StatisticsTests
{
    [Fact]
    [Trait("AC", "ARRAY-05")]
    public async Task QuantilesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "quantile"))
        {
            var values = item.Numbers("values");
            var p = item.Number("p");
            var sorted = values.Where(v => !double.IsNaN(v)).Order().ToArray();

            GoldenFixture.Same(item.Number("quantile"), Quantiles.Of(values, p), item.ToString());
            GoldenFixture.Same(item.Number("quantileSorted"), Quantiles.OfSorted(sorted, p), item.ToString());
            Assert.True(ExpectedIndex(item) == Quantiles.IndexOf(values, p), item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-06")]
    public async Task SummaryStatisticsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "statistics"))
        {
            var values = item.Numbers("values");
            var because = item.ToString();

            GoldenFixture.Same(item.Number("sum"), Statistics.Sum(values), because);
            GoldenFixture.Same(item.Number("mean"), Statistics.Mean(values), because);
            GoldenFixture.Same(item.Number("median"), Statistics.Median(values), because);
            GoldenFixture.Same(item.Number("min"), Statistics.Min(values), because);
            GoldenFixture.Same(item.Number("max"), Statistics.Max(values), because);
            Assert.Equal((int)item.Number("minIndex"), Statistics.MinIndex(values));
            Assert.Equal((int)item.Number("maxIndex"), Statistics.MaxIndex(values));
            GoldenFixture.Same(item.Number("variance"), Statistics.Variance(values), because);
            GoldenFixture.Same(item.Number("deviation"), Statistics.Deviation(values), because);
            GoldenFixture.Same(item.Number("fsum"), Statistics.FSum(values), because);
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-07")]
    public async Task CumulativeSumsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "statistics"))
        {
            var values = item.Numbers("values");
            var sums = new double[values.Length];

            Statistics.CumulativeSum(values, sums);

            GoldenFixture.Same(item.Numbers("cumsum"), sums, item.ToString());
        }

        Assert.Throws<ArgumentException>(() => Statistics.CumulativeSum([1, 2], new double[1]));
    }

    [Fact]
    [Trait("AC", "ARRAY-08")]
    public async Task ThresholdRulesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "statistics"))
        {
            var values = item.Numbers("values");
            var (min, max) = Extent.Of(values);

            GoldenFixture.Same(item.Number("sturges"), ThresholdRule.Sturges.Count(values, min, max), item.ToString());
            GoldenFixture.Same(item.Number("scott"), ThresholdRule.Scott.Count(values, min, max), item.ToString());
            GoldenFixture.Same(item.Number("freedmanDiaconis"), ThresholdRule.FreedmanDiaconis.Count(values, min, max), item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-12")]
    public async Task RangesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "range"))
        {
            GoldenFixture.Same(item.Numbers("values"), Sequence.Range(item.Number("start"), item.Number("stop"), item.Number("step")), item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-13")]
    public async Task NiceBoundsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "nice"))
        {
            var (start, stop) = Ticks.Nice(item.Number("start"), item.Number("stop"), item.Number("count"));

            GoldenFixture.Same(item.Numbers("nice"), [start, stop], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-16")]
    public async Task KeyBisectorsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "bisector"))
        {
            var points = item.Numbers("keys").Select(x => new Point(x)).ToArray();
            var x = item.Number("x");

            Assert.Equal((int)item.Number("left"), Bisector.Left(points, x, default(PointKey)));
            Assert.Equal((int)item.Number("right"), Bisector.Right(points, x, default(PointKey)));
            Assert.Equal((int)item.Number("center"), Bisector.Center(points, x, default(PointKey)));
        }
    }

    private static int ExpectedIndex(JsonElement item)
    {
        var index = (int)item.Number("quantileIndex");
        return index == -2 ? -1 : index;
    }

    private readonly record struct Point(double X);

    private readonly struct PointKey : IKeySelector<Point>
    {
        public double Key(Point item) => item.X;
    }
}
