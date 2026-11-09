using System.Globalization;
using System.Text;

namespace Aioros.Graphs.Demo;

internal static class Page
{
    internal const string Style = """
        :root{color-scheme:light;--bg:#f4f4f2;--surface:#fcfcfb;--text:#0b0b0b;--muted:#52514e;--faint:#8a8984;--grid:#e4e3df;--axis:#a5a49f;--border:#dddcd7;--ok:#0a7a3d;--diff:#b3261e;
        --s1:#2a78d6;--s2:#eb6834;--s3:#1baf7a;--s4:#eda100;--s5:#e87ba4;--s6:#008300;--s7:#6250d6;--s8:#e34948}
        @media (prefers-color-scheme:dark){:root{color-scheme:dark;--bg:#111110;--surface:#1a1a19;--text:#f0efec;--muted:#c3c2b7;--faint:#8f8e86;--grid:#2c2c2a;--axis:#5c5b57;--border:#2f2f2d;--ok:#5cc98a;--diff:#f2867e;
        --s1:#3987e5;--s2:#d95926;--s3:#199e70;--s4:#c98500;--s5:#d55181;--s6:#008300;--s7:#9085e9;--s8:#e66767}}
        *{box-sizing:border-box}
        body{margin:0;background:var(--bg);color:var(--text);font:15px/1.5 system-ui,-apple-system,"Segoe UI",Roboto,sans-serif}
        main{max-width:1360px;margin:0 auto;padding:24px 16px 48px}
        h1{font-size:28px;margin:0 0 4px;letter-spacing:-.01em}
        h2{font-size:18px;margin:0}
        p{margin:4px 0;color:var(--muted)}
        code{font:13px ui-monospace,SFMono-Regular,Menlo,monospace}
        section{background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:16px;margin-top:20px}
        .pair{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,420px),1fr));gap:16px;margin-top:12px}
        figure{margin:0;min-width:0}
        figcaption{font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:.06em;color:var(--faint);margin-bottom:4px}
        .verdict{font-size:14px;margin-top:8px}
        .ok{color:var(--ok);font-weight:600}.diff{color:var(--diff);font-weight:600}
        .table-wrap{overflow-x:auto}
        table{border-collapse:collapse;width:100%;font-size:14px;font-variant-numeric:tabular-nums}
        th,td{text-align:left;padding:6px 10px;border-bottom:1px solid var(--border);white-space:nowrap}
        th{color:var(--muted);font-weight:600}td.n,th.n{text-align:right}
        svg.chart{display:block;width:100%;height:auto;font-family:system-ui,-apple-system,"Segoe UI",Roboto,sans-serif}
        .s1{--c:var(--s1)}.s2{--c:var(--s2)}.s3{--c:var(--s3)}.s4{--c:var(--s4)}.s5{--c:var(--s5)}.s6{--c:var(--s6)}.s7{--c:var(--s7)}.s8{--c:var(--s8)}
        .line{fill:none;stroke:var(--c);stroke-width:2;stroke-linejoin:round}
        .area{fill:var(--c);opacity:.16}
        .band,.bar,.slice,.stream,.swatch{fill:var(--c);stroke:var(--surface);stroke-width:1}
        .grid{stroke:var(--grid)}.domain,.tick{stroke:var(--axis)}
        .label,.unit{fill:var(--muted);font-size:11px}
        .legend{fill:var(--text);font-size:12px}.legend-value{fill:var(--muted);font-size:12px;font-variant-numeric:tabular-nums}
        .slice-name{fill:#fff;font-size:12px;font-weight:600}.slice-value{fill:#fff;font-size:11px}
        .panel{fill:var(--text);font-size:12px;font-family:ui-monospace,SFMono-Regular,Menlo,monospace}
        .dot{fill:var(--surface);stroke:var(--text);stroke-width:1.5}
        .symbol{fill:var(--c);fill-opacity:.7;stroke:var(--c);stroke-width:1}
        .radial{fill:var(--c);fill-opacity:.55;stroke:var(--c);stroke-width:1}
        .ring{fill:none;stroke:var(--grid)}
        .cell{stroke:var(--surface);stroke-width:.5}
        .axis{color:var(--axis)}.axis text{fill:var(--muted);font-size:11px}
        .tile,.bubble,.sector,.chord-group{fill:var(--c);stroke:var(--surface);stroke-width:.5}
        .sector.depth2{fill-opacity:.75}.sector.depth3{fill-opacity:.55}.bubble{fill-opacity:.8}
        .tile-name,.sector-name{fill:#fff;font-size:10px;font-weight:600}.bubble-name{fill:#fff;font-size:9px}
        .group-circle{fill:var(--grid);fill-opacity:.5;stroke:var(--axis);stroke-width:.5}.group-circle.depth0{fill:none}
        .link{fill:none;stroke:var(--axis);stroke-width:1}.node{fill:var(--axis)}.node.inner{fill:var(--text)}
        .ribbon{fill:var(--c);fill-opacity:.62;stroke:var(--surface);stroke-width:.5}
        .flow-node{fill:var(--c)}.flow{fill:none;stroke:var(--c);stroke-opacity:.4}
        .edge{fill:none;stroke:var(--axis);stroke-opacity:.7;stroke-width:1}.vertex{fill:var(--c);stroke:var(--surface);stroke-width:1.2}
        """;

