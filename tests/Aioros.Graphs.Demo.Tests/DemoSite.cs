using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aioros.Graphs.Demo.Tests;

internal sealed class DemoSite : IDisposable
{
    private readonly WebApplicationFactory<Program> factory;

    public DemoSite()
    {
        Folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "sites", Guid.NewGuid().ToString("N"))).FullName;
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("Demo:Reference", Reference);
            host.UseSetting("Demo:Results", Results);
        });
    }

    public string Folder { get; }

    public string Reference => Path.Combine(Folder, "d3.json");

    public string Results => Path.Combine(Folder, "results");

    public HttpClient Client() => factory.CreateClient();

    public static ChartResult Draw(Chart chart)
    {
        var scene = chart.Draw(DataSet.Generate());
        return new ChartResult(chart.Id, scene.ToSvg(chart.Title), [.. scene.Paths], [.. scene.Texts], 1, 1, 1);
    }

    public Task WriteReferenceAsync(Func<ChartResult, ChartResult>? change = null)
    {
        var charts = Charts.All.Select(Draw).Select(change ?? (chart => chart)).ToArray();
        return Json.WriteAsync(Reference, json => Json.Run(json, new RunResult("d3 test", "test machine", "2026-01-01T00:00:00Z", [0, 0], charts)));
    }

    public async Task WriteDemoRunAsync(string name, string commit, double engineNs, double d3Ns)
    {
        var folder = Directory.CreateDirectory(Path.Combine(Results, "demo")).FullName;
        var charts = string.Join(',', Charts.All.Select(chart =>
            $$"""{"id":"{{chart.Id}}","engineMedianNs":{{engineNs}},"engineIterations":1,"d3MedianNs":{{d3Ns}},"d3Iterations":1,"paths":1,"pathsIdentical":true,"textsIdentical":true,"svgIdentical":true}"""));
        await File.WriteAllTextAsync(Path.Combine(folder, name + ".json"), $$"""
            {"kind":"demo","commit":"{{commit}}","machine":"Machine {{commit}}","method":"m",
             "engine":{"runtime":"Engine runtime {{commit}}","date":"2026-01-01T00:00:00Z","loadAverage":[1.5,1.2]},
             "d3":{"runtime":"d3 runtime {{commit}}","date":"2026-01-01T00:00:00Z","loadAverage":[2.5,2.2]},
             "charts":[{{charts}}]}
            """);
    }

    public async Task WriteMicroRunAsync(string name, double engineNs, double d3Ns)
    {
        var folder = Directory.CreateDirectory(Path.Combine(Results, "micro", name, "engine", "results")).FullName;
        await File.WriteAllTextAsync(Path.Combine(folder, "Aioros.Graphs.Benchmarks.ShapeBenchmarks-report-full-compressed.json"), $$$"""
            {"Title":"t","HostEnvironmentInfo":{"RuntimeVersion":".NET {{{name}}}","ProcessorName":"Processor {{{name}}}"},
             "Benchmarks":[
               {"Type":"ShapeBenchmarks","Method":"LineLinear10kToSvg","Statistics":{"Median":{{{engineNs}}}},"Memory":{"BytesAllocatedPerOperation":0}},
               {"Type":"ShapeBenchmarks","Method":"LineMonotoneX10kToCounter","Statistics":{"Median":500},"Memory":{"BytesAllocatedPerOperation":64}}]}
            """);
        var run = Path.Combine(Results, "micro", name);
        await File.WriteAllTextAsync(Path.Combine(run, "d3-shapes.json"), $$"""
            {"runtime":"Node {{name}}","machine":"m","date":"d","loadAverage":[0,0],"results":[{"name":"LineLinear10kToSvg","medianNs":{{d3Ns}},"iterations":1,"samples":7}]}
            """);
        await File.WriteAllTextAsync(Path.Combine(run, "load-before.txt"), " 00:00:00 up 1 day,  0 user,  load average: 2.84, 3.35, 2.93\n");
        await File.WriteAllTextAsync(Path.Combine(run, "load-after.txt"), " 00:30:00 up 1 day,  0 user,  load average: 3.06, 4.31, 3.75\n");
    }

    public void Dispose()
    {
        factory.Dispose();
        Directory.Delete(Folder, recursive: true);
    }
}
