using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Tests.Formatting;

public sealed class UtcTimeFormatTests
{
    [Fact]
    [Trait("AC", "FORMAT-07")]
    public async Task DatesAreFormattedAsD3Async()
    {
        var dates = (await GoldenFixture.CasesAsync("time", "dates"))[0].Numbers("dates");
        foreach (var item in await GoldenFixture.CasesAsync("time", "formats"))
        {
            var format = UtcTimeFormat.Create(item.Text("specifier"));
            var outputs = item.GetProperty("outputs").EnumerateArray().Select(e => e.GetString()).ToList();
            for (var i = 0; i < dates.Length; i++)
            {
                var actual = format.Format(dates[i]);
                Assert.True(outputs[i] == actual, $"\"{item.Text("specifier")}\" of {dates[i]:R}: expected \"{outputs[i]}\", got \"{actual}\"");
                var span = new char[format.MaxLength];
                Assert.True(format.TryFormat(dates[i], span, out var written));
                Assert.Equal(outputs[i], new string(span, 0, written));
                Assert.False(outputs[i]!.Length > 0 && format.TryFormat(dates[i], new char[outputs[i]!.Length - 1], out _), "too short");
            }
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-08")]
    public async Task MultiScaleTickLabelsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("time", "multiScale"))
        {
            Assert.True(item.Text("text") == UtcTimeFormat.MultiScale(item.Number("date")), $"{item}: got {UtcTimeFormat.MultiScale(item.Number("date"))}");
            var span = new char[32];
            Assert.True(UtcTimeFormat.TryMultiScale(item.Number("date"), span, out var written));
            Assert.Equal(item.Text("text"), new string(span, 0, written));
        }
    }
}