    public static string Write(Chart[] charts, RunResult engine, RunResult d3)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>Aioros.Graphs demo</title>\n<style>\n")
            .Append(Style).Append("\n</style>\n</head>\n<body>\n<main>\n<h1>Aioros.Graphs demo</h1>\n")
            .Append("<p>").Append(charts.Length).Append(" charts drawn twice from the same data: by Aioros.Graphs (C#) and by its reference, the d3 gallery's chart in d3 7.9.0 (the sankey with d3-sankey 0.12.3). ")
            .Append("Scales, ticks, tick labels and path data come from each library; the axes, gridlines and markup are drawn by the same small scene code in both, except the log-scale chart, whose axes are d3-axis's own markup (from the engine's <code>Axis</code> and <code>SvgAxisWriter</code>, and from d3-axis on a minimal DOM). No JavaScript runs on this page.</p>\n");
        Summary(html, charts, engine, d3);
        for (var i = 0; i < charts.Length; i++)
        {
            Section(html, charts[i], engine.Charts[i], d3.Charts.Single(c => c.Id == charts[i].Id));
        }

        html.Append("</main>\n</body>\n</html>\n");
        return html.ToString();
    }

    private static bool Quiet(RunResult engine, RunResult d3) => engine.LoadAverage.Concat(d3.LoadAverage).All(load => load < 4);

    private static void Summary(StringBuilder html, Chart[] charts, RunResult engine, RunResult d3)
    {
        html.Append("<section>\n<h2>Summary</h2>\n<p>Compute time per chart: scales, ticks, tick labels and path strings, without the SVG markup. Median of 11 samples after a warm-up. ")
            .Append(Quiet(engine, d3) ? "Measured with both runtimes pinned to the same cores of an otherwise quiet machine (" : "<strong>Indicative only</strong>: measured on a shared, loaded machine (").Append(Scene.Escape(engine.Machine))
            .Append("; 1-minute load average ").Append(Load(engine.LoadAverage)).Append(" during the engine run, ").Append(Load(d3.LoadAverage)).Append(" during d3's).</p>\n")
            .Append("<div class=\"table-wrap\"><table>\n<tr><th>Chart</th><th>Path data</th><th>Tick labels</th><th>Whole SVG</th><th class=\"n\">Engine</th><th class=\"n\">d3</th><th class=\"n\">d3 ÷ engine</th></tr>\n");
        for (var i = 0; i < charts.Length; i++)
        {
            var e = engine.Charts[i];
            var d = d3.Charts.Single(c => c.Id == charts[i].Id);
            html.Append("<tr><td>").Append(Scene.Escape(charts[i].Title)).Append("</td><td>").Append(Mark(e.Paths.SequenceEqual(d.Paths)))
                .Append("</td><td>").Append(Mark(e.Texts.SequenceEqual(d.Texts))).Append("</td><td>").Append(Mark(e.Svg == d.Svg))
                .Append("</td><td class=\"n\">").Append(Micro(e.MedianNs)).Append("</td><td class=\"n\">").Append(Micro(d.MedianNs))
                .Append("</td><td class=\"n\">").Append((d.MedianNs / e.MedianNs).ToString("0.0", CultureInfo.InvariantCulture)).Append("×</td></tr>\n");
        }

        html.Append("</table></div>\n<p>Engine: ").Append(Scene.Escape(engine.Runtime)).Append(". d3: ").Append(Scene.Escape(d3.Runtime)).Append(".</p>\n</section>\n");
    }

    private static void Section(StringBuilder html, Chart chart, ChartResult e, ChartResult d)
    {
        html.Append("<section id=\"").Append(chart.Id).Append("\">\n<h2>").Append(Scene.Escape(chart.Title)).Append("</h2>\n<p>").Append(Scene.Escape(chart.Description)).Append("</p>\n")
            .Append("<div class=\"pair\">\n<figure><figcaption>Aioros engine</figcaption>\n").Append(e.Svg).Append("\n</figure>\n")
            .Append("<figure><figcaption>d3 7.9.0</figcaption>\n").Append(d.Svg).Append("\n</figure>\n</div>\n<p class=\"verdict\">")
            .Append(PathVerdict(e, d)).Append(" Tick labels and texts: ").Append(e.Texts.SequenceEqual(d.Texts) ? $"<span class=\"ok\">identical</span> ({e.Texts.Length})." : "<span class=\"diff\">different</span>.")
            .Append(" Whole SVG: ").Append(e.Svg == d.Svg ? "<span class=\"ok\">byte-identical</span>." : "<span class=\"diff\">different</span>.")
            .Append(" Compute: engine ").Append(Micro(e.MedianNs)).Append(", d3 ").Append(Micro(d.MedianNs)).Append(".</p>\n</section>\n");
    }

    private static string PathVerdict(ChartResult e, ChartResult d)
    {
        var bytes = e.Paths.Sum(p => Encoding.UTF8.GetByteCount(p));
        if (e.Paths.SequenceEqual(d.Paths))
        {
            return $"Path data: <span class=\"ok\">byte-identical to d3</span> ({e.Paths.Length} {(e.Paths.Length == 1 ? "path" : "paths")}, {bytes:N0} bytes).";
        }

        var same = e.Paths.Zip(d.Paths).Count(pair => pair.First == pair.Second);
        return $"Path data: <span class=\"diff\">{same} of {Math.Max(e.Paths.Length, d.Paths.Length)} paths identical</span>.";
    }

    private static string Mark(bool same) => same ? "<span class=\"ok\">identical</span>" : "<span class=\"diff\">different</span>";

    internal static string Micro(double ns) => (ns / 1000).ToString(ns < 100_000 ? "0.0" : "0", CultureInfo.InvariantCulture) + " µs";

    internal static string Load(double[] load) => string.Join("–", load.Select(l => l.ToString("0.#", CultureInfo.InvariantCulture)));
}
