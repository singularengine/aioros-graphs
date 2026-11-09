using System.Globalization;

namespace Aioros.Graphs.Demo.Site;

internal static class DemoSite
{
    public static WebApplication Create(string[] args)
    {
        var app = WebApplication.CreateSlimBuilder(args).Build();
        var root = app.Environment.ContentRootPath;
        var reference = app.Configuration["Demo:Reference"] ?? Path.Combine(root, "out", "d3.json");
        var results = app.Configuration["Demo:Results"] ?? Path.GetFullPath(Path.Combine(root, "..", "..", "bench", "results"));
        var data = DataSet.Generate();

        app.MapGet("/", async (HttpContext context, CancellationToken cancellation) =>
        {
            var d3 = await ReferenceAsync(reference);
            var drawing = Drawing.Of(Charts.All, data, d3);
            return Html(context, drawing, SitePages.Index(drawing, d3 is not null, await LabRuns.LatestDemoAsync(results, cancellation)));
        });

        app.MapGet("/charts/{id}", async (string id, HttpContext context, CancellationToken cancellation) =>
        {
            var index = Array.FindIndex(Charts.All, chart => chart.Id == id);
            if (index < 0)
            {
                return Results.NotFound();
            }

            var d3 = await ReferenceAsync(reference);
            var drawing = Drawing.Of([Charts.All[index]], data, d3);
            var previous = index > 0 ? Charts.All[index - 1] : null;
            var next = index < Charts.All.Length - 1 ? Charts.All[index + 1] : null;
            return Html(context, drawing, SitePages.Chart(drawing, d3 is not null, await LabRuns.LatestDemoAsync(results, cancellation), previous, next));
        });

        app.MapGet("/benchmarks", async (CancellationToken cancellation) =>
            Results.Content(SitePages.Benchmarks(await LabRuns.LatestMicroAsync(results, cancellation), await LabRuns.LatestDemoAsync(results, cancellation)), "text/html; charset=utf-8"));

        return app;
    }

    private static async Task<RunResult?> ReferenceAsync(string path) =>
        File.Exists(path) ? await Json.ReadRunAsync(path) : null;

    private static IResult Html(HttpContext context, Drawing drawing, string html)
    {
        context.Response.Headers["Server-Timing"] = "draw;dur=" + drawing.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture);
        return Results.Content(html, "text/html; charset=utf-8");
    }
}
