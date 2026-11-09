import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync, writeFileSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const d3 = require("d3-force");

const graphNodes = 77, bodyCount = 1000;
const links = [];
for (let i = 1; i < graphNodes; i++) links.push([i * 2654435761 % 4294967296 % i, i]);
for (let k = 0; k < 178; k++) {
  const a = (k * 17 + 3) % graphNodes, b = (k * 29 + 11) % graphNodes;
  links.push([a, a === b ? (b + 1) % graphNodes : b]);
}

const bodies = Array.from({length: bodyCount}, () => ({}));
const charge = d3.forceSimulation(bodies).force("charge", d3.forceManyBody()).stop();

const cases = {
  Graph77Links254Ticks300: () => {
    const nodes = Array.from({length: graphNodes}, () => ({}));
    const simulation = d3.forceSimulation(nodes)
      .force("link", d3.forceLink(links.map(([source, target]) => ({source, target}))))
      .force("charge", d3.forceManyBody())
      .force("center", d3.forceCenter())
      .stop();
    simulation.tick(300);
    return nodes[0].x;
  },
  ManyBody1000Tick: () => {
    charge.tick();
    return bodies[0].x;
  },
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
