# Working on this repository

Guidance for every agent and person working on Aioros.Graphs, the chart engine of Aioros. It follows Aioros's rules
(`aioros/AGENTS.md`, `aioros/docs/principles.md`), adapted to a library that ports d3. The rules are enforced by the
architecture tests and the analyzers wherever they can be.

## Before you change anything

1. Read the [README](README.md) and the [roadmap](docs/roadmap.md).
2. Build and test. Both must be green before and after your change. The machine may be shared: build with `-m:4`.
3. Do not reinvent and do not assume. The reference is d3's source and its output: read the module you port
   (`tools/golden/node_modules/d3-*/src`), port it operation for operation (the same floating-point operations in the
   same order, so results match bit for bit), and note any divergence and its reason in its acceptance criterion.

```
dotnet build Aioros.Graphs.slnx -m:4
dotnet test --solution Aioros.Graphs.slnx
```

The SDK is pinned in `global.json`; `eng/install-sdk.sh` installs it into `.dotnet/`. CI's `dependencies` workflow
fails when that SDK lags a security release of .NET or a package has a known advisory (`eng/check-dependencies.py`,
as Aioros's).

## Writing tests

Tests come before the code they verify.

1. **Write the acceptance criteria first**, in the area's file in [docs/acceptance](docs/acceptance), as a table row
   `| AREA-NN | criterion |`. The identifier is never reused. Each criterion describes one observable behavior in
   Given / When / Then form, names the expected outcome precisely, and covers the rejected paths too.
2. **Check for duplicates** in the acceptance files and the tests; extend an existing test instead of adding one.
3. **Generate the golden cases from d3** when the behavior is d3's: add or extend a module in `tools/golden/cases`,
   then `cd tools/golden && npm ci && npm run generate`, and commit the JSON it writes to
   `tests/Aioros.Graphs.Tests/Golden`. Compare exactly; a tolerance needs a reason in its acceptance criterion.
4. **One test per criterion**, carrying it: `[Trait("AC", "SHAPE-01")]`. The architecture tests fail on a criterion
   without exactly one test and on a test without exactly one known criterion. The test name states the behavior in
   PascalCase, with the `Async` suffix for asynchronous tests.
5. Write the test first and watch it fail (a stub throwing `NotImplementedException` is fine), then write the code.
6. No sleeps, no waits on time. Prove a new rule can fail: break it on purpose, watch the test fail, restore it.

## Architecture

- `src/Aioros.Graphs` references no package and no project: pure math and geometry.
- Only `Aioros.Graphs.Avalonia` (and the tests) reference Avalonia; the version is Aioros's (12.1.3).
- **Composition over inheritance.** Every class and record class is `sealed` or `static`; benchmark classes are the
  exception BenchmarkDotNet forces.
- **No locks, no concurrent collections, no reflection** in `src/`. Scales are immutable; shapes are static and
  stateless.
- **Performance is the point.** On hot paths: structs, spans, generic specialization over struct sinks and curves,
  pooled buffers (`ArrayPool`), `stackalloc` for small scratch. No LINQ, no closures, no boxing, no allocation per
  point. Measure with `bench/Aioros.Graphs.Benchmarks` (`[MemoryDiagnoser]`) and compare with `bench/d3`.
- **Screaming architecture.** Folders and namespaces name what the code does: `Arrays`, `Scales`, `Shapes`, `Paths`,
  `Formatting`, `Time`.

## Code

- Modern C# 14 on .NET 10. The smallest code that states the behavior clearly.
- No comments. XML documentation on the public API only (required: the projects generate documentation).
- No type longer than 600 lines; Sonar's cognitive complexity (15), nesting (3) and method length (80) limits apply.
- Warnings are errors.

## Documentation

- Everything in the repository is written in English.
- Update the roadmap in the same change when something it says changes, and keep THIRD-PARTY-NOTICES.md in step with what is ported.
- **Branding.** Product surfaces (READMEs, Galleries, demos, page metadata, package and template descriptions) lead
  with the ecosystem's brands: Aioros ("The XAML framework for web and native apps"), Aioros.Cosmo, Aioros.Pictor,
  Aioros.Graphs. Avalonia is named once, as a compatibility statement where it helps a developer; shadcn/ui, Recharts
  and d3 appear as the reference on fidelity and compare pages and in the credits; comparisons say "native" or "desktop
  render". Internal technical documents, code and type names keep their names.
