using System.Globalization;
using System.Text;

namespace Aioros.Graphs.Demo.Site;

internal static class SitePages
{
    private const string SiteStyle = """
        header{display:flex;flex-wrap:wrap;align-items:baseline;gap:8px 24px;margin-bottom:8px}
        header a.home{font-weight:700;font-size:18px;color:var(--text);text-decoration:none}
        nav{display:flex;gap:16px}nav a{color:var(--muted);text-decoration:none}nav a[aria-current]{color:var(--text);font-weight:600}
        a{color:var(--s1)}
        .none{color:var(--faint);font-weight:600}
        .cards{display:grid;grid-template-columns:repeat(auto-fill,minmax(min(100%,300px),1fr));gap:16px;margin-top:20px}
        .card{background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:12px;color:inherit;text-decoration:none;min-width:0}
        .card:hover{border-color:var(--axis)}.card h3{font-size:14px;margin:0 0 8px}
        .notice{border-left:3px solid var(--s4);padding-left:12px}
        .pager{display:flex;justify-content:space-between;gap:16px;margin-top:20px}
        """;

    private const string NoReference = "<p class=\"notice\">d3's reference is not written yet, so nothing is compared: run <code>samples/Demo/serve.sh</code>, which writes it with Node before it starts the site.</p>\n";

    private const string NoLabRun = "<p class=\"notice\">No lab run is recorded under <code>bench/results</code>: dispatch the bench workflow and copy its results (docs/benchmarks.md).</p>\n";

    public static string Index(Drawing drawing, bool compared, LabDemo? lab)
    {
        var html = Open("Aioros.Graphs demo", "charts");
        html.Append("<h1>Aioros.Graphs demo</h1>\n<p>").Append(drawing.Charts.Length)
            .Append(" charts drawn by Aioros.Graphs on the server for this request (").Append(Milliseconds(drawing.Elapsed))
            .Append(" for all of them), each compared with its reference, the same chart of the d3 gallery drawn by d3 7.9.0 from the same data. No JavaScript runs on this site.</p>\n");
        if (!compared)
        {
            html.Append(NoReference);
        }

        html.Append("<section>\n<h2>Summary</h2>\n<div class=\"table-wrap\"><table>\n<tr><th>Chart</th><th>Path data</th><th>Tick labels</th><th>Whole SVG</th><th class=\"n\">Engine (lab)</th><th class=\"n\">d3 (lab)</th><th class=\"n\">d3 ÷ engine</th></tr>\n");
        foreach (var drawn in drawing.Charts)
        {
            var timing = lab?.Chart(drawn.Chart.Id);
            html.Append("<tr data-chart=\"").Append(drawn.Chart.Id).Append("\"><td><a href=\"/charts/").Append(drawn.Chart.Id).Append("\">").Append(Scene.Escape(drawn.Chart.Title)).Append("</a></td>")
                .Append(Verdict(drawn.Comparison.Paths)).Append(Verdict(drawn.Comparison.Texts)).Append(Verdict(drawn.Comparison.Svg))
                .Append(Timing(timing)).Append("</tr>\n");
        }

        html.Append("</table></div>\n");
        html.Append(lab is null ? NoLabRun : $"<p>Timings from the lab run <code>{Scene.Escape(lab.Name)}</code>; see <a href=\"/benchmarks\">Benchmarks</a>.</p>\n");
        html.Append("</section>\n<div class=\"cards\">\n");
        foreach (var drawn in drawing.Charts)
        {
            html.Append("<a class=\"card\" href=\"/charts/").Append(drawn.Chart.Id).Append("\"><h3>").Append(Scene.Escape(drawn.Chart.Title)).Append("</h3>\n").Append(drawn.Engine.Svg).Append("\n</a>\n");
        }

        html.Append("</div>\n");
        return Close(html);
    }

