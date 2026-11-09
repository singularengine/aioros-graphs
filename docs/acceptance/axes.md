# Axes

d3-axis as data (`Axis`, `AxisLayout`) and as SVG markup (`SvgAxisWriter`): the tick positions, labels, offsets,
tick lines and domain path d3-axis computes, without a DOM. Golden fixture: `Golden/axis.json`, from
`tools/golden/cases/axis.mjs`, which renders d3-axis into a minimal DOM and records the markup it builds (the axis's
`<g>` serialized as a browser's `outerHTML`), the domain path, each tick's transform and each label.

| ID | Criterion |
|---|---|
| AXIS-01 | Given each orientation, linear scale (ascending, descending, piecewise and equal bounds), tick arguments (none, a count, a count and a specifier) and setting (tick sizes inner and outer, zero and negative, padding, offset 0 and fractional) of the `linear` cases, when `Axis.Create` lays the axis out and `SvgAxisWriter` writes it, then the domain path, each tick's transform and label, and the whole markup equal d3-axis's byte for byte. |
| AXIS-02 | Given each orientation and list of explicit tick values of the `values` cases (empty and with NaN), when the axis is laid out from those values and the scale's tick format, then the domain path, transforms, labels and markup equal d3's `axis.tickValues(values)`. |
| AXIS-03 | Given each orientation, band scale (padding, inner and outer padding, align, rounded and reversed ranges) and key list (six keys, one and none) of the `band` cases, when the axis is laid out from the scale and its keys, then each tick sits at the band's center (rounded when the scale rounds) and the domain path, transforms, labels and markup equal d3's. |
| AXIS-04 | Given each orientation and point scale of the `point` cases (padding, rounding, reversed range), when the axis is laid out from the scale and its keys, then the domain path, transforms, labels and markup equal d3's. |
| AXIS-05 | Given each orientation, UTC domain (minutes to decades) and tick arguments of the `utc` cases (none, a count, a count and a specifier), when the axis is laid out from a `UtcScale`, then the ticks, the multi-scale or specified labels, the domain path and the markup equal d3's. |
| AXIS-06 | Given each layout of the `linear` cases, when `AxisLayout.WriteLines` writes it to a path sink, then the sink receives the domain as d3's domain path with its H and V commands drawn as lines, followed by one line per tick from the axis to the tick's end (`k × tickSizeInner`). |
| AXIS-07 | Given `Axis.Create` with explicit values, when the positions or the labels are fewer than the values, or the range has fewer than one stop, then an `ArgumentException` is thrown. |
| AXIS-08 | Given each orientation, scale (log in base 10 and 2 with positive and negative domains, sqrt, pow with exponent 3, symlog) and tick arguments of the `continuous` cases, when `Axis.Create` lays out a `LogScale`, `PowScale` or `SymlogScale` axis, then the ticks, the labels (the log format hiding the labels d3 hides), the domain path and the markup equal d3's. |
| AXIS-09 | Given each time zone (Berlin, São Paulo, Kolkata), orientation, local domain (a year, a day across a daylight-saving change, two weeks across another, three decades) and tick arguments of the `time` cases, when `Axis.Create` lays out a `TimeScale` in that zone, then the local ticks, the local multi-scale or specified labels, the domain path and the markup equal d3-axis's over `scaleTime` in a process running in that zone. |
