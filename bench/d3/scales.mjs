import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync, writeFileSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const d3 = {...require("d3-scale"), ...require("d3-array")};

const points = 10_000;
const values = [];
let walk = 100;
for (let i = 0; i < points; i++) {
  walk += Math.sin(i * 0.37) * 0.8 + Math.cos(i * 0.011) * 0.3;
  values.push(walk);
}

const log = d3.scaleLog([1, 1e6], [0, 960]);
const pow = d3.scalePow([Math.min(...values), Math.max(...values)], [500, 0]).exponent(2);
const bin = d3.bin();

const cases = {
  LogTicks: () => log.ticks(10).length,
  LogTicksAndLabels: () => { const format = log.tickFormat(10); return log.ticks(10).reduce((n, t) => n + format(t).length, 0); },
  LogMap10k: () => { let s = 0; for (let i = 0; i < points; i++) s += log(values[i]); return s; },
  PowMap10k: () => { let s = 0; for (let i = 0; i < points; i++) s += pow(values[i]); return s; },
  BinSturges10k: () => bin(values).length,
  Quantile10k: () => d3.quantile(values, 0.9),
};

const results = [];
const load = [loadavg()[0]];
for (const [name, run] of Object.entries(cases)) {
  let iterations = 1, elapsed = 0;
  for (;;) {
    const start = process.hrtime.bigint();
    for (let i = 0; i < iterations; i++) run();
    elapsed = Number(process.hrtime.bigint() - start);
    if (elapsed > 2e8) break;
    iterations *= 2;
  }
  const samples = [];
  for (let k = 0; k < 7; k++) {
    const start = process.hrtime.bigint();
    for (let i = 0; i < iterations; i++) run();
    samples.push(Number(process.hrtime.bigint() - start) / iterations);
  }
  samples.sort((a, b) => a - b);
  results.push({name, medianNs: samples[3], iterations, samples: 7});
  console.log(`${name.padEnd(24)} ${samples[3].toFixed(1).padStart(14)} ns (median of 7, ${iterations} iterations each)`);
}

if (process.argv[2]) {
  load.push(loadavg()[0]);
  const version = JSON.parse(readFileSync(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "node_modules", "d3", "package.json"), "utf8")).version;
  writeFileSync(process.argv[2], JSON.stringify({runtime: `d3 ${version} on Node ${process.version}`, machine: `${cpus()[0].model}, ${cpus().length} threads`, date: new Date().toISOString(), loadAverage: load, results}, null, 1));
}
