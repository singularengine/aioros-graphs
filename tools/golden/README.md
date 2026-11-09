# Golden fixtures

The C# tests compare the engine with d3 itself. `generate.mjs` runs every module in `cases/` against the d3 version
pinned in `package.json` and writes one JSON file per module to `tests/Aioros.Graphs.Tests/Golden/`. The fixtures
are committed, so building and testing need no Node; regenerate them only when a case or the d3 version changes:

```
cd tools/golden
npm ci
npm run generate
```

Each module exports a default function returning `{ group: [cases] }`. Numbers that JSON cannot hold are written as
strings: `"NaN"`, `"Infinity"`, `"-Infinity"` and `"-0"`. Time values are milliseconds since the Unix epoch, as
JavaScript's `Date` holds them.
