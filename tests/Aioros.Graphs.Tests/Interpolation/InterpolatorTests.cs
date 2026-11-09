using System.Text.Json;
using Aioros.Graphs.Colors;
using Aioros.Graphs.Interpolation;

namespace Aioros.Graphs.Tests.Interpolation;

public sealed class InterpolatorTests
{
    [Fact]
    [Trait("AC", "INTERP-01")]
    public async Task NumbersAndRoundedNumbersMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "number"))
        {
            var (a, b) = (item.Number("a"), item.Number("b"));
            var ts = item.Numbers("ts");
            var number = new NumberInterpolator(a, b);
            var round = new RoundInterpolator(a, b);
            GoldenFixture.Same(item.Numbers("values"), [.. ts.Select(number.Interpolate)], item.ToString());
            GoldenFixture.Same(item.Numbers("rounded"), [.. ts.Select(round.Interpolate)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "INTERP-02")]
    public async Task HuesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "hue"))
        {
            var hue = new HueInterpolator(item.Number("a"), item.Number("b"));
            GoldenFixture.Same(item.Numbers("values"), [.. item.Numbers("ts").Select(hue.Interpolate)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "INTERP-03")]
    public async Task BasisSplinesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "basis"))
        {
            var values = item.Numbers("values");
            var ts = item.Numbers("ts");
            var open = new BasisInterpolator(values);
            var closed = BasisInterpolator.Closed(values);
            GoldenFixture.Same(item.Numbers("open"), [.. ts.Select(open.Interpolate)], item.ToString());
            GoldenFixture.Same(item.Numbers("closed"), [.. ts.Select(closed.Interpolate)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "INTERP-04")]
    public async Task DiscreteValuesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "discrete"))
        {
            var discrete = new DiscreteInterpolator<double>(item.Numbers("values"));
            GoldenFixture.Same(item.Numbers("outputs"), [.. item.Numbers("ts").Select(discrete.Interpolate)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "INTERP-05")]
    public async Task NumberArraysMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "numberArray"))
        {
            var interpolator = new NumberArrayInterpolator(item.Numbers("a"), item.Numbers("b"));
            var ts = item.Numbers("ts");
            var values = item.GetProperty("values").EnumerateArray().ToList();
            for (var i = 0; i < ts.Length; i++)
            {
                GoldenFixture.Same(values[i].Numbers(), interpolator.Interpolate(ts[i]).ToArray(), $"{item} at {ts[i]}");
                var destination = new double[values[i].GetArrayLength()];
                interpolator.Interpolate(ts[i], destination);
                GoldenFixture.Same(values[i].Numbers(), destination, $"{item} at {ts[i]} into a span");
            }
        }
    }

    [Fact]
    [Trait("AC", "INTERP-06")]
    public async Task ColorsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "colors"))
        {
            var a = Color.Parse(item.Text("a"));
            var b = Color.Parse(item.Text("b"));
            var ts = item.Numbers("ts");
            var outputs = item.GetProperty("outputs");
            var because = $"{item.Text("a")} to {item.Text("b")}";
            AssertColors(outputs, "rgb", ts, new RgbInterpolator(a, b), because);
            AssertColors(outputs, "rgbGamma", ts, new RgbInterpolator(a, b, 2.2), because);
            AssertColors(outputs, "hsl", ts, new HslInterpolator(a, b), because);
            AssertColors(outputs, "hslLong", ts, HslInterpolator.LongArc(a, b), because);
            AssertColors(outputs, "lab", ts, new LabInterpolator(a, b), because);
            AssertColors(outputs, "hcl", ts, new HclInterpolator(a, b), because);
            AssertColors(outputs, "hclLong", ts, HclInterpolator.LongArc(a, b), because);
            AssertColors(outputs, "cubehelix", ts, new CubehelixInterpolator(a, b), because);
            AssertColors(outputs, "cubehelixLong", ts, CubehelixInterpolator.LongArc(a, b), because);
            AssertColors(outputs, "cubehelixGamma", ts, new CubehelixInterpolator(a, b, 3), because);
            AssertColors(outputs, "cubehelixLongGamma", ts, CubehelixInterpolator.LongArc(a, b, 3), because);
        }
    }

    [Fact]
    [Trait("AC", "INTERP-07")]
    public async Task RgbSplinesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "rgbBasis"))
        {
            Color[] colors = [.. item.GetProperty("colors").EnumerateArray().Select(e => Color.Parse(e.GetString()))];
            var ts = item.Numbers("ts");
            AssertColors(item, "open", ts, new RgbBasisInterpolator(colors), item.ToString());
            AssertColors(item, "closed", ts, RgbBasisInterpolator.Closed(colors), item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "INTERP-08")]
    public async Task PiecewiseInterpolationMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "piecewise"))
        {
            var ts = item.Numbers("ts");
            if (item.TryGetProperty("numbers", out _))
            {
                var piecewise = Interpolators.Piecewise(item.Numbers("numbers"));
                GoldenFixture.Same(item.Numbers("values"), [.. ts.Select(piecewise.Interpolate)], item.ToString());
            }
            else
            {
                Color[] colors = [.. item.GetProperty("colors").EnumerateArray().Select(e => Color.Parse(e.GetString()))];
                AssertColors(item, "outputs", ts, Interpolators.PiecewiseRgb(colors), item.ToString());
            }
        }
    }

    [Fact]
    [Trait("AC", "INTERP-09")]
    public async Task QuantizedSamplesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "quantize"))
        {
            var n = item.GetProperty("n").GetInt32();
            var numbers = new double[n];
            Interpolators.Quantize(new NumberInterpolator(-2, 8), numbers.AsSpan());
            GoldenFixture.Same(item.Numbers("numbers"), numbers, item.ToString());
            var colors = Interpolators.Quantize(new RgbInterpolator(Color.Parse("red"), Color.Parse("blue")), n);
            Assert.Equal(item.GetProperty("colors").EnumerateArray().Select(e => e.GetString()), colors.Select(c => c.FormatRgb()));
        }
    }

    [Fact]
    [Trait("AC", "INTERP-10")]
    public async Task ZoomMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "zoom"))
        {
            var p0 = item.Numbers("p0");
            var p1 = item.Numbers("p1");
            var start = new ZoomView(p0[0], p0[1], p0[2]);
            var end = new ZoomView(p1[0], p1[1], p1[2]);
            var zoom = item.GetProperty("rho").ValueKind == JsonValueKind.Null ? new ZoomInterpolator(start, end) : new ZoomInterpolator(start, end, item.Number("rho"));
            Near(item.Number("duration"), zoom.Duration, $"{item} duration");
            var ts = item.Numbers("ts");
            var values = item.GetProperty("values").EnumerateArray().ToList();
            for (var i = 0; i < ts.Length; i++)
            {
                var expected = values[i].Numbers();
                var view = zoom.Interpolate(ts[i]);
                Near(expected[0], view.X, $"{item} x at {ts[i]}");
                Near(expected[1], view.Y, $"{item} y at {ts[i]}");
                Near(expected[2], view.Width, $"{item} width at {ts[i]}");
            }
        }
    }

    [Fact]
    [Trait("AC", "INTERP-11")]
    public void ColorInterpolationDoesNotAllocate()
    {
        var a = Color.Parse("steelblue");
        var b = Color.Parse("hsl(30, 80%, 40%)");
        var rgb = new RgbInterpolator(a, b, 2.2);
        var hsl = new HslInterpolator(a, b);
        var lab = new LabInterpolator(a, b);
        var hcl = HclInterpolator.LongArc(a, b);
        var cubehelix = new CubehelixInterpolator(a, b, 3);
        var viridis = ColorSchemes.Viridis;
        var rdBu = ColorSchemes.Interpolator(BrewerScheme.RdBu);
        Span<char> chars = stackalloc char[64];
        Span<byte> bytes = stackalloc byte[64];
        var total = Run(chars, bytes, rgb, hsl, lab, hcl, cubehelix, viridis, rdBu);
        var before = GC.GetAllocatedBytesForCurrentThread();
        total += Run(chars, bytes, rgb, hsl, lab, hcl, cubehelix, viridis, rdBu);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(total > 0);
        Assert.Equal(0, allocated);
    }

    private static int Run(Span<char> chars, Span<byte> bytes, RgbInterpolator rgb, HslInterpolator hsl, LabInterpolator lab, HclInterpolator hcl, CubehelixInterpolator cubehelix, SchemeInterpolator viridis, SchemeInterpolator rdBu)
    {
        var total = 0;
        for (var i = 0; i < 1000; i++)
        {
            var t = i / 999.0;
            total += Write(rgb.Interpolate(t), chars, bytes);
            total += Write(hsl.Interpolate(t), chars, bytes);
            total += Write(lab.Interpolate(t), chars, bytes);
            total += Write(hcl.Interpolate(t), chars, bytes);
            total += Write(cubehelix.Interpolate(t), chars, bytes);
            total += viridis.TryFormat(t, chars, out var written) ? written : 0;
            total += rdBu.TryFormat(t, bytes, out written) ? written : 0;
        }

        return total;
    }

    private static int Write(Rgb color, Span<char> chars, Span<byte> bytes)
    {
        color.TryFormatRgb(chars, out var a);
        color.TryFormatHex(bytes, out var b);
        return a + b;
    }

    private static void AssertColors<TInterpolator>(JsonElement outputs, string name, double[] ts, TInterpolator interpolator, string because)
        where TInterpolator : IInterpolator<Rgb>
    {
        var expected = outputs.GetProperty(name).EnumerateArray().Select(e => e.GetString()).ToList();
        for (var i = 0; i < ts.Length; i++)
        {
            var actual = interpolator.Interpolate(ts[i]).FormatRgb();
            Assert.True(expected[i] == actual, $"{name} {because} at {ts[i]:R}: expected {expected[i]}, got {actual}");
        }
    }

    private static void Near(double expected, double actual, string because) =>
        Assert.True(expected.Equals(actual) || Math.Abs(expected - actual) <= 1e-12 * Math.Max(1, Math.Abs(expected)), $"{because}: expected {expected:R}, got {actual:R}");
}