    public static string Chart(Drawing drawing, bool compared, LabDemo? lab, Chart? previous, Chart? next)
    {
        var drawn = drawing.Charts[0];
        var chart = drawn.Chart;
        var html = Open($"{chart.Title} · Aioros.Graphs demo", "charts");
        html.Append("<h1>").Append(Scene.Escape(chart.Title)).Append("</h1>\n<p>").Append(Scene.Escape(chart.Description)).Append("</p>\n");
        if (!compared)
        {
            html.Append(NoReference);
        }

        html.Append("<section>\n<div class=\"pair\">\n<figure><figcaption>Aioros.Graphs, drawn for this request in ").Append(Milliseconds(drawing.Elapsed)).Append("</figcaption>\n")
            .Append(drawn.Engine.Svg).Append("\n</figure>\n<figure><figcaption>d3 7.9.0</figcaption>\n")
            .Append(drawn.D3?.Svg ?? "<p class=\"none\">not compared</p>").Append("\n</figure>\n</div>\n<p class=\"verdict\">")
            .Append(PathVerdict(drawn.Comparison)).Append(" Tick labels and texts: ").Append(Mark(drawn.Comparison.Texts, "identical"))
            .Append(". Whole SVG: ").Append(Mark(drawn.Comparison.Svg, "byte-identical")).Append(".</p>\n");
        var timing = lab?.Chart(chart.Id);
        html.Append(lab is null || timing is null
            ? NoLabRun
            : $"<p>Compute time in the lab run <code>{Scene.Escape(lab.Name)}</code>: engine {Page.Micro(timing.EngineNs)}, d3 {Page.Micro(timing.D3Ns)} ({Ratio(timing.D3Ns / timing.EngineNs)} faster).</p>\n");
        html.Append("</section>\n<div class=\"pager\">").Append(Link(previous, "← ")).Append(Link(next, "", " →")).Append("</div>\n");
        return Close(html);
    }

    public static string Benchmarks(LabMicro? micro, LabDemo? demo)
    {
        var html = Open("Benchmarks · Aioros.Graphs demo", "benchmarks");
        html.Append("<h1>Benchmarks</h1>\n<p>The engine against d3 on the same work, from the latest run recorded on the lab runner (both runtimes pinned to the same cores). Ratios above 1 mean the engine is faster. The method is in <code>docs/benchmarks.md</code>.</p>\n");
        if (micro is null && demo is null)
        {
            html.Append(NoLabRun);
            return Close(html);
        }

        if (micro is not null)
        {
            BenchmarkPage.Micro(html, micro);
        }

        if (demo is not null)
        {
            BenchmarkPage.Charts(html, demo);
        }

        return Close(html);
    }

    internal static string Duration(double ns) => ns < 1000 ? ns.ToString("0", CultureInfo.InvariantCulture) + " ns" : Page.Micro(ns);

    internal static string Ratio(double ratio) => ratio.ToString("0.0", CultureInfo.InvariantCulture) + "×";

    private static string Timing(LabChart? timing) => timing is null
        ? "<td class=\"n\">–</td><td class=\"n\">–</td><td class=\"n\">–</td>"
        : $"<td class=\"n\">{Page.Micro(timing.EngineNs)}</td><td class=\"n\">{Page.Micro(timing.D3Ns)}</td><td class=\"n\">{Ratio(timing.D3Ns / timing.EngineNs)}</td>";

    private static string Verdict(bool? same) => $"<td class=\"verdict\">{Mark(same, "identical")}</td>";

    private static string Mark(bool? same, string identical) => same switch
    {
        true => $"<span class=\"ok\">{identical}</span>",
        false => "<span class=\"diff\">different</span>",
        null => "<span class=\"none\">not compared</span>",
    };

    private static string PathVerdict(Comparison comparison) => comparison.Paths switch
    {
        true => $"Path data: <span class=\"ok\">byte-identical to d3</span> ({comparison.TotalPaths} {(comparison.TotalPaths == 1 ? "path" : "paths")}).",
        false => $"Path data: <span class=\"diff\">{comparison.SamePaths} of {comparison.TotalPaths} paths identical</span>.",
        null => "Path data: <span class=\"none\">not compared</span>.",
    };

    private static string Link(Chart? chart, string before, string after = "") =>
        chart is null ? "<span></span>" : $"<a href=\"/charts/{chart.Id}\">{before}{Scene.Escape(chart.Title)}{after}</a>";

    private static string Milliseconds(TimeSpan elapsed) => elapsed.TotalMilliseconds.ToString("0.00", CultureInfo.InvariantCulture) + " ms";

    private static StringBuilder Open(string title, string current)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>")
            .Append(Scene.Escape(title)).Append("</title>\n<style>\n").Append(Page.Style).Append(SiteStyle).Append("\n</style>\n</head>\n<body>\n<main>\n<header><a class=\"home\" href=\"/\">Aioros.Graphs</a>\n<nav>")
            .Append(NavLink("/", "Charts", current == "charts")).Append(NavLink("/benchmarks", "Benchmarks", current == "benchmarks")).Append("</nav></header>\n");
        return html;
    }

    private static string NavLink(string href, string text, bool current) =>
        $"<a href=\"{href}\"{(current ? " aria-current=\"page\"" : "")}>{text}</a>";

    private static string Close(StringBuilder html) => html.Append("</main>\n</body>\n</html>\n").ToString();
}
