using System.Text.Json;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Scales;
using Aioros.Graphs.Tests.Time;

namespace Aioros.Graphs.Tests.Formatting;

public sealed class LocaleTests
{
    [Fact]
    [Trait("AC", "FORMAT-11")]
    public async Task LocalesMatchD3DefinitionsAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("locale", "numberLocales"))
        {
            var definition = item.GetProperty("definition");
            var locale = NumberLocale.Get(item.Text("name"));
            var because = item.Text("name");
            Assert.Equal(Optional(definition, "decimal") ?? ".", locale.DecimalMark);
            Assert.Equal(Optional(definition, "thousands"), locale.Thousands);
            Assert.Equal(definition.GetProperty("grouping").EnumerateArray().Select(g => g.GetInt32()), locale.Grouping);
            var currency = definition.GetProperty("currency").EnumerateArray().Select(c => c.GetString()).ToList();
            Assert.Equal(currency[0], locale.CurrencyPrefix);
            Assert.Equal(currency[1], locale.CurrencySuffix);
            Assert.True(Strings(definition, "numerals")?.SequenceEqual(locale.Numerals!) ?? locale.Numerals is null, because);
            Assert.Equal(Optional(definition, "percent") ?? "%", locale.Percent);
            Assert.Equal(Optional(definition, "minus") ?? "−", locale.Minus);
            Assert.Equal(Optional(definition, "nan") ?? "NaN", locale.Nan);
        }

        foreach (var item in await GoldenFixture.CasesAsync("locale", "timeLocales"))
        {
            var definition = item.GetProperty("definition");
            var locale = TimeLocale.Get(item.Text("name"));
            Assert.Equal(definition.Text("dateTime"), locale.DateTime);
            Assert.Equal(definition.Text("date"), locale.Date);
            Assert.Equal(definition.Text("time"), locale.Time);
            Assert.Equal(Strings(definition, "periods"), locale.Periods);
            Assert.Equal(Strings(definition, "days"), locale.Days);
            Assert.Equal(Strings(definition, "shortDays"), locale.ShortDays);
            Assert.Equal(Strings(definition, "months"), locale.Months);
            Assert.Equal(Strings(definition, "shortMonths"), locale.ShortMonths);
        }

        Assert.False(NumberLocale.TryGet("xx-XX", out _));
        Assert.False(TimeLocale.TryGet("xx-XX", out _));
        Assert.Throws<ArgumentException>(() => NumberLocale.Get("xx-XX"));
        Assert.Throws<ArgumentException>(() => TimeLocale.Get("xx-XX"));
    }

    [Fact]
    [Trait("AC", "FORMAT-12")]
    public async Task NumbersAreFormattedInEachLocaleAsD3Async()
    {
        var values = (await GoldenFixture.CasesAsync("locale", "values"))[0].Numbers("values");
        foreach (var item in await GoldenFixture.CasesAsync("locale", "numberFormats"))
        {
            var format = NumberFormat.Create(item.Text("specifier"), NumberLocale.Get(item.Text("locale")));
            Compare(item, values, format.Format);
        }

        foreach (var item in await GoldenFixture.CasesAsync("locale", "prefixFormats"))
        {
            var format = NumberFormat.CreatePrefix(item.Text("specifier"), item.Number("reference"), NumberLocale.Get(item.Text("locale")));
            Compare(item, values, format.Format);
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-13")]
    public async Task TickFormatsUseTheLocaleAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("locale", "tickFormats"))
        {
            var format = TickFormat.Create(item.Number("start"), item.Number("stop"), item.Number("count"), item.OptionalText("specifier"), NumberLocale.Get(item.Text("locale")));
            Assert.Equal(item.GetProperty("labels").EnumerateArray().Select(l => l.GetString()), item.Numbers("ticks").Select(format.Format));
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-14")]
    public async Task DatesAreFormattedInEachLocaleAsD3Async()
    {
        var dates = (await GoldenFixture.CasesAsync("locale", "dates"))[0].Numbers("dates");
        foreach (var item in await GoldenFixture.CasesAsync("locale", "timeFormats"))
        {
            var format = UtcTimeFormat.Create(item.Text("specifier"), TimeLocale.Get(item.Text("locale")));
            Compare(item, dates, format.Format);
        }

        var fields = await GoldenFixture.CasesAsync("localtime", "fields");
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "localeFormats"))
        {
            var zone = LocalTimeTests.Zone(item);
            var format = TimeFormat.Create(item.Text("specifier"), zone, TimeLocale.Get(item.Text("locale")));
            var zoneDates = fields.Where(d => d.Text("zone") == item.Text("zone")).Select(d => d.Number("date")).ToArray();
            Compare(item, zoneDates, format.Format);
        }
    }

    [Fact]
    [Trait("AC", "FORMAT-15")]
    public async Task MultiScaleLabelsUseTheLocaleAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("locale", "multiScale"))
        {
            var locale = TimeLocale.Get(item.Text("locale"));
            var ticks = item.Numbers("ticks");
            Assert.Equal(item.GetProperty("labels").EnumerateArray().Select(l => l.GetString()), ticks.Select(t => UtcTimeFormat.MultiScale(t, locale)));
            Assert.Equal(item.GetProperty("formatted").EnumerateArray().Select(l => l.GetString()), ticks.Select(UtcScale.TickFormat("%c", locale)));
        }
    }

    private static void Compare(JsonElement item, double[] inputs, Func<double, string> format)
    {
        var outputs = item.GetProperty("outputs").EnumerateArray().Select(e => e.GetString()).ToList();
        for (var i = 0; i < inputs.Length; i++)
        {
            var actual = format(inputs[i]);
            Assert.True(outputs[i] == actual, $"{item.Text("locale")} \"{item.Text("specifier")}\" of {inputs[i]:R}: expected \"{outputs[i]}\", got \"{actual}\"");
        }
    }

    private static string? Optional(JsonElement definition, string name) =>
        definition.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static string[]? Strings(JsonElement definition, string name) =>
        definition.TryGetProperty(name, out var value) ? [.. value.EnumerateArray().Select(e => e.GetString()!)] : null;
}
