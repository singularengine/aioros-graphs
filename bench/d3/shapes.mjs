import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync, writeFileSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const d3 = {...require("d3-shape"), ...require("d3-scale")};

const points = 10_000;
const values = [], index = [];
let walk = 100;
for (let i = 0; i < points; i++) {
  walk += Math.sin(i * 0.37) * 0.8 + Math.cos(i * 0.011) * 0.3;
  values.push(walk);
  index.push(i);
}

const x = d3.scaleLinear([0, points - 1], [0, 960]);
const y = d3.scaleLinear([Math.min(...values), Math.max(...values)], [500, 0]).nice();
const data = index.map((i) => [x(i), y(values[i])]);
const pieValues = [12, 7, 3.5, 22, 9, 1.25, 15, 4, 6, 18, 2, 11];
const arc = d3.arc().innerRadius(60).outerRadius(100).cornerRadius(4);

const cases = {
  LinearTicks: () => y.ticks(10).length,
  LinearMap10k: () => { let s = 0; for (let i = 0; i < points; i++) s += y(values[i]); return s; },
  LineLinear10kToSvg: () => d3.line()(data).length,
  LineMonotoneX10kToSvg: () => d3.line().curve(d3.curveMonotoneX)(data).length,
  AreaNatural10kToSvg: () => d3.area().curve(d3.curveNatural).y0(500)(data).length,
  PieAndArcs12ToSvg: () => d3.pie().padAngle(0.02)(pieValues).reduce((n, s) => n + arc(s).length, 0),
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
