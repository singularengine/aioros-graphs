using System.Globalization;
using System.Runtime.InteropServices;
using Aioros.Graphs.Demo;
using Aioros.Graphs.Demo.Site;

if (args.Length == 0 || args[0] is not ("data" or "page"))
{
    await DemoSite.Create(args).RunAsync();
    return 0;
}

if (args.Length < 2)
{
    await Console.Error.WriteLineAsync("usage: Demo [data|page <output directory> [commit]]");
    return 1;
}

var output = Directory.CreateDirectory(args[1]).FullName;
var dataPath = Path.Combine(output, "data.json");
if (args[0] == "data")
{
    await Json.WriteAsync(dataPath, json => Json.Data(json, DataSet.Generate()));
    Console.WriteLine($"wrote {dataPath}");
    return 0;
}

var data = DataSet.Generate();
var load = await LoadAverageAsync();
var charts = new List<ChartResult>();
foreach (var chart in Charts.All)
{
    var scene = chart.Draw(data);
    var (median, iterations) = Timing.Measure(() => chart.Draw(data));
    charts.Add(new ChartResult(chart.Id, scene.ToSvg(chart.Title), [.. scene.Paths], [.. scene.Texts], median, iterations, 11));
    Console.WriteLine($"{chart.Id,-14} {median / 1000,10:F1} µs  ({iterations} iterations per sample)");
}

var engine = new RunResult(
    $".NET {Environment.Version}, {RuntimeInformation.ProcessArchitecture}, Release",
    await MachineAsync(),
    DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
    [.. load, .. await LoadAverageAsync()],
    [.. charts]);
await Json.WriteAsync(Path.Combine(output, "engine.json"), json => Json.Run(json, engine));
var d3 = await Json.ReadRunAsync(Path.Combine(output, "d3.json"));
var page = Path.Combine(output, "index.html");
await File.WriteAllTextAsync(page, Page.Write(Charts.All, engine, d3));
var commit = args.Length > 2 ? args[2] : "unknown";
await Json.WriteAsync(Path.Combine(output, "results.json"), json => Json.Results(json, commit, engine, d3));
Console.WriteLine($"wrote {page}");
return 0;

static async Task<double[]> LoadAverageAsync()
{
    var text = await File.ReadAllTextAsync("/proc/loadavg");
    return [double.Parse(text.Split(' ')[0], CultureInfo.InvariantCulture)];
}

static async Task<string> MachineAsync()
{
    var model = (await File.ReadAllLinesAsync("/proc/cpuinfo")).FirstOrDefault(line => line.StartsWith("model name", StringComparison.Ordinal));
    return $"{model?.Split(':', 2)[1].Trim() ?? "unknown CPU"}, {Environment.ProcessorCount} threads";
}
