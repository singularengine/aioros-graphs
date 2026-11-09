using System.Diagnostics;

namespace Aioros.Graphs.Demo.Site;

internal sealed record Comparison(bool? Paths, bool? Texts, bool? Svg, int SamePaths, int TotalPaths)
{
    public static Comparison Of(ChartResult engine, ChartResult? d3) => d3 is null
        ? new(null, null, null, 0, engine.Paths.Length)
        : new(
            engine.Paths.SequenceEqual(d3.Paths),
            engine.Texts.SequenceEqual(d3.Texts),
            engine.Svg == d3.Svg,
            engine.Paths.Zip(d3.Paths).Count(pair => pair.First == pair.Second),
            Math.Max(engine.Paths.Length, d3.Paths.Length));
}

internal sealed record Drawn(Chart Chart, ChartResult Engine, ChartResult? D3, Comparison Comparison);

internal sealed record Drawing(Drawn[] Charts, TimeSpan Elapsed)
{
    public static Drawing Of(IEnumerable<Chart> charts, DemoData data, RunResult? reference)
    {
        var start = Stopwatch.GetTimestamp();
        var engine = charts.Select(chart => (chart, result: Draw(chart, data))).ToArray();
        var elapsed = Stopwatch.GetElapsedTime(start);
        return new Drawing(
            [.. engine.Select(pair =>
            {
                var d3 = reference?.Charts.FirstOrDefault(c => c.Id == pair.chart.Id);
                return new Drawn(pair.chart, pair.result, d3, Comparison.Of(pair.result, d3));
            })],
            elapsed);
    }

    private static ChartResult Draw(Chart chart, DemoData data)
    {
        var scene = chart.Draw(data);
        return new ChartResult(chart.Id, scene.ToSvg(chart.Title), [.. scene.Paths], [.. scene.Texts], 0, 0, 0);
    }
}
