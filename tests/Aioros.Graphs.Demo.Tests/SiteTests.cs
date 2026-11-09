using System.Net;
using System.Text.RegularExpressions;

namespace Aioros.Graphs.Demo.Tests;

public sealed partial class SiteTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("AC", "DEMO-01")]
    public async Task TheIndexLinksEveryChartWithItsVerdictsAsync()
    {
        using var site = new DemoSite();
        await site.WriteReferenceAsync();

        var response = await site.Client().GetAsync(new Uri("/", UriKind.Relative), Cancellation);
        var html = await response.Content.ReadAsStringAsync(Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        foreach (var chart in Charts.All)
        {
            Assert.Contains($"href=\"/charts/{chart.Id}\"", Row(html, chart.Id), StringComparison.Ordinal);
            Assert.Equal(["identical", "identical", "identical"], Verdicts(Row(html, chart.Id)));
        }
    }

    [Fact]
    [Trait("AC", "DEMO-02")]
    public async Task AChartPageShowsTheEngineBesideTheReferenceWithTheLabMediansAsync()
    {
        using var site = new DemoSite();
        await site.WriteReferenceAsync();
        await site.WriteDemoRunAsync("2026-01-01-aaa1111", "aaa1111", 35_344, 99_400);
        var chart = Charts.All[0];

        var html = await site.Client().GetStringAsync(new Uri($"/charts/{chart.Id}", UriKind.Relative), Cancellation);

        Assert.Equal(2, Count(html, DemoSite.Draw(chart).Svg));
        Assert.Contains("Whole SVG: <span class=\"ok\">byte-identical</span>", html, StringComparison.Ordinal);
        Assert.Contains("35.3 µs", html, StringComparison.Ordinal);
        Assert.Contains("99.4 µs", html, StringComparison.Ordinal);
        Assert.Contains("aaa1111", html, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "DEMO-03")]
    public async Task AChangedPathIsReportedAsDifferentAsync()
    {
        using var site = new DemoSite();
        var changed = Charts.All[1];
        await site.WriteReferenceAsync(chart => chart.Id != changed.Id ? chart : chart with { Paths = [chart.Paths[0] + "Z", .. chart.Paths[1..]], Svg = chart.Svg + " " });
        var client = site.Client();

        var page = await client.GetStringAsync(new Uri($"/charts/{changed.Id}", UriKind.Relative), Cancellation);
        var index = await client.GetStringAsync(new Uri("/", UriKind.Relative), Cancellation);

        var total = DemoSite.Draw(changed).Paths.Length;
        Assert.Contains($"{total - 1} of {total} paths identical", page, StringComparison.Ordinal);
        Assert.Contains("Whole SVG: <span class=\"diff\">different</span>", page, StringComparison.Ordinal);
        Assert.Equal(["different", "identical", "different"], Verdicts(Row(index, changed.Id)));
        Assert.Equal(["identical", "identical", "identical"], Verdicts(Row(index, Charts.All[0].Id)));
    }

    [Fact]
    [Trait("AC", "DEMO-04")]
    public async Task AnUnknownChartIsNotFoundAsync()
    {
        using var site = new DemoSite();

        var response = await site.Client().GetAsync(new Uri("/charts/no-such-chart", UriKind.Relative), Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "DEMO-05")]
    public async Task WithoutAReferenceEveryVerdictIsNotComparedAsync()
    {
        using var site = new DemoSite();
        var client = site.Client();
        var chart = Charts.All[0];

        var index = await client.GetStringAsync(new Uri("/", UriKind.Relative), Cancellation);
        var page = await client.GetStringAsync(new Uri($"/charts/{chart.Id}", UriKind.Relative), Cancellation);

        Assert.All(Charts.All, c => Assert.Equal(["not compared", "not compared", "not compared"], Verdicts(Row(index, c.Id))));
        Assert.Contains("samples/Demo/serve.sh", index, StringComparison.Ordinal);
        Assert.Contains(DemoSite.Draw(chart).Svg, page, StringComparison.Ordinal);
        Assert.Contains("Whole SVG: <span class=\"none\">not compared</span>", page, StringComparison.Ordinal);
        Assert.Contains("samples/Demo/serve.sh", page, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "DEMO-06")]
    public async Task TheBenchmarksPageShowsTheNewestLabRunAsync()
    {
        using var site = new DemoSite();
        await site.WriteDemoRunAsync("2026-01-01-aaa1111", "aaa1111", 100_000, 150_000);
        await site.WriteDemoRunAsync("2026-01-02-bbb2222", "bbb2222", 100_000, 200_000);
        await site.WriteDemoRunAsync("2026-01-03-ccc3333-workstation", "ccc3333", 100_000, 900_000);
        await site.WriteMicroRunAsync("2026-01-01-aaa1111", 2000, 3000);
        await site.WriteMicroRunAsync("2026-01-02-bbb2222", 1000, 3000);
        await site.WriteMicroRunAsync("2026-01-03-ccc3333-workstation", 1000, 9000);

        var html = await site.Client().GetStringAsync(new Uri("/benchmarks", UriKind.Relative), Cancellation);

        Assert.Contains("<tr><td>Shape.LineLinear10kToSvg</td><td class=\"n\">1.0 µs</td><td class=\"n\">0 B</td><td class=\"n\">3.0 µs</td><td class=\"n\">3.0×</td></tr>", html, StringComparison.Ordinal);
        Assert.Contains("<tr><td>Shape.LineMonotoneX10kToCounter</td><td class=\"n\">500 ns</td><td class=\"n\">64 B</td><td class=\"n\">–</td><td class=\"n\">–</td></tr>", html, StringComparison.Ordinal);
        Assert.Contains("<tr><td>geometric mean of 1 paired</td><td></td><td></td><td></td><td class=\"n\">3.0×</td></tr>", html, StringComparison.Ordinal);
        Assert.Equal(Charts.All.Length, Count(html, "<td class=\"n\">100 µs</td><td class=\"n\">200 µs</td><td class=\"n\">2.0×</td>"));
        Assert.Contains("Machine bbb2222", html, StringComparison.Ordinal);
        Assert.Contains("Engine runtime bbb2222", html, StringComparison.Ordinal);
        Assert.Contains("d3 runtime bbb2222", html, StringComparison.Ordinal);
        Assert.Contains(".NET 2026-01-02-bbb2222", html, StringComparison.Ordinal);
        Assert.Contains("Node 2026-01-02-bbb2222", html, StringComparison.Ordinal);
        Assert.Contains("2.84 to 3.06", html, StringComparison.Ordinal);
        Assert.Contains("1.5–1.2", html, StringComparison.Ordinal);
        Assert.DoesNotContain("aaa1111", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ccc3333", html, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "DEMO-07")]
    public async Task WithoutALabRunThePagesSaySoAsync()
    {
        using var site = new DemoSite();
        await site.WriteDemoRunAsync("2026-01-03-ccc3333-workstation", "ccc3333", 100_000, 900_000);
        var client = site.Client();

        var response = await client.GetAsync(new Uri("/benchmarks", UriKind.Relative), Cancellation);
        var benchmarks = await response.Content.ReadAsStringAsync(Cancellation);
        var page = await client.GetStringAsync(new Uri($"/charts/{Charts.All[0].Id}", UriKind.Relative), Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No lab run is recorded", benchmarks, StringComparison.Ordinal);
        Assert.Contains("No lab run is recorded", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ccc3333", benchmarks + page, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "DEMO-08")]
    public async Task PagesThatDrawChartsReportTheDrawingTimeAsync()
    {
        using var site = new DemoSite();
        var client = site.Client();

        foreach (var path in new[] { "/", $"/charts/{Charts.All[0].Id}" })
        {
            var response = await client.GetAsync(new Uri(path, UriKind.Relative), Cancellation);
            Assert.Matches(ServerTiming(), Assert.Single(response.Headers.GetValues("Server-Timing")));
        }
    }

    private static string Row(string html, string id)
    {
        var match = Regex.Match(html, $"<tr data-chart=\"{Regex.Escape(id)}\">(.*?)</tr>", RegexOptions.Singleline, TimeSpan.FromSeconds(1));
        Assert.True(match.Success, $"no summary row for {id}");
        return match.Groups[1].Value;
    }

    private static string[] Verdicts(string row) =>
        [.. VerdictCell().Matches(row).Select(match => match.Groups[1].Value)];

    private static int Count(string text, string part)
    {
        var count = 0;
        for (var at = text.IndexOf(part, StringComparison.Ordinal); at >= 0; at = text.IndexOf(part, at + part.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    [GeneratedRegex("<td class=\"verdict\"><span class=\"[a-z]+\">([a-z ]+)</span></td>")]
    private static partial Regex VerdictCell();

    [GeneratedRegex(@"^draw;dur=\d+(\.\d+)?$")]
    private static partial Regex ServerTiming();
}
