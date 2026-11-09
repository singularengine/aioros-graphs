using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Tests.Arrays;

public sealed class ArrayTests
{
    [Fact]
    [Trait("AC", "ARRAY-01")]
    public async Task TicksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "ticks"))
        {
            var (start, stop, count) = (item.Number("start"), item.Number("stop"), item.Number("count"));
            var expected = item.Numbers("ticks");
            var span = new double[expected.Length + 4];

            GoldenFixture.Same(expected, Ticks.Compute(start, stop, count), item.ToString());
            Assert.Equal(expected.Length, Ticks.Count(start, stop, count));
            GoldenFixture.Same(expected, span[..Ticks.Compute(start, stop, count, span)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-02")]
    public async Task IncrementsAndStepsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "increments"))
        {
            var (start, stop, count) = (item.Number("start"), item.Number("stop"), item.Number("count"));

            GoldenFixture.Same(item.Number("increment"), Ticks.Increment(start, stop, count), item.ToString());
            GoldenFixture.Same(item.Number("step"), Ticks.Step(start, stop, count), item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-03")]
    public async Task BisectorsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "bisect"))
        {
            var values = item.Numbers("values");
            var x = item.Number("x");

            Assert.Equal((int)item.Number("left"), Bisect.Left(values, x));
            Assert.Equal((int)item.Number("right"), Bisect.Right(values, x));
            Assert.Equal((int)item.Number("center"), Bisect.Center(values, x));
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-04")]
    public async Task ExtentMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "extent"))
        {
            var expected = item.Numbers("extent");
            var (min, max) = Extent.Of(item.Numbers("values"));

            GoldenFixture.Same(expected, [min, max], item.ToString());
        }
    }
}
