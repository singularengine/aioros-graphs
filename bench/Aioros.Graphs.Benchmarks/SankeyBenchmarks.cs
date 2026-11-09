using System.Buffers;
using Aioros.Graphs.Sankey;
using Aioros.Graphs.Svg;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class SankeyBenchmarks
{
    private static readonly int[] Layers = [8, 10, 12, 10, 8, 2];

    private static readonly SankeyOptions Options = new() { X0 = 1, Y0 = 5, X1 = 959, Y1 = 495, NodeWidth = 15, NodePadding = 10 };

    private readonly ArrayBufferWriter<byte> buffer = new(1 << 16);
    private int[] sources = [];
    private int[] targets = [];
    private double[] values = [];
    private int nodes;

    [GlobalSetup]
    public void Setup()
    {
        var s = new List<int>();
        var t = new List<int>();
        var v = new List<double>();
        var seen = new HashSet<int>();
        var start = 0;
        for (var l = 0; l < Layers.Length - 1; l++)
        {
            var next = start + Layers[l];
            for (var link = start * 3; link < next * 3; link++)
            {
                var (node, k) = Math.DivRem(link, 3);
                var target = next + (k * 7 + node * 3) % Layers[l + 1];
                if (seen.Add(node * 1000 + target))
                {
                    s.Add(node);
                    t.Add(target);
                    v.Add(1 + (node * 13 + k * 29) % 50);
                }
            }

            start = next;
        }

        nodes = start + Layers[^1];
        sources = [.. s];
        targets = [.. t];
        values = [.. v];
    }

    [Benchmark]
    public int SankeyLayout50()
    {
        var layout = SankeyLayout.Compute(nodes, sources, targets, values, Options);
        return layout.LinkCount;
    }

    [Benchmark]
    public int SankeyLayoutAndLinks50ToSvg()
    {
        var layout = SankeyLayout.Compute(nodes, sources, targets, values, Options);
        buffer.ResetWrittenCount();
        for (var i = 0; i < layout.LinkCount; i++)
        {
            var writer = new SvgPathWriter(buffer, null);
            layout.WriteLink(ref writer, i);
        }

        return buffer.WrittenCount;
    }
}
