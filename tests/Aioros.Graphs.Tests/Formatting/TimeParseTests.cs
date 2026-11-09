using System.Text.Json;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Tests.Time;

namespace Aioros.Graphs.Tests.Formatting;

public sealed class TimeParseTests
{
    [Fact]
    [Trait("AC", "FORMAT-16")]
    public async Task UtcParsingMatchesD3Async()
    {
        var failures = new List<string>();
        foreach (var item in await GoldenFixture.CasesAsync("parse", "utc"))
        {
            var parse = UtcTimeParse.Create(item.Text("specifier"));
            Check(item, parse.TryParse, failures);
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    [Trait("AC", "FORMAT-17")]
    public async Task LocaleParsingMatchesD3Async()
    {
        var failures = new List<string>();
        foreach (var item in await GoldenFixture.CasesAsync("parse", "locales"))
        {
            var parse = UtcTimeParse.Create(item.Text("specifier"), TimeLocale.Get(item.Text("locale")));
            Check(item, parse.TryParse, failures);
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    [Trait("AC", "FORMAT-18")]
    public async Task LocalParsingMatchesD3Async()
    {
        var failures = new List<string>();
        foreach (var item in await GoldenFixture.CasesAsync("parse", "local"))
        {
            var parse = TimeParse.Create(item.Text("specifier"), LocalTimeTests.Zone(item));
            Check(item, parse.TryParse, failures);
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    [Trait("AC", "FORMAT-19")]
    public void RejectedTextsParseToNull()
    {
        Assert.Null(UtcTimeParse.Create("%Y-%m-%d").Parse("2024-02"));
        Assert.Null(TimeParse.Create("%Y-%m-%d", TimeZoneInfo.Utc).Parse("2024-02-29x"));
        Assert.Equal(1709164800000, UtcTimeParse.Create("%Y-%m-%d").Parse("2024-02-29"));
        Assert.True(double.IsNaN(UtcTimeParse.Create("%Q").Parse("8640000000000001")!.Value));
        Assert.Equal("%Y", UtcTimeParse.Create("%Y").Specifier);
        Assert.Throws<ArgumentNullException>(() => UtcTimeParse.Create(null!));
        Assert.Throws<ArgumentNullException>(() => TimeParse.Create(null!));
        Assert.Throws<ArgumentNullException>(() => UtcTimeParse.Create("%Y").Parse(null!));
        Assert.Throws<ArgumentNullException>(() => TimeParse.Create("%Y").Parse(null!));
    }

    private static void Check(JsonElement item, Parser parse, List<string> failures)
    {
        var inputs = item.GetProperty("inputs").EnumerateArray().Select(e => e.GetString()!).ToList();
        var outputs = item.GetProperty("outputs").EnumerateArray().ToList();
        for (var i = 0; i < inputs.Count; i++)
        {
            var parsed = parse(inputs[i], out var actual);
            var expected = outputs[i];
            var ok = expected.ValueKind == JsonValueKind.Null ? !parsed : parsed && expected.Number().Equals(actual);
            if (!ok)
            {
                failures.Add($"{item.OptionalText("zone")} {item.OptionalText("locale")} \"{item.Text("specifier")}\" \"{inputs[i]}\": expected {expected}, got {(parsed ? actual.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "null")}");
            }
        }
    }

    private delegate bool Parser(ReadOnlySpan<char> text, out double milliseconds);
}
