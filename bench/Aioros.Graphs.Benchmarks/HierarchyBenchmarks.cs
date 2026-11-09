using Aioros.Graphs.Hierarchies;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class HierarchyBenchmarks
{
    private const int Nodes = 1_000;

    private readonly int[] parents = new int[Nodes];
    private readonly double[] values = new double[Nodes];
    private Hierarchy root = null!;

    [GlobalSetup]
    public void Setup()
    {
        parents[0] = -1;
        for (var i = 1; i < Nodes; i++)
        {
            parents[i] = (int)(unchecked((uint)i * 2654435761u) % (uint)i);
            values[i] = 1 + i * 37 % 100;
        }

        var summed = Hierarchy.FromParents(parents).Sum(values);
        var sums = summed.Values.ToArray();
        root = summed.Sort((a, b) => sums[b] - sums[a]);
    }

    [Benchmark]
    public int StratifySumSort1000()
    {
        var summed = Hierarchy.FromParents(parents).Sum(values);
        var sums = summed.Values.ToArray();
        return summed.Sort((a, b) => sums[b] - sums[a]).Count;
    }

    [Benchmark]
    public double Tree1000() => Tree.Compute(root, new NodeLayoutOptions { Width = 960, Height = 500 }).X[Nodes - 1];

    [Benchmark]
    public double Cluster1000() => Cluster.Compute(root, new NodeLayoutOptions { Width = 960, Height = 500 }).X[Nodes - 1];

    [Benchmark]
    public double TreemapSquarify1000() =>
        Treemap.Compute(root, new TreemapOptions { Width = 960, Height = 500, PaddingInner = 1, Round = true }).X0[Nodes - 1];

    [Benchmark]
    public double TreemapBinary1000() =>
        Treemap.Compute(root, new TreemapOptions { Width = 960, Height = 500, Tiling = TreemapTiling.Binary }).X0[Nodes - 1];

    [Benchmark]
    public double Pack1000() => Pack.Compute(root, new PackOptions { Width = 960, Height = 960, Padding = 2 }).R[Nodes - 1];

    [Benchmark]
    public double Partition1000() => Partition.Compute(root, new PartitionOptions { Width = 2 * Math.PI, Height = 300 }).X1[Nodes - 1];
}
