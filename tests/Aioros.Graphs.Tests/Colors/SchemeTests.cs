using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Tests.Colors;

public sealed class SchemeTests
{
    [Fact]
    [Trait("AC", "CHROMA-01")]
    public async Task CategoricalSchemesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("chromatic", "categorical"))
        {
            var expected = item.GetProperty("colors").EnumerateArray().Select(e => e.GetString()!).ToList();
            var actual = item.Text("name") switch
            {
                "Category10" => ColorSchemes.Category10,
                "Accent" => ColorSchemes.Accent,
                "Dark2" => ColorSchemes.Dark2,
                "Observable10" => ColorSchemes.Observable10,
                "Paired" => ColorSchemes.Paired,
                "Pastel1" => ColorSchemes.Pastel1,
                "Pastel2" => ColorSchemes.Pastel2,
                "Set1" => ColorSchemes.Set1,
                "Set2" => ColorSchemes.Set2,
                "Set3" => ColorSchemes.Set3,
                _ => ColorSchemes.Tableau10,
            };
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    [Trait("AC", "CHROMA-02")]
    public async Task BrewerSchemesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("chromatic", "brewer"))
        {
            var name = Enum.Parse<BrewerScheme>(item.Text("name"));
            var sizes = item.GetProperty("sizes").EnumerateArray().ToList();
            for (var k = 0; k < sizes.Count; k++)
            {
                Assert.Equal(sizes[k].EnumerateArray().Select(e => e.GetString()!).ToList(), ColorSchemes.Scheme(name, k));
            }
        }
    }

    [Fact]
    [Trait("AC", "CHROMA-03")]
    public async Task SchemeInterpolatorsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("chromatic", "interpolators"))
        {
            var name = item.Text("name");
            var interpolator = Interpolator(name);
            var ts = item.Numbers("ts");
            var outputs = item.GetProperty("outputs").EnumerateArray().Select(e => e.GetString()!).ToList();
            for (var i = 0; i < ts.Length; i++)
            {
                var text = interpolator.Format(ts[i]);
                Assert.True(outputs[i] == text, $"{name}({ts[i]:R}): expected {outputs[i]}, got {text}");
                var color = interpolator.Interpolate(ts[i]);
                Assert.True(outputs[i] == (outputs[i][0] == '#' ? color.FormatHex() : color.FormatRgb()), $"{name}({ts[i]:R}) as a color");
            }
        }
    }

    private static SchemeInterpolator Interpolator(string name) => name switch
    {
        "Viridis" => ColorSchemes.Viridis,
        "Magma" => ColorSchemes.Magma,
        "Inferno" => ColorSchemes.Inferno,
        "Plasma" => ColorSchemes.Plasma,
        "Cividis" => ColorSchemes.Cividis,
        "Turbo" => ColorSchemes.Turbo,
        "Warm" => ColorSchemes.Warm,
        "Cool" => ColorSchemes.Cool,
        "Rainbow" => ColorSchemes.Rainbow,
        "Sinebow" => ColorSchemes.Sinebow,
        "CubehelixDefault" => ColorSchemes.CubehelixDefault,
        _ => ColorSchemes.Interpolator(Enum.Parse<BrewerScheme>(name)),
    };
}
