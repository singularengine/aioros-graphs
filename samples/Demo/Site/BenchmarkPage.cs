using System.Text;

namespace Aioros.Graphs.Demo.Site;

internal static class BenchmarkPage
{
    public static void Micro(StringBuilder html, LabMicro run)
    {
        html.Append("<section>\n<h2>Micro-benchmarks</h2>\n<p>Run <code>").Append(Scene.Escape(run.Name)).Append("</code> on ").Append(Scene.Escape(run.Processor))
            .Append(". Engine: ").Append(Scene.Escape(run.EngineRuntime)).Append(", BenchmarkDotNet. d3: ").Append(Scene.Escape(run.D3Runtime))
            .Append(". 1-minute load average ").Append(Scene.Escape(run.LoadBefore)).Append(" to ").Append(Scene.Escape(run.LoadAfter)).Append(".</p>\n")
            .Append("<div class=\"table-wrap\"><table>\n<tr><th>Benchmark</th><th class=\"n\">Engine</th><th class=\"n\">Allocated</th><th class=\"n\">d3</th><th class=\"n\">d3 ÷ engine</th></tr>\n");
        var ratios = new List<double>();
        foreach (var benchmark in run.Benchmarks)
        {
            if (benchmark.D3Ns is { } d3)
            {
                ratios.Add(d3 / benchmark.EngineNs);
            }

            html.Append("<tr><td>").Append(Scene.Escape(benchmark.Name)).Append("</td><td class=\"n\">").Append(SitePages.Duration(benchmark.EngineNs))
                .Append("</td><td class=\"n\">").Append(benchmark.Allocated is { } bytes ? $"{bytes} B" : "–")
                .Append("</td><td class=\"n\">").Append(benchmark.D3Ns is { } ns ? SitePages.Duration(ns) : "–")
                .Append("</td><td class=\"n\">").Append(benchmark.D3Ns is { } other ? SitePages.Ratio(other / benchmark.EngineNs) : "–").Append("</td></tr>\n");
        }

        if (ratios.Count > 0)
        {
            html.Append("<tr><td>geometric mean of ").Append(ratios.Count).Append(" paired</td><td></td><td></td><td></td><td class=\"n\">").Append(SitePages.Ratio(GeometricMean(ratios))).Append("</td></tr>\n");
        }

        html.Append("</table></div>\n</section>\n");
    }

    public static void Charts(StringBuilder html, LabDemo run)
    {
        html.Append("<section>\n<h2>Charts</h2>\n<p>Run <code>").Append(Scene.Escape(run.Name)).Append("</code> (commit <code>").Append(Scene.Escape(run.Commit)).Append("</code>) on ").Append(Scene.Escape(run.Machine))
            .Append(". Engine: ").Append(Scene.Escape(run.EngineRuntime)).Append(", load average ").Append(Page.Load(run.EngineLoad))
            .Append(". d3: ").Append(Scene.Escape(run.D3Runtime)).Append(", load average ").Append(Page.Load(run.D3Load))
            .Append(". Per chart: scales, ticks, tick labels and path strings, without the SVG markup; median of 11 samples after a warm-up.</p>\n")
            .Append("<div class=\"table-wrap\"><table>\n<tr><th>Chart</th><th class=\"n\">Engine</th><th class=\"n\">d3</th><th class=\"n\">d3 ÷ engine</th></tr>\n");
        var titles = Demo.Charts.All.ToDictionary(chart => chart.Id, chart => chart.Title, StringComparer.Ordinal);
        foreach (var chart in run.Charts)
        {
            html.Append("<tr><td><a href=\"/charts/").Append(chart.Id).Append("\">").Append(Scene.Escape(titles.GetValueOrDefault(chart.Id, chart.Id))).Append("</a></td><td class=\"n\">").Append(Page.Micro(chart.EngineNs))
                .Append("</td><td class=\"n\">").Append(Page.Micro(chart.D3Ns)).Append("</td><td class=\"n\">").Append(SitePages.Ratio(chart.D3Ns / chart.EngineNs)).Append("</td></tr>\n");
        }

        html.Append("<tr><td>geometric mean</td><td></td><td></td><td class=\"n\">").Append(SitePages.Ratio(GeometricMean([.. run.Charts.Select(c => c.D3Ns / c.EngineNs)]))).Append("</td></tr>\n</table></div>\n</section>\n");
    }

    private static double GeometricMean(List<double> values) => Math.Exp(values.Sum(Math.Log) / values.Count);
}
