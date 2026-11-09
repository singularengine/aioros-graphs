import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync, writeFileSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const d3 = {...require("d3-time-format"), ...require("d3-interpolate"), ...require("d3-array")};

const dates = 1_000, rows = 10_000, start = 1_577_836_800_000;
const iso = d3.utcFormat("%Y-%m-%dT%H:%M:%SZ"), wall = d3.utcFormat("%Y-%m-%d %H:%M");
const utcTexts = [], localTexts = [];
for (let i = 0; i < dates; i++) {
  utcTexts.push(iso(new Date(start + i * 3_600_123)));
  localTexts.push(wall(new Date(start + i * 3_600_123)));
}

const data = [];
for (let i = 0; i < rows; i++) data.push({category: "c" + (i * 7 % 12), subcategory: "s" + (i * 13 % 7), value: i * 37 % 101 / 7});
const values = data.map((d) => d.value);

const utc = d3.utcParse("%Y-%m-%dT%H:%M:%SZ");
const local = d3.timeParse("%Y-%m-%d %H:%M");
const path = d3.interpolateString("M0,0L10,10C1,2,3,4,5,6Z", "M5,5L20,30C7,8,9,10,11,12Z");

const cases = {
  UtcParse1000: () => {
    let total = 0;
    for (const t of utcTexts) total += +utc(t);
    return total;
  },
  LocalParse1000: () => {
    let total = 0;
    for (const t of localTexts) total += +local(t);
    return total;
  },
  InterpolateString1000: () => {
    let total = 0;
    for (let i = 0; i < 1000; i++) total += path(i / 999).length;
    return total;
  },
  NestedRollup10k: () => d3.rollup(data, (v) => d3.sum(v, (d) => d.value), (d) => d.category, (d) => d.subcategory).size,
  FCumulativeSum10k: () => d3.fcumsum(values)[rows - 1],
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
  console.log(`${name.padEnd(30)} ${samples[3].toFixed(1).padStart(14)} ns (median of 7, ${iterations} iterations each)`);
}

if (process.argv[2]) {
  load.push(loadavg()[0]);
  const version = JSON.parse(readFileSync(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "node_modules", "d3", "package.json"), "utf8")).version;
  writeFileSync(process.argv[2], JSON.stringify({runtime: `d3 ${version} on Node ${process.version}`, machine: `${cpus()[0].model}, ${cpus().length} threads`, date: new Date().toISOString(), loadAverage: load, results}, null, 1));
}
