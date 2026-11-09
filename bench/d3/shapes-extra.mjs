import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync, writeFileSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const d3 = {...require("d3-shape"), ...require("d3-scale"), ...require("d3-path")};

const points = 10_000, marks = 1_000;
const values = [], index = [];
let walk = 100;
for (let i = 0; i < points; i++) {
  walk += Math.sin(i * 0.37) * 0.8 + Math.cos(i * 0.011) * 0.3;
  values.push(walk);
  index.push(i);
}

const min = Math.min(...values), max = Math.max(...values);
const xScale = d3.scaleLinear([0, points - 1], [0, 960]);
const yScale = d3.scaleLinear([min, max], [500, 0]).nice();
const aScale = d3.scaleLinear([0, points], [0, 2 * Math.PI]);
const rScale = d3.scaleLinear([min, max], [50, 250]);
const x = index.map((i) => xScale(i)), y = values.map((v) => yScale(v));
const data = index.map((i) => [x[i], y[i]]);
const polar = index.map((i) => [aScale(i), rScale(values[i])]);
const types = [d3.symbolCircle, d3.symbolTriangle, d3.symbolStar, d3.symbolCross];

function translated(context, dx, dy) {
  return {
    moveTo: (a, b) => context.moveTo(a + dx, b + dy),
    lineTo: (a, b) => context.lineTo(a + dx, b + dy),
    closePath: () => context.closePath(),
    arc: (a, b, r, a0, a1, ccw) => context.arc(a + dx, b + dy, r, a0, a1, ccw),
    rect: (a, b, w, h) => context.rect(a + dx, b + dy, w, h),
  };
}

const cases = {
  Symbols1000ToSvg: () => {
    const context = d3.pathRound(3);
    for (let i = 0; i < marks; i++) types[i & 3].draw(translated(context, x[i * 10], y[i * 10]), 64);
    return (context + "").length;
  },
  LineRadial10kToSvg: () => d3.lineRadial()(polar).length,
  LinksHorizontal1000ToSvg: () => {
    const context = d3.pathRound(3);
    const link = d3.linkHorizontal().context(context);
    for (let i = 0; i < marks; i++) link({source: [x[i * 10], y[i * 10]], target: [x[i * 10] + 80, y[(i * 10 + 5000) % points]]});
    return (context + "").length;
  },
  LineBasisClosed10kToSvg: () => d3.line().curve(d3.curveBasisClosed)(data).length,
  LineCatmullRomClosed10kToSvg: () => d3.line().curve(d3.curveCatmullRomClosed)(data).length,
  LineBundle10kToSvg: () => d3.line().curve(d3.curveBundle)(data).length,
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
  console.log(`${name.padEnd(30)} ${samples[3].toFixed(1).padStart(14)} ns (median of 7, ${iterations} iterations each)`);
}

if (process.argv[2]) {
  load.push(loadavg()[0]);
  const version = JSON.parse(readFileSync(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "node_modules", "d3", "package.json"), "utf8")).version;
  writeFileSync(process.argv[2], JSON.stringify({runtime: `d3 ${version} on Node ${process.version}`, machine: `${cpus()[0].model}, ${cpus().length} threads`, date: new Date().toISOString(), loadAverage: load, results}, null, 1));
}
