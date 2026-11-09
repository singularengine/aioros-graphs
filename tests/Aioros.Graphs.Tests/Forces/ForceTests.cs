using System.Text.Json;
using Aioros.Graphs.Forces;

namespace Aioros.Graphs.Tests.Forces;

public sealed class ForceTests
{
    [Fact]
    [Trait("AC", "FORCE-01")]
    public async Task NodesStartWhereD3PlacesThemAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("force", "initial"))
        {
            var simulation = Create(item);
            var nodes = item.GetProperty("nodes").EnumerateArray().ToList();
            Assert.Equal(nodes.Count, simulation.Count);
            for (var i = 0; i < nodes.Count; i++)
            {
                GoldenFixture.Same(nodes[i].Number("x"), simulation.X[i], $"x[{i}] of {item}");
                GoldenFixture.Same(nodes[i].Number("y"), simulation.Y[i], $"y[{i}] of {item}");
                GoldenFixture.Same(nodes[i].Number("vx"), simulation.VelocityX[i], $"vx[{i}] of {item}");
                GoldenFixture.Same(nodes[i].Number("vy"), simulation.VelocityY[i], $"vy[{i}] of {item}");
            }

            if (item.TryGetProperty("alpha", out _))
            {
                GoldenFixture.Same(item.Number("alpha"), simulation.Alpha, "alpha");
                GoldenFixture.Same(item.Number("alphaMin"), simulation.AlphaMin, "alphaMin");
                GoldenFixture.Same(item.Number("alphaDecay"), simulation.AlphaDecay, "alphaDecay");
                GoldenFixture.Same(item.Number("velocityDecay"), simulation.VelocityDecay, "velocityDecay");
            }
        }
    }

    [Fact]
    [Trait("AC", "FORCE-02")]
    public Task LinkForceMatchesD3Async() => SimulateAsync("link");

    [Fact]
    [Trait("AC", "FORCE-03")]
    public void MismatchedSpansAndUnknownEndpointsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new ForceSimulation([1, 2], [1]));
        Assert.Throws<ArgumentException>(() => new ForceSimulation([1, 2], [1, 2], [1], []));
        Assert.Throws<ArgumentException>(() => new ForceSimulation([1, 2], [1, 2], [], [1, 2, 3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ForceSimulation(-1));
        Assert.Throws<ArgumentException>(() => new LinkForce([0, 1], [1]));
        var simulation = new ForceSimulation(3);
        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.Force("link", new LinkForce([0, 1], [1, 3])));
        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.Force("link", new LinkForce([-1], [1])));
        Assert.Throws<ArgumentException>(() => simulation.Force("link", new LinkForce([0], [1]) { Distances = [1, 2] }));
        Assert.Throws<ArgumentException>(() => simulation.Force("link", new LinkForce([0], [1]) { Strengths = [] }));
        Assert.Throws<ArgumentException>(() => simulation.Force("charge", new ManyBodyForce { Strengths = [1, 2] }));
        Assert.Throws<ArgumentException>(() => simulation.Force("collide", new CollideForce { Radii = [1, 2, 3, 4] }));
        Assert.Throws<ArgumentException>(() => simulation.Force("x", new XForce { Targets = [1] }));
        Assert.Throws<ArgumentException>(() => simulation.Force("y", new YForce { Strengths = [1] }));
    }

    [Fact]
    [Trait("AC", "FORCE-04")]
    public Task ManyBodyForceMatchesD3Async() => SimulateAsync("manyBody");

    [Fact]
    [Trait("AC", "FORCE-05")]
    public Task CenterForceMatchesD3Async() => SimulateAsync("center");

    [Fact]
    [Trait("AC", "FORCE-06")]
    public Task CollideForceMatchesD3Async() => SimulateAsync("collide");

    [Fact]
    [Trait("AC", "FORCE-07")]
    public Task PositionForcesMatchD3Async() => SimulateAsync("position");

    [Fact]
    [Trait("AC", "FORCE-08")]
    public Task CombinedSimulationsMatchD3Async() => SimulateAsync("combined");

    [Fact]
    [Trait("AC", "FORCE-09")]
    public Task NamedForcesAreReplacedInPlaceAndRemovedAsync() => SimulateAsync("named");

    [Fact]
    [Trait("AC", "FORCE-10")]
    public async Task RunTicksUntilAlphaFallsBelowItsMinimumAsync()
    {
        var (sources, targets) = await MiserablesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("force", "run"))
        {
            var simulation = Miserables(item.GetProperty("settings"));
            simulation.Force("link", new LinkForce(sources, targets)).Force("charge", new ManyBodyForce()).Force("center", new CenterForce());

            var ticks = simulation.Run();

            Assert.Equal((int)item.Number("ticks"), ticks);
            Check(item.GetProperty("state"), simulation, item.ToString());
        }

        var endless = new ForceSimulation(10) { AlphaTarget = 0.3 };
        endless.Force("charge", new ManyBodyForce());
        Assert.Equal(50, endless.Run(50));
    }

    [Fact]
    [Trait("AC", "FORCE-11")]
    public async Task FindReturnsD3sClosestNodeAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("force", "find"))
        {
            var simulation = new ForceSimulation(item.Numbers("x"), item.Numbers("y"));
            foreach (var search in item.GetProperty("cases").EnumerateArray())
            {
                var radius = search.OptionalNumber("radius") ?? double.PositiveInfinity;
                var found = simulation.Find(search.Number("x"), search.Number("y"), radius);
                Assert.True((int)search.Number("index") == found, $"{search}: got {found}");
            }
        }
    }

    [Fact]
    [Trait("AC", "FORCE-12")]
    public async Task TicksAllocateNothingAsync()
    {
        var (sources, targets) = await MiserablesAsync();
        var simulation = Miserables(default);
        simulation.Force("link", new LinkForce(sources, targets)).Force("charge", new ManyBodyForce())
            .Force("center", new CenterForce()).Force("collide", new CollideForce { Radius = 6 });
        simulation.Tick();

        var before = GC.GetAllocatedBytesForCurrentThread();
        simulation.Tick(100);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    private static async Task<(int[] Sources, int[] Targets)> MiserablesAsync()
    {
        var combined = (await GoldenFixture.CasesAsync("force", "combined"))[0];
        var links = combined.GetProperty("links").EnumerateArray().ToList();
        return ([.. links.Select(l => (int)l[0].GetDouble())], [.. links.Select(l => (int)l[1].GetDouble())]);
    }

    private static ForceSimulation Miserables(JsonElement settings)
    {
        var simulation = new ForceSimulation(77);
        if (settings.ValueKind == JsonValueKind.Object)
        {
            simulation.AlphaDecay = settings.OptionalNumber("alphaDecay") ?? simulation.AlphaDecay;
            simulation.AlphaMin = settings.OptionalNumber("alphaMin") ?? simulation.AlphaMin;
        }

        return simulation;
    }

    private static async Task SimulateAsync(string group)
    {
        foreach (var item in await GoldenFixture.CasesAsync("force", group))
        {
            var simulation = Create(item);
            var settings = item.GetProperty("settings");
            if (settings.OptionalNumber("alpha") is { } alpha)
            {
                simulation.Alpha = alpha;
            }

            simulation.AlphaMin = settings.OptionalNumber("alphaMin") ?? simulation.AlphaMin;
            simulation.AlphaDecay = settings.OptionalNumber("alphaDecay") ?? simulation.AlphaDecay;
            simulation.AlphaTarget = settings.OptionalNumber("alphaTarget") ?? simulation.AlphaTarget;
            if (settings.OptionalNumber("velocityDecay") is { } decay)
            {
                simulation.VelocityDecay = decay;
            }

            var links = item.GetProperty("links").EnumerateArray().Select(l => ((int)l[0].GetDouble(), (int)l[1].GetDouble())).ToList();
            foreach (var step in item.GetProperty("steps").EnumerateArray())
            {
                var spec = step.GetProperty("force");
                simulation.Force(step.Text("name"), spec.ValueKind == JsonValueKind.Null ? null : Build(spec, links));
            }

            var done = 0;
            foreach (var state in item.GetProperty("states").EnumerateArray())
            {
                var ticks = (int)state.Number("ticks");
                simulation.Tick(ticks - done);
                done = ticks;
                Check(state, simulation, $"{group} {item.Text("graph")} {item.GetProperty("steps")} {settings} after {ticks}");
            }
        }
    }

    private static void Check(JsonElement state, ForceSimulation simulation, string because)
    {
        GoldenFixture.Same(state.Number("alpha"), simulation.Alpha, $"alpha, {because}");
        GoldenFixture.Same(state.Numbers("x"), simulation.X.ToArray(), $"x, {because}");
        GoldenFixture.Same(state.Numbers("y"), simulation.Y.ToArray(), $"y, {because}");
        GoldenFixture.Same(state.Numbers("vx"), simulation.VelocityX.ToArray(), $"vx, {because}");
        GoldenFixture.Same(state.Numbers("vy"), simulation.VelocityY.ToArray(), $"vy, {because}");
    }

    private static ForceSimulation Create(JsonElement item)
    {
        var n = (int)item.Number("n");
        if (item.GetProperty("x").ValueKind == JsonValueKind.Null && item.GetProperty("fx").ValueKind == JsonValueKind.Null)
        {
            return new ForceSimulation(n);
        }

        var x = item.GetProperty("x").ValueKind == JsonValueKind.Null ? Enumerable.Repeat(double.NaN, n).ToArray() : item.Numbers("x");
        var y = item.GetProperty("y").ValueKind == JsonValueKind.Null ? Enumerable.Repeat(double.NaN, n).ToArray() : item.Numbers("y");
        var fx = item.GetProperty("fx").ValueKind == JsonValueKind.Null ? [] : item.Numbers("fx");
        var fy = item.GetProperty("fy").ValueKind == JsonValueKind.Null ? [] : item.Numbers("fy");
        return new ForceSimulation(x, y, fx, fy);
    }

    private static IForce Build(JsonElement spec, List<(int Source, int Target)> links) => spec.Text("type") switch
    {
        "link" => new LinkForce([.. links.Select(l => l.Source)], [.. links.Select(l => l.Target)])
        {
            Distance = Scalar(spec, "distance") ?? 30,
            Distances = List(spec, "distance"),
            Strength = Scalar(spec, "strength"),
            Strengths = List(spec, "strength"),
            Iterations = (int)(spec.OptionalNumber("iterations") ?? 1),
        },
        "manyBody" => ManyBody(spec),
        "center" => new CenterForce { X = spec.Number("x"), Y = spec.Number("y"), Strength = spec.OptionalNumber("strength") ?? 1 },
        "collide" => new CollideForce
        {
            Radius = Scalar(spec, "radius") ?? 1,
            Radii = List(spec, "radius"),
            Strength = spec.OptionalNumber("strength") ?? 1,
            Iterations = (int)(spec.OptionalNumber("iterations") ?? 1),
        },
        "x" => new XForce { X = Scalar(spec, "target") ?? 0, Targets = List(spec, "target"), Strength = Scalar(spec, "strength") ?? 0.1, Strengths = List(spec, "strength") },
        "y" => new YForce { Y = Scalar(spec, "target") ?? 0, Targets = List(spec, "target"), Strength = Scalar(spec, "strength") ?? 0.1, Strengths = List(spec, "strength") },
        var type => throw new InvalidOperationException(type),
    };

    private static ManyBodyForce ManyBody(JsonElement spec)
    {
        var force = new ManyBodyForce { Strength = Scalar(spec, "strength") ?? -30, Strengths = List(spec, "strength") };
        if (spec.OptionalNumber("theta") is { } theta)
        {
            force = new ManyBodyForce { Strength = force.Strength, Strengths = force.Strengths, Theta = theta };
        }

        if (spec.OptionalNumber("distanceMin") is { } min)
        {
            force = new ManyBodyForce { Strength = force.Strength, Strengths = force.Strengths, DistanceMin = min, DistanceMax = spec.OptionalNumber("distanceMax") ?? double.PositiveInfinity };
        }

        return force;
    }

    private static double? Scalar(JsonElement spec, string property) =>
        spec.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.Number or JsonValueKind.String ? value.Number() : null;

    private static double[]? List(JsonElement spec, string property) =>
        spec.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array ? value.Numbers() : null;
}
