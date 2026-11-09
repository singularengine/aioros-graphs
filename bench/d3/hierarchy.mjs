import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync, writeFileSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const d3 = require("d3-hierarchy");

const nodes = 1_000;
const rows = [{id: 0, parentId: null}];
const values = [0];
for (let i = 1; i < nodes; i++) {
  rows.push({id: i, parentId: (Math.imul(i, 2654435761 | 0) >>> 0) % i});
  values.push(1 + i * 37 % 100);
}

const build = () => d3.stratify()(rows).sum((d) => values[d.id]).sort((a, b) => b.value - a.value);
const root = build();
const last = (node) => node.descendants().find((d) => d.data.id === nodes - 1);
const target = last(root);

const cases = {
  StratifySumSort1000: () => build().height,
  Tree1000: () => (d3.tree().size([960, 500])(root), target.x),
  Cluster1000: () => (d3.cluster().size([960, 500])(root), target.x),
  TreemapSquarify1000: () => (d3.treemap().size([960, 500]).paddingInner(1).round(true)(root), target.x0),
  TreemapBinary1000: () => (d3.treemap().size([960, 500]).tile(d3.treemapBinary)(root), target.x0),
  Pack1000: () => (d3.pack().size([960, 960]).padding(2)(root), target.r),
  Partition1000: () => (d3.partition().size([2 * Math.PI, 300])(root), target.x1),
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
