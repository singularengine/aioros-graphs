# Chords

d3-chord's layouts and ribbons (`ChordLayout`, `Ribbon`, `RibbonShape`). Golden fixtures: `Golden/chord.json` from
`tools/golden/cases/chord.mjs` (d3-chord 3.0.1, the version d3 7.9.0 bundles). Angles and values are compared bit for
bit. d3's ribbons are drawn into an unrounded `path()` and their ends go through sines and cosines, so the unrounded
path data is compared within a relative 1e-12 per number; drawn through `pathRound(3)` they are
compared byte for byte.

| ID | Criterion |
|---|---|
| CHORD-01 | Given each matrix of the `chords` cases (d3's four-group example, a 12 × 12 matrix with ties, self-loops, an empty row, an all-zero matrix, a single group, no group, a NaN and a negative entry, an asymmetric matrix) and each setting (pad angles 0, 0.05, 2, -1 and NaN; groups, subgroups and chords unsorted, ascending and descending), when `ChordLayout.Compute` runs undirected, then every group's index, start angle, end angle and value and every chord's source and target equal d3's `chord()`, in d3's order: empty chords removed, a pair's larger end as its source, ties in their input order. |
| CHORD-02 | Given the same matrices and settings, when `ChordLayout.Compute` runs with `Directed`, then the groups and chords equal d3's `chordDirected()`: each group's incoming subgroups (last group first) before its outgoing ones, sorted by d3's signed values when subgroups are sorted. |
| CHORD-03 | Given the same matrices and settings, when `ChordLayout.Compute` runs with `Transpose`, then the groups and chords equal d3's `chordTranspose()`. |
| CHORD-04 | Given a chord layout, when the matrix does not hold exactly `n` × `n` values or `n` is negative, then an `ArgumentException` is thrown. |
| CHORD-05 | Given each ribbon of the `ribbons` cases (every chord of three layouts at radii 100 and 240.5, separate source and target radii, pad angles that shrink and that collapse an end, ends that coincide, reversed angles), when `Ribbon.Write` writes it, then the path data equals d3's `ribbon()` within a relative 1e-12 unrounded and byte for byte through `pathRound(3)`, and several ribbons written into one path equal d3's ribbons drawn on one context. |
| CHORD-06 | Given the same ribbons and head radii 10 (d3's default), 0 and 25, when `Ribbon.WriteArrow` writes them, then the path data equals d3's `ribbonArrow().headRadius(r)` within a relative 1e-12 unrounded and byte for byte through `pathRound(3)`. |
