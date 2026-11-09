import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync, writeFileSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const {sankey, sankeyLinkHorizontal} = require("d3-sankey");

const layers = [8, 10, 12, 10, 8, 2];
const links = [];
const seen = new Set();
let start = 0;
for (let l = 0; l < layers.length - 1; l++) {
  const next = start + layers[l];
  for (let node = start; node < next; node++) {
    for (let k = 0; k < 3; k++) {
      const target = next + (k * 7 + node * 3) % layers[l + 1];
      if (seen.has(node * 1000 + target)) continue;
      seen.add(node * 1000 + target);
      links.push([node, target, 1 + (node * 13 + k * 29) % 50]);
    }
  }
  start = next;
}
const nodes = start + layers[layers.length - 1];

const generator = sankey().extent([[1, 5], [959, 495]]).nodeWidth(15).nodePadding(10);
const shape = sankeyLinkHorizontal();
const graph = () => ({
  nodes: Array.from({length: nodes}, () => ({})),
  links: links.map(([source, target, value]) => ({source, target, value})),
});

const cases = {
  SankeyLayout50: () => generator(graph()).links.length,
  SankeyLayoutAndLinks50ToSvg: () => {
    let length = 0;
    for (const link of generator(graph()).links) length += shape(link).length;
    return length;
  },
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
  const version = JSON.parse(readFileSync(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "node_modules", "d3-sankey", "package.json"), "utf8")).version;
  writeFileSync(process.argv[2], JSON.stringify({runtime: `d3-sankey ${version} on Node ${process.version}`, machine: `${cpus()[0].model}, ${cpus().length} threads`, date: new Date().toISOString(), loadAverage: load, results}, null, 1));
}
