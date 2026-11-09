using System.Text;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Tests.Formatting;

public sealed class JsNumberTests
{
    [Fact]
    [Trait("AC", "NUMBER-01")]
    public async Task ShortestTextMatchesJavaScriptStringAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("numbers", "shortest"))
        {
            var value = item.Number("value");
            var bytes = new byte[JsNumber.MaxShortestLength];
            var written = JsNumber.WriteShortest(value, bytes);

            Assert.Equal(item.Text("text"), JsNumber.ToShortestString(value));
            Assert.Equal(item.Text("text"), Encoding.UTF8.GetString(bytes, 0, written));
        }
    }

    [Fact]
    [Trait("AC", "NUMBER-02")]
    public async Task FixedTextMatchesToFixedAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("numbers", "fixed"))
        {
            var digits = (int)item.Number("digits");
            Assert.True(item.Text("text") == JsNumber.ToFixed(item.Number("value"), digits), $"{item}: got {JsNumber.ToFixed(item.Number("value"), digits)}");
        }
    }

    [Fact]
    [Trait("AC", "NUMBER-03")]
    public async Task ExponentialTextMatchesToExponentialAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("numbers", "exponential"))
        {
            var digits = item.OptionalNumber("digits") is { } d ? (int?)d : null;
            var actual = JsNumber.ToExponential(item.Number("value"), digits);
            Assert.True(item.Text("text") == actual, $"{item}: got {actual}");
        }
    }

    [Fact]
    [Trait("AC", "NUMBER-04")]
    public async Task PrecisionTextMatchesToPrecisionAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("numbers", "precision"))
        {
            var actual = JsNumber.ToPrecision(item.Number("value"), (int)item.Number("digits"));
            Assert.True(item.Text("text") == actual, $"{item}: got {actual}");
        }
    }

    [Fact]
    [Trait("AC", "NUMBER-05")]
    public async Task RoundMatchesMathRoundAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("numbers", "round"))
        {
            GoldenFixture.Same(item.Number("rounded"), JsNumber.Round(item.Number("value")), item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "NUMBER-06")]
    public async Task RoundedTextMatchesPathRoundAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("numbers", "rounded"))
        {
            var bytes = new byte[JsNumber.MaxShortestLength];
            var written = JsNumber.WriteRounded(item.Number("value"), (int)item.Number("digits"), bytes);
            var actual = Encoding.UTF8.GetString(bytes, 0, written);
            Assert.True(item.Text("text") == actual, $"{item}: got {actual}");
        }
    }
}
