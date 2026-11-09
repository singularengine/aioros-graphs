using Aioros.Graphs.Forces;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class ForceBenchmarks
{
    private const int GraphNodes = 77;
    private const int Bodies = 1_000;

    private readonly int[] sources = new int[GraphNodes - 1 + 178];
    private readonly int[] targets = new int[GraphNodes - 1 + 178];
    private ForceSimulation bodies = null!;

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 1; i < GraphNodes; i++)
        {
            sources[i - 1] = (int)(i * 2654435761L % 4294967296L % i);
            targets[i - 1] = i;
        }

        for (var k = 0; k < 178; k++)
        {
            var a = (k * 17 + 3) % GraphNodes;
            var b = (k * 29 + 11) % GraphNodes;
            sources[GraphNodes - 1 + k] = a;
            targets[GraphNodes - 1 + k] = a == b ? (b + 1) % GraphNodes : b;
        }

        bodies = new ForceSimulation(Bodies);
        bodies.Force("charge", new ManyBodyForce());
    }

    [Benchmark]
    public double Graph77Links254Ticks300()
    {
        var simulation = new ForceSimulation(GraphNodes);
        simulation.Force("link", new LinkForce(sources, targets)).Force("charge", new ManyBodyForce()).Force("center", new CenterForce());
        simulation.Tick(300);
        return simulation.X[0];
    }

    [Benchmark]
    public double ManyBody1000Tick()
    {
        bodies.Tick();
        return bodies.X[0];
    }
}
