# Paths

d3-path's canvas-like API (`PathContext`) and the sinks that receive its commands: SVG path data (`SvgPathWriter`) and
Avalonia geometry (`GeometrySink`, over a `StreamGeometryContext` or any `IGeometryContext`). Golden fixture: `Golden/path.json`, from `tools/golden/cases/path.mjs`.

| ID | Criterion |
|---|---|
| PATH-01 | Given each program of the `programs` cases (moves, lines, quadratic and cubic curves, center arcs in both directions, full circles, empty and zero-radius arcs, tangent arcs, rectangles and closes, with and without a current point), when it runs on a `PathContext` over an `SvgPathWriter` with the case's digits (none, 3 or 0), then the UTF-8 path data equals d3's `path()` or `pathRound(digits)` output byte for byte. |
| PATH-02 | Given a `PathContext`, when `Arc` or `ArcTo` is called with a negative radius, then an `ArgumentOutOfRangeException` is thrown and nothing is written, as d3 throws "negative radius". |
| PATH-03 | Given an output buffer, when an `SvgPathWriter` is created with digits below 0 or above 15, then an `ArgumentOutOfRangeException` is thrown. |
| PATH-04 | Given a path with a move, line, quadratic and cubic curves, an arc with each sweep, a close followed by a line, and a rectangle, when it is drawn through a `GeometrySink` and completed, then the Avalonia geometry context receives filled figures begun at each move, the matching `LineTo`, `QuadraticBezierTo`, `CubicBezierTo` and `ArcTo` calls (sweep 1 as `Clockwise`), a closed end at each close, a new figure at the subpath start for a line after a close, the rectangle as a closed four-sided figure, and an open end when completed. |
