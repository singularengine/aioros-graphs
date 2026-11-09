using System.Buffers;
using Aioros.Graphs.Chords;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Svg;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class ChordBenchmarks
{
    private const int Groups = 10;

    private static readonly ChordOptions Options = new() { PadAngle = 0.05, SortSubgroups = ChordSort.Descending };

    private readonly ArrayBufferWriter<byte> buffer = new(1 << 16);
    private readonly double[] matrix = new double[Groups * Groups];
    private ChordLayout layout = null!;

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < Groups; i++)
        {
            for (var j = 0; j < Groups; j++)
            {
                matrix[i * Groups + j] = (i * 7 + j * 13) % 17 + 1;
            }
        }

        layout = ChordLayout.Compute(matrix, Groups, Options);
    }

    [Benchmark]
    public int Chord10x10() => ChordLayout.Compute(matrix, Groups, Options).Chords.Length;

    [Benchmark]
    public int Ribbons10x10ToSvg() => Ribbons(null);

    [Benchmark]
    public int Ribbons10x10ToSvgRounded() => Ribbons(3);

    private int Ribbons(int? digits)
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer, digits);
        var context = new PathContext<SvgPathWriter>(ref writer);
        foreach (var chord in layout.Chords)
        {
            Ribbon.Write(ref context, RibbonShape.Of(chord, 200));
        }

        return buffer.WrittenCount;
    }
}
