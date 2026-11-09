# Sankey

d3-sankey's layout and link shape (`SankeyLayout.Compute`, `SankeyLayout.WriteLink`). Golden fixtures:
`Golden/sankey.json` from `tools/golden/cases/sankey.mjs`, run against d3-sankey 0.12.3 (with the d3-array 2.12.1 and
d3-shape 1.3.7 it depends on). Every coordinate is compared bit for bit and every path byte for byte.

| ID | Criterion |
|---|---|
| SANKEY-01 | Given each graph of the `layouts` cases (a generated energy-like graph of 48 nodes, small graphs, several sources and sinks, links skipping columns, isolated nodes, zero and NaN link values, fixed node values, a graph with no links) and each setting (the default extent and a custom one, node widths and paddings, the alignments justify, left, right and center, 0, 1, 6 and 32 iterations), when `SankeyLayout.Compute` lays it out with nodes given by index, then each node's `X0`, `X1`, `Y0`, `Y1`, `Value`, `Depth`, `Height` and `Layer` and each link's `Y0`, `Y1` and `Width` equal d3's `sankey()` bit for bit (NaN where d3 gives NaN), and each node's source and target links come in d3's order. |
| SANKEY-02 | Given the graphs of the `ids` cases, whose nodes have string ids (one id given twice) and whose links name their ends by id, when `SankeyLayout.Compute` lays them out with those ids, then the layout equals d3's `sankey().nodeId(d => d.id)`, a repeated id naming its last node as d3's map does. |
| SANKEY-03 | Given the graphs and node orders of the `nodeSorts` cases (input order, d3's `nodeSort(null)`, and a comparison by descending value and by descending index), when `SankeyLayout.Compute` lays them out, then the layout equals d3's with that `nodeSort`: a comparison sorts each column once and neither keeps the columns sorted by breadth during relaxation. |
| SANKEY-04 | Given the graphs and link orders of the `linkSorts` cases (input order, d3's `linkSort(null)`, and a comparison by descending value), when `SankeyLayout.Compute` lays them out, then the layout and each node's link order equal d3's with that `linkSort`, the links never being reordered by breadth. |
| SANKEY-05 | Given every link of the `layouts` cases, when `SankeyLayout.WriteLink` writes it through an unrounded `SvgPathWriter`, then the path data equals d3-sankey's `sankeyLinkHorizontal()` byte for byte, and links written one after another through one `PathContext` equal d3's links drawn on one context. |
| SANKEY-06 | Given a link whose source or target names no node (an index out of range, a negative index, or an unknown string id), when `SankeyLayout.Compute` runs, then an `ArgumentException` is thrown with d3's message `missing: <id>`. |
| SANKEY-07 | Given the graphs of the `cycles` cases (a self link, a two-node cycle, a cycle behind a source), when `SankeyLayout.Compute` runs, then an `ArgumentException` is thrown with d3's message `circular link`, as d3 throws. |
| SANKEY-08 | Given links whose source, target and value spans differ in length, ids that are not one per node, fixed values that are neither empty nor one per node, or no nodes at all (where d3 fails creating its columns), when `SankeyLayout.Compute` runs, then an `ArgumentException` is thrown. |
