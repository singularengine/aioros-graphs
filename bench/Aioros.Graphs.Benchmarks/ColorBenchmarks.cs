using Aioros.Graphs.Colors;
using Aioros.Graphs.Interpolation;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class ColorBenchmarks
{
    private const int Samples = 1000;

    private readonly string[] specifiers =
    [
        "steelblue", "#4682b4", "#fff", "rgb(70, 130, 180)", "rgba(70, 130, 180, 0.5)", "hsl(207, 44%, 49%)",
        "rebeccapurple", "#ff000080", "rgb(10%, 20%, 30%)", "transparent", "  Navy ", "hsla(120, 100%, 25%, 0.3)",
    ];

    private readonly byte[] output = new byte[64];
    private readonly RgbInterpolator rgb = new(Color.Parse("steelblue"), Color.Parse("brown"));
    private readonly HclInterpolator hcl = new(Color.Parse("steelblue"), Color.Parse("brown"));
    private readonly SchemeInterpolator viridis = ColorSchemes.Viridis;
    private readonly SchemeInterpolator rdBu = ColorSchemes.Interpolator(BrewerScheme.RdBu);

    [Benchmark]
    public double Parse12()
    {
        var total = 0.0;
        foreach (var specifier in specifiers)
        {
            total += Color.Parse(specifier).Opacity;
        }

        return total;
    }

    [Benchmark]
    public int RgbInterpolate1000ToRgb()
    {
        var total = 0;
        for (var i = 0; i < Samples; i++)
        {
            rgb.Interpolate(i / (Samples - 1.0)).TryFormatRgb(output, out var written);
            total += written;
        }

        return total;
    }

    [Benchmark]
    public int HclInterpolate1000ToRgb()
    {
        var total = 0;
        for (var i = 0; i < Samples; i++)
        {
            hcl.Interpolate(i / (Samples - 1.0)).TryFormatRgb(output, out var written);
            total += written;
        }

        return total;
    }

    [Benchmark]
    public int Viridis1000()
    {
        var total = 0;
        for (var i = 0; i < Samples; i++)
        {
            total += viridis.Format(i / (Samples - 1.0)).Length;
        }

        return total;
    }

    [Benchmark]
    public int RdBu1000()
    {
        var total = 0;
        for (var i = 0; i < Samples; i++)
        {
            rdBu.TryFormat(i / (Samples - 1.0), output, out var written);
            total += written;
        }

        return total;
    }
}
