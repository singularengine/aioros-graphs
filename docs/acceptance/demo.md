# Demo site

The demo web app (`samples/Demo`, an ASP.NET Core app with no dependency beyond the engine and its SVG writer). Every
page draws its charts with the engine on the request it answers and compares them with d3's output for the same data
(the reference, `out/d3.json`, written by `samples/Demo/d3.mjs`). The timings it shows are the recorded lab runs
under `bench/results`, never measured on a request.

| ID | Criterion |
|---|---|
| DEMO-01 | Given the site with a reference that matches the engine, when `/` is requested, then it answers 200 with an HTML page that links every chart of `Charts.All` to `/charts/{id}` and states, per chart, path data, tick labels and whole SVG `identical`, compared on that request. |
| DEMO-02 | Given the site with a reference that matches the engine and a recorded lab run, when `/charts/{id}` is requested for a chart, then the page shows the SVG the engine drew on that request beside the reference's SVG, states the whole SVG `byte-identical`, and gives that chart's engine and d3 medians from the latest lab run. |
| DEMO-03 | Given a reference in which one path of a chart differs from the engine's, when that chart's page is requested, then it states how many of its paths are identical (`n of m paths identical`) and the whole SVG `different`; `/` marks that chart's path data and whole SVG `different`. |
| DEMO-04 | Given an id that names no chart, when `/charts/{id}` is requested, then the site answers 404. |
| DEMO-05 | Given no reference file, when `/` or a chart page is requested, then the page answers 200 with the engine's charts, every verdict reads `not compared`, and it says how to write the reference (`samples/Demo/serve.sh`). |
| DEMO-06 | Given a results folder with two lab runs and a newer workstation run (a name ending in `-workstation`), when `/benchmarks` is requested, then it shows the newest lab run only: its micro-benchmarks (engine median, bytes allocated, d3 median, d3 ÷ engine and the geometric mean of the paired ones), its charts' medians and ratios, the machine, the runtimes and the load averages. |
| DEMO-07 | Given a results folder without lab runs, when `/benchmarks` is requested, then it answers 200 and states that no lab run is recorded; a chart page states the same instead of medians. |
| DEMO-08 | Given any page that draws charts, when it is answered, then the response carries a `Server-Timing` header `draw;dur=<milliseconds>` with the time the engine spent drawing that page's charts. |
