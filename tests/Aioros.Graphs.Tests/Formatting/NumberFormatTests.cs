using Aioros.Graphs.Arrays;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Tests.Formatting;

public sealed class NumberFormatTests
{
    [Fact]
    [Trait("AC", "FORMAT-01")]
    public async Task SpecifiersAreNormalizedAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("format", "formats"))
        {
            Assert.Equal(item.Text("normalized"), FormatSpecifier.Parse(item.Text("specifier")).ToString());
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-02")]
    public async Task NumbersAreFormattedAsD3Async()
    {
        var values = (await GoldenFixture.CasesAsync("format", "values"))[0].Numbers("values");
        foreach (var item in await GoldenFixture.CasesAsync("format", "formats"))
        {
            var format = NumberFormat.Create(item.Text("specifier"));
            var outputs = item.GetProperty("outputs").EnumerateArray().Select(e => e.GetString()).ToList();
            for (var i = 0; i < values.Length; i++)
            {
                var actual = format.Format(values[i]);
                Assert.True(outputs[i] == actual, $"\"{item.Text("specifier")}\" of {values[i]:R}: expected \"{outputs[i]}\", got \"{actual}\"");
            }
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-03")]
    public async Task InvalidSpecifiersAreRejectedAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("format", "rejected"))
        {
            Assert.False(item.GetProperty("valid").GetBoolean());
            Assert.Throws<FormatException>(() => NumberFormat.Create(item.Text("specifier")));
            Assert.Throws<FormatException>(() => FormatSpecifier.Parse(item.Text("specifier")));
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-04")]
    public async Task PrefixFormatsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("format", "prefix"))
        {
            var format = NumberFormat.CreatePrefix(item.Text("specifier"), item.Number("reference"));
            var values = item.Numbers("values");
            var outputs = item.GetProperty("outputs").EnumerateArray().Select(e => e.GetString()).ToList();
            for (var i = 0; i < values.Length; i++)
            {
                var actual = format.Format(values[i]);
                Assert.True(outputs[i] == actual, $"prefix \"{item.Text("specifier")}\" {item.Number("reference")} of {values[i]:R}: expected \"{outputs[i]}\", got \"{actual}\"");
            }
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-05")]
    public async Task PrecisionsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("format", "precision"))
        {
            var (step, value) = (item.Number("step"), item.Number("value"));

            GoldenFixture.Same(item.Number("fixed"), FormatPrecision.Fixed(step), $"fixed {item}");
            GoldenFixture.Same(item.Number("prefix"), FormatPrecision.Prefix(step, value), $"prefix {item}");
            GoldenFixture.Same(item.Number("round"), FormatPrecision.Round(step, value), $"round {item}");
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-06")]
    public async Task TickLabelsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("format", "ticks"))
        {
            var (start, stop, count) = (item.Number("start"), item.Number("stop"), item.Number("count"));
            var ticks = item.Numbers("ticks");
            GoldenFixture.Same(ticks, Ticks.Compute(start, stop, count), item.ToString());
            var format = TickFormat.Create(start, stop, count, item.OptionalText("specifier"));
            var outputs = item.GetProperty("outputs").EnumerateArray().Select(e => e.GetString()).ToList();
            for (var i = 0; i < ticks.Length; i++)
            {
                var actual = format.Format(ticks[i]);
                Assert.True(outputs[i] == actual, $"{start}..{stop} ({count}, \"{item.OptionalText("specifier")}\") of {ticks[i]:R}: expected \"{outputs[i]}\", got \"{actual}\"");
            }
        }
    }
}
