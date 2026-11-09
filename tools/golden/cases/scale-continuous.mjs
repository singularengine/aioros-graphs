import {scaleLog, scalePow, scaleSqrt, scaleSymlog, scaleRadial} from "d3-scale";
import {version} from "../version.mjs";

export const d3Version = version("d3-scale");

const logs = [
  {domain: [1, 10], range: [0, 1]},
  {domain: [1, 1000], range: [0, 960]},
  {domain: [0.5, 250], range: [500, 0]},
  {domain: [3.7, 4.2e6], range: [0, 800]},
  {domain: [1e-5, 1e3], range: [0, 800]},
  {domain: [1000, 1], range: [0, 300]},
  {domain: [-1, -1000], range: [0, 300]},
  {domain: [-0.02, -50], range: [0, 300]},
  {domain: [1, 1024], range: [0, 100], base: 2},
  {domain: [1, 100], range: [0, 100], base: Math.E},
  {domain: [1, 59049], range: [0, 100], base: 3},
  {domain: [0.3, 7.7], range: [0, 100], base: 2.5},
  {domain: [2, 9], range: [0, 100]},
  {domain: [1, 1.5], range: [0, 100]},
  {domain: [1, 10, 1000], range: [0, 50, 100]},
  {domain: [1, 1000], range: [0, 960], clamp: true},
  {domain: [1, 1000], range: [0.5, 959.5], round: true},
  {domain: [0.37, 1234], range: [0, 1], nice: true},
  {domain: [-0.37, -1234], range: [0, 1], nice: true},
  {domain: [1234, 0.37], range: [0, 1], nice: true, base: 2},
  {domain: [5, 5], range: [0, 1]},
];

const pows = [
  {domain: [0, 1], range: [0, 1], exponent: 2},
  {domain: [0, 100], range: [0, 960], exponent: 0.5},
  {domain: [-10, 10], range: [0, 100], exponent: 3},
  {domain: [-4, 9], range: [0, 100], exponent: 0.5},
  {domain: [0, 1e6], range: [0, 300], exponent: 0.25},
  {domain: [10, 0], range: [0, 100], exponent: 1},
  {domain: [1, 100], range: [0, 100], exponent: -1},
  {domain: [0, 50, 100], range: [0, 10, 300], exponent: 2},
  {domain: [0, 100], range: [0, 960], exponent: 2, clamp: true},
  {domain: [0, 100], range: [0.5, 959.5], exponent: 1.7, round: true},
  {domain: [0.13, 96.2], range: [0, 1], exponent: 2, nice: 10},
  {domain: [3, 3], range: [0, 1], exponent: 2},
];

const sqrts = [
  {domain: [0, 1], range: [0, 1]},
  {domain: [0, 1e4], range: [0, 50]},
  {domain: [-100, 100], range: [0, 50], clamp: true},
  {domain: [2.5, 97.3], range: [0, 50], nice: 5},
];

const symlogs = [
  {domain: [0, 1], range: [0, 1]},
  {domain: [-100, 100], range: [0, 960]},
  {domain: [0, 1e6], range: [500, 0]},
  {domain: [-1e3, 10], range: [0, 300], constant: 0.5},
  {domain: [-5, 5], range: [0, 100], constant: 10},
  {domain: [0, 100], range: [0, 960], clamp: true},
  {domain: [0, 100], range: [0.5, 959.5], round: true},
  {domain: [-1.7, 23.4], range: [0, 1], nice: 10},
];

const radials = [
  {domain: [0, 1], range: [0, 1]},
  {domain: [0, 100], range: [0, 200]},
  {domain: [0, 100], range: [30, 200]},
  {domain: [-50, 50], range: [-100, 100]},
  {domain: [0, 100], range: [0, 200], clamp: true},
  {domain: [0, 100], range: [0, 200], round: true},
  {domain: [0.7, 93.1], range: [10, 200], nice: 10},
];

const inputs = [-1000, -50, -1, -0.5, 0, 0.25, 0.5, 1, 2, 3, 7.5, 10, 12.5, 50, 99.9, 100, 150, 1000, 1e5, 0.0005, NaN, Infinity, -Infinity];
const formats = [[10, undefined], [5, undefined], [2, undefined], [20, undefined], [10, ".2e"], [10, ",.0f"], [Infinity, undefined], [10, "$.2"], [10, "+.1%"]];

function configure(scale, config) {
  if (config.base != null) scale.base(config.base);
  if (config.exponent != null) scale.exponent(config.exponent);
  if (config.constant != null) scale.constant(config.constant);
  scale.domain(config.domain);
  if (config.round) scale.rangeRound(config.range); else scale.range(config.range);
  if (config.clamp) scale.clamp(true);
  if (config.nice === true) scale.nice(); else if (config.nice != null) scale.nice(config.nice);
  return scale;
}

function run(make, configs, kind) {
  return configs.map((config) => {
    const scale = configure(make(), config);
    return {
      ...config,
      niceDomain: scale.domain(),
      inputs,
      outputs: inputs.map((x) => scale(x) ?? NaN),
      inverted: inputs.map((y) => scale.invert(y)),
      ticks: [undefined, 1, 2, 5, 20].map((count) => ({count: count ?? 10, ticks: scale.ticks(count)})),
      formats: formats.filter(([count]) => kind === "log" || count !== Infinity).map(([count, specifier]) => {
        const ticks = scale.ticks(count === Infinity ? 10 : count);
        const format = scale.tickFormat(count, specifier);
        return {count: String(count), specifier: specifier ?? null, ticks, labels: ticks.map((t) => format(t))};
      }),
    };
  });
}

export default function() {
  return {
    log: run(scaleLog, logs, "log"),
    pow: run(scalePow, pows, "pow"),
    sqrt: run(scaleSqrt, sqrts, "pow"),
    symlog: run(scaleSymlog, symlogs, "symlog"),
    radial: run(scaleRadial, radials, "radial"),
  };
}
