using System.Text.Json;
using Aioros.Graphs.Colors;
using Aioros.Graphs.Scales;

namespace Aioros.Graphs.Tests.Scales;

public sealed class ColorScaleTests
{
    [Fact]
    [Trait("AC", "SCALE-37")]
    public async Task SequentialAndDivergingScalesMapToD3sSchemeColorsAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-color", "sequential"))
        {
            var interpolator = Scheme(item.Text("scheme"));
            var domain = item.Numbers("domain");
            var scale = item.Text("kind") switch
            {
                "sqrt" => SequentialScale<Rgb, SchemeInterpolator>.CreateSqrt(domain, interpolator),
                "log" => SequentialScale<Rgb, SchemeInterpolator>.CreateLog(domain, interpolator),
                _ => SequentialScale<Rgb, SchemeInterpolator>.Create(domain, interpolator),
            };
            Check(item, interpolator, scale.Position, scale.Map);
        }

        foreach (var item in await GoldenFixture.CasesAsync("scale-color", "diverging"))
        {
            var interpolator = Scheme(item.Text("scheme"));
            var domain = item.Numbers("domain");
            var scale = item.Text("kind") == "symlog"
                ? DivergingScale<Rgb, SchemeInterpolator>.CreateSymlog(domain, interpolator)
                : DivergingScale<Rgb, SchemeInterpolator>.Create(domain, interpolator);
            Check(item, interpolator, scale.Position, scale.Map);
        }
    }

    private static void Check(JsonElement item, SchemeInterpolator interpolator, Func<double, double> position, Func<double, Rgb> map)
    {
        var inputs = item.Numbers("inputs");
        var outputs = item.GetProperty("outputs").EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Null ? null : e.GetString()).ToList();
        for (var i = 0; i < inputs.Length; i++)
        {
            var x = inputs[i];
            if (outputs[i] is null)
            {
                Assert.True(double.IsNaN(position(x)) && (!double.IsNaN(x) || map(x) == default), $"{item.Text("scheme")} {item.Text("kind")} {x}: d3 returned undefined");
                continue;
            }

            var t = position(x);
            Assert.True(outputs[i] == interpolator.Format(t), $"{item.Text("scheme")} {item.Text("kind")} [{string.Join(", ", item.Numbers("domain"))}] {x}: expected {outputs[i]}, got {interpolator.Format(t)}");
            Assert.Equal(interpolator.Interpolate(t), map(x));
        }
    }

    private static SchemeInterpolator Scheme(string name) => name switch
    {
        "viridis" => ColorSchemes.Viridis,
        "plasma" => ColorSchemes.Plasma,
        "turbo" => ColorSchemes.Turbo,
        _ => ColorSchemes.Interpolator(Enum.Parse<BrewerScheme>(name)),
    };
}
