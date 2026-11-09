using Aioros.Graphs.Formatting;
using Aioros.Graphs.Tests.Time;

namespace Aioros.Graphs.Tests.Formatting;

public sealed class TimeFormatTests
{
    [Fact]
    [Trait("AC", "FORMAT-09")]
    public async Task LocalDatesAreFormattedAsD3Async()
    {
        var dates = (await GoldenFixture.CasesAsync("localtime", "fields")).ToList();
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "formats"))
        {
            var zone = LocalTimeTests.Zone(item);
            var format = TimeFormat.Create(item.Text("specifier"), zone);
            var zoneDates = dates.Where(d => d.Text("zone") == item.Text("zone")).Select(d => d.Number("date")).ToList();
            var outputs = item.GetProperty("outputs").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.Equal(zoneDates.Count, outputs.Count);
            for (var i = 0; i < outputs.Count; i++)
            {
                var actual = format.Format(zoneDates[i]);
                Assert.True(outputs[i] == actual, $"{item.Text("zone")} \"{item.Text("specifier")}\" of {zoneDates[i]:R}: expected \"{outputs[i]}\", got \"{actual}\"");
                var span = new char[format.MaxLength];
                Assert.True(format.TryFormat(zoneDates[i], span, out var written));
                Assert.Equal(outputs[i], new string(span, 0, written));
            }
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-10")]
    public async Task LocalMultiScaleTickLabelsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "multiScale"))
        {
            var zone = LocalTimeTests.Zone(item);
            var actual = TimeFormat.MultiScale(item.Number("date"), zone);
            Assert.True(item.Text("text") == actual, $"{item}: got {actual}");
            var span = new char[32];
            Assert.True(TimeFormat.TryMultiScale(item.Number("date"), span, out var written, zone));
            Assert.Equal(item.Text("text"), new string(span, 0, written));
        }
    }
}
