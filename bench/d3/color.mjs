import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync, writeFileSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const d3 = {...require("d3-color"), ...require("d3-interpolate"), ...require("d3-scale-chromatic")};

const samples = 1000;
const specifiers = ["steelblue", "#4682b4", "#fff", "rgb(70, 130, 180)", "rgba(70, 130, 180, 0.5)", "hsl(207, 44%, 49%)",
  "rebeccapurple", "#ff000080", "rgb(10%, 20%, 30%)", "transparent", "  Navy ", "hsla(120, 100%, 25%, 0.3)"];
const rgb = d3.interpolateRgb("steelblue", "brown");
const hcl = d3.interpolateHcl("steelblue", "brown");

const cases = {
  Parse12: () => { let s = 0; for (const c of specifiers) s += d3.color(c).opacity; return s; },
  RgbInterpolate1000ToRgb: () => { let n = 0; for (let i = 0; i < samples; i++) n += rgb(i / (samples - 1)).length; return n; },
  HclInterpolate1000ToRgb: () => { let n = 0; for (let i = 0; i < samples; i++) n += hcl(i / (samples - 1)).length; return n; },
  Viridis1000: () => { let n = 0; for (let i = 0; i < samples; i++) n += d3.interpolateViridis(i / (samples - 1)).length; return n; },
  RdBu1000: () => { let n = 0; for (let i = 0; i < samples; i++) n += d3.interpolateRdBu(i / (samples - 1)).length; return n; },
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
  const times = [];
  for (let k = 0; k < 7; k++) {
    const start = process.hrtime.bigint();
    for (let i = 0; i < iterations; i++) run();
    times.push(Number(process.hrtime.bigint() - start) / iterations);
  }
  times.sort((a, b) => a - b);
  results.push({name, medianNs: times[3], iterations, samples: 7});
  console.log(`${name.padEnd(24)} ${times[3].toFixed(1).padStart(14)} ns (median of 7, ${iterations} iterations each)`);
}

if (process.argv[2]) {
  load.push(loadavg()[0]);
  const version = JSON.parse(readFileSync(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "node_modules", "d3", "package.json"), "utf8")).version;
  writeFileSync(process.argv[2], JSON.stringify({runtime: `d3 ${version} on Node ${process.version}`, machine: `${cpus()[0].model}, ${cpus().length} threads`, date: new Date().toISOString(), loadAverage: load, results}, null, 1));
}
