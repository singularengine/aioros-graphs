# Benchmarks

How Aioros.Graphs compares with d3 7.9.0 when both do the same work, and the latest results.

## What is measured

- **Micro-benchmarks.** `bench/Aioros.Graphs.Benchmarks` uses BenchmarkDotNet to measure shapes, symbols, time axes, scales, colors and layouts. The scripts in `bench/d3` measure d3 on Node with the same data.
- **Charts.** `samples/Demo` draws nineteen d3 gallery charts from the same data in both engines, checks that the outputs are byte-identical and times each one.

Charts time everything a chart computes: scales, ticks, labels, stacks and path strings. Both runtimes are pinned to the same four CPU cores, and each result is the median of several samples after a warm-up.

## Latest results

Measured on 2026-10-13, on a self-hosted machine with .NET 10 and Node 22. Every chart was byte-identical to d3.

| Chart | Aioros.Graphs | d3 | Times faster |
| --- | --- | --- | --- |
| Line with a time axis, 520 points | 135 µs | 199 µs | 1.5× |
| Stacked area, 5 × 36 | 41.2 µs | 191 µs | 4.6× |
| Stacked bars, 4 × 8 | 9.7 µs | 38.7 µs | 4.0× |
| Donut, 7 slices | 20.3 µs | 34.6 µs | 1.7× |
| Streamgraph, 8 × 100 | 388 µs | 1204 µs | 3.1× |
| Histogram, 1,000 values | 12.0 µs | 46.6 µs | 3.9× |
| Log-scale line with axis markup | 83.8 µs | 498 µs | 5.9× |
| Treemap, 108 leaves | 64.4 µs | 103 µs | 1.6× |
| Sankey diagram | 65.0 µs | 184 µs | 2.8× |
| Force graph, 60 nodes, 300 ticks | 8709 µs | 28888 µs | 3.3× |
| **Geometric mean of all nineteen** | | | **2.7×** |

The engine is not always faster. Writing chord ribbons without rounding takes 93.8 µs against d3's 56.1 µs.

## Running it

```bash
bench/run.sh micro      # the micro-benchmarks
bench/run.sh demo       # the charts
bench/run.sh all        # both
```

Results are written under `bench/results`. On a machine doing other work, compare ratios and not absolute times.
