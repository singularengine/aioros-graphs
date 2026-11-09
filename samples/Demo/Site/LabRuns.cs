using System.Text.Json;

namespace Aioros.Graphs.Demo.Site;

internal sealed record LabChart(string Id, double EngineNs, double D3Ns);

internal sealed record LabDemo(string Name, string Commit, string Machine, string EngineRuntime, string D3Runtime, double[] EngineLoad, double[] D3Load, LabChart[] Charts)
{
    public LabChart? Chart(string id) => Charts.FirstOrDefault(chart => chart.Id == id);
}

internal sealed record LabBenchmark(string Name, double EngineNs, long? Allocated, double? D3Ns);

internal sealed record LabMicro(string Name, string Processor, string EngineRuntime, string D3Runtime, string LoadBefore, string LoadAfter, LabBenchmark[] Benchmarks);

internal static class LabRuns
{
    private const string Workstation = "-workstation";

    public static async Task<LabDemo?> LatestDemoAsync(string results, CancellationToken cancellation)
    {
        var folder = Path.Combine(results, "demo");
        var file = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.json").Where(path => !Path.GetFileNameWithoutExtension(path).EndsWith(Workstation, StringComparison.Ordinal)).Order(StringComparer.Ordinal).LastOrDefault()
            : null;
        if (file is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(file, cancellation));
        var root = document.RootElement;
        var engine = root.GetProperty("engine");
        var d3 = root.GetProperty("d3");
        return new LabDemo(
            Path.GetFileNameWithoutExtension(file),
            root.GetProperty("commit").GetString()!,
            root.GetProperty("machine").GetString()!,
            engine.GetProperty("runtime").GetString()!,
            d3.GetProperty("runtime").GetString()!,
            Numbers(engine.GetProperty("loadAverage")),
            Numbers(d3.GetProperty("loadAverage")),
            [.. root.GetProperty("charts").EnumerateArray().Select(chart => new LabChart(
                chart.GetProperty("id").GetString()!,
                chart.GetProperty("engineMedianNs").GetDouble(),
                chart.GetProperty("d3MedianNs").GetDouble()))]);
    }

    public static async Task<LabMicro?> LatestMicroAsync(string results, CancellationToken cancellation)
    {
        var folder = Path.Combine(results, "micro");
        var run = Directory.Exists(folder)
            ? Directory.EnumerateDirectories(folder).Where(path => !path.EndsWith(Workstation, StringComparison.Ordinal) && Directory.Exists(Path.Combine(path, "engine", "results"))).Order(StringComparer.Ordinal).LastOrDefault()
            : null;
        if (run is null)
        {
            return null;
        }

        var d3 = new Dictionary<string, double>(StringComparer.Ordinal);
        var d3Runtime = "d3";
        foreach (var file in Directory.EnumerateFiles(run, "d3-*.json").Order(StringComparer.Ordinal))
        {
            using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(file, cancellation));
            d3Runtime = document.RootElement.GetProperty("runtime").GetString()!;
            foreach (var result in document.RootElement.GetProperty("results").EnumerateArray())
            {
                d3[result.GetProperty("name").GetString()!] = result.GetProperty("medianNs").GetDouble();
            }
        }

        var benchmarks = new List<LabBenchmark>();
        var (processor, runtime) = ("unknown processor", ".NET");
        foreach (var file in Directory.EnumerateFiles(Path.Combine(run, "engine", "results"), "*.json").Order(StringComparer.Ordinal))
        {
            using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(file, cancellation));
            var host = document.RootElement.GetProperty("HostEnvironmentInfo");
            (processor, runtime) = (host.GetProperty("ProcessorName").GetString()!, host.GetProperty("RuntimeVersion").GetString()!);
            benchmarks.AddRange(document.RootElement.GetProperty("Benchmarks").EnumerateArray().Select(benchmark => Benchmark(benchmark, d3)));
        }

        return new LabMicro(
            Path.GetFileName(run),
            processor,
            runtime,
            d3Runtime,
            await LoadAsync(Path.Combine(run, "load-before.txt"), cancellation),
            await LoadAsync(Path.Combine(run, "load-after.txt"), cancellation),
            [.. benchmarks.OrderBy(benchmark => benchmark.Name, StringComparer.Ordinal)]);
    }

    private static LabBenchmark Benchmark(JsonElement benchmark, Dictionary<string, double> d3)
    {
        var method = benchmark.GetProperty("Method").GetString()!;
        var kind = benchmark.GetProperty("Type").GetString()!.Replace("Benchmarks", "", StringComparison.Ordinal);
        long? allocated = benchmark.TryGetProperty("Memory", out var memory) && memory.ValueKind == JsonValueKind.Object
            ? memory.GetProperty("BytesAllocatedPerOperation").GetInt64()
            : null;
        return new LabBenchmark($"{kind}.{method}", benchmark.GetProperty("Statistics").GetProperty("Median").GetDouble(), allocated, d3.TryGetValue(method, out var ns) ? ns : null);
    }

    private static async Task<string> LoadAsync(string path, CancellationToken cancellation)
    {
        if (!File.Exists(path))
        {
            return "?";
        }

        var text = await File.ReadAllTextAsync(path, cancellation);
        var at = text.IndexOf("load average:", StringComparison.Ordinal);
        return at < 0 ? "?" : text[(at + "load average:".Length)..].Split(',')[0].Trim();
    }

    private static double[] Numbers(JsonElement array) =>
        [.. array.EnumerateArray().Select(value => value.GetDouble())];
}
