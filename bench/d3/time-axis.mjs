import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync, writeFileSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const d3 = {...require("d3-scale"), ...require("d3-time-format")};

const start = Date.UTC(2024, 0, 1), yearEnd = Date.UTC(2025, 0, 1), twoYearsEnd = Date.UTC(2025, 11, 31);
const year = d3.scaleUtc([start, yearEnd], [0, 960]);
const twoYears = d3.scaleUtc([start, twoYearsEnd], [0, 960]);
const yearTicks = year.ticks(8);
const multi = year.tickFormat();
const localYear = d3.scaleTime([start, yearEnd], [0, 960]);
const localTicks = localYear.ticks(8);
const localMulti = localYear.tickFormat();
const linear = d3.scaleLinear([0, 1234], [0, 960]);
const linearFormat = linear.tickFormat(10);

const cases = {
  UtcTicksYear8: () => year.ticks(8).length,
  UtcTicksTwoYears10: () => twoYears.ticks(10).length,
  MultiScaleLabels13: () => { let n = 0; for (const t of yearTicks) n += multi(t).length; return n; },
  LocalTicksYear8: () => localYear.ticks(8).length,
  LocalMultiScaleLabels: () => { let n = 0; for (const t of localTicks) n += localMulti(t).length; return n; },
  UtcFormatCreate: () => d3.utcFormat("%Y-%m-%d"),
  LinearTickFormatCreate: () => linear.tickFormat(10),
  LinearTickFormatCreateSi: () => linear.tickFormat(10, ".2~s"),
  LinearTickFormatCreateCurrency: () => linear.tickFormat(10, "$,f"),
  LinearTickLabel: () => linearFormat(1200).length,
};

const results = [];
const load = [loadavg()[0]];
for (const [name, run] of Object.entries(cases)) {
  let iterations = 1, elapsed = 0;
  for (;;) {
    const begin = process.hrtime.bigint();
    for (let i = 0; i < iterations; i++) run();
    elapsed = Number(process.hrtime.bigint() - begin);
    if (elapsed > 2e8) break;
    iterations *= 2;
  }
  const samples = [];
  for (let k = 0; k < 7; k++) {
    const begin = process.hrtime.bigint();
    for (let i = 0; i < iterations; i++) run();
    samples.push(Number(process.hrtime.bigint() - begin) / iterations);
  }
  samples.sort((a, b) => a - b);
  results.push({name, medianNs: samples[3], iterations, samples: 7});
  console.log(`${name.padEnd(32)} ${samples[3].toFixed(1).padStart(14)} ns (median of 7, ${iterations} iterations each)`);
}

if (process.argv[2]) {
  load.push(loadavg()[0]);
  const version = JSON.parse(readFileSync(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "node_modules", "d3", "package.json"), "utf8")).version;
  writeFileSync(process.argv[2], JSON.stringify({runtime: `d3 ${version} on Node ${process.version}`, timeZone: process.env.TZ ?? null, machine: `${cpus()[0].model}, ${cpus().length} threads`, date: new Date().toISOString(), loadAverage: load, results}, null, 1));
}
