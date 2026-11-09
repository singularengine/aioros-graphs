import {
  scaleSequential, scaleSequentialLog, scaleSequentialPow, scaleSequentialSqrt, scaleSequentialSymlog, scaleSequentialQuantile,
  scaleDiverging, scaleDivergingLog, scaleDivergingPow, scaleDivergingSqrt, scaleDivergingSymlog,
} from "d3-scale";
import {version} from "../version.mjs";

export const d3Version = version("d3-scale");

const makers = {
  linear: [scaleSequential, scaleDiverging], log: [scaleSequentialLog, scaleDivergingLog], pow: [scaleSequentialPow, scaleDivergingPow],
  sqrt: [scaleSequentialSqrt, scaleDivergingSqrt], symlog: [scaleSequentialSymlog, scaleDivergingSymlog],
};

const sequentials = [
  {kind: "linear", domain: [0, 1]},
  {kind: "linear", domain: [0, 100]},
  {kind: "linear", domain: [100, 0], range: [10, 20]},
  {kind: "linear", domain: [-1.7, 23.4], clamp: true},
  {kind: "linear", domain: [5, 5]},
  {kind: "linear", domain: [0, 100], range: [0, 255], round: true},
  {kind: "linear", domain: [0.13, 96.2], nice: 10},
  {kind: "log", domain: [1, 1000]},
  {kind: "log", domain: [-1, -1000], clamp: true},
  {kind: "log", domain: [0.37, 1234], nice: true, base: 2},
  {kind: "log", domain: [3, 70000], range: [0, 1000]},
  {kind: "pow", domain: [0, 100], exponent: 2},
  {kind: "pow", domain: [-10, 10], exponent: 3, clamp: true},
  {kind: "pow", domain: [1, 50], exponent: 0.25, nice: 5},
  {kind: "sqrt", domain: [0, 1e4]},
  {kind: "symlog", domain: [-100, 100]},
  {kind: "symlog", domain: [0, 1e6], constant: 10, clamp: true},
];

const divergings = [
  {kind: "linear", domain: [0, 0.5, 1]},
  {kind: "linear", domain: [-1, 0, 1]},
  {kind: "linear", domain: [-20, 0, 100], range: [0, 50, 80]},
  {kind: "linear", domain: [10, 0, -10]},
  {kind: "linear", domain: [0, 0, 1]},
  {kind: "linear", domain: [-5, 2, 2], clamp: true},
  {kind: "linear", domain: [-30, 5, 100], range: [0, 128, 255], round: true},
  {kind: "linear", domain: [-0.87, 0.1, 1.33], nice: 10},
  {kind: "log", domain: [0.1, 1, 10]},
  {kind: "log", domain: [-10, -1, -0.1], clamp: true},
  {kind: "log", domain: [0.37, 2, 1234], nice: true},
  {kind: "pow", domain: [-10, 0, 10], exponent: 2},
  {kind: "sqrt", domain: [0, 25, 100]},
  {kind: "symlog", domain: [-1e3, 0, 10], constant: 0.5},
];

const quantiles = [
  {domain: [3, 1, 4, 1, 5, 9, 2, 6, 5, 3, 5]},
  {domain: [NaN, 10, -5, 2.5, NaN, 7]},
  {domain: [42]},
  {domain: Array.from({length: 100}, (_, i) => (i * 37 % 101) / 7)},
];

const inputs = [-1e4, -1000, -50, -10, -1, -0.5, 0, 0.1, 0.25, 0.5, 1, 2, 3, 5, 7.5, 10, 12.5, 50, 99.9, 100, 150, 1000, 1e5, NaN, Infinity, -Infinity];
const formats = [[10, undefined], [5, undefined], [10, ".2e"], [10, "+.1%"]];

function configure(scale, config) {
  if (config.base != null) scale.base(config.base);
  if (config.exponent != null) scale.exponent(config.exponent);
  if (config.constant != null) scale.constant(config.constant);
  scale.domain(config.domain);
  if (config.range) { if (config.round) scale.rangeRound(config.range); else scale.range(config.range); }
  if (config.clamp) scale.clamp(true);
  if (config.nice === true) scale.nice(); else if (config.nice != null) scale.nice(config.nice);
  return scale;
}

function run(configs, which) {
  return configs.map((config) => {
    const scale = configure(makers[config.kind][which](), config);
    return {
      ...config,
      niceDomain: scale.domain(),
      inputs,
      outputs: inputs.map((x) => scale(x) ?? NaN),
      ends: scale.range(),
      ticks: [undefined, 1, 5, 20].map((count) => ({count: count ?? 10, ticks: scale.ticks(count)})),
      formats: formats.map(([count, specifier]) => {
        const ticks = scale.ticks(count);
        const format = scale.tickFormat(count, specifier);
        return {count, specifier: specifier ?? null, ticks, labels: ticks.map((t) => format(t))};
      }),
    };
  });
}

export default function() {
  return {
    sequential: run(sequentials, 0),
    diverging: run(divergings, 1),
    quantile: quantiles.map((config) => {
      const scale = scaleSequentialQuantile().domain(config.domain);
      return {
        ...config,
        sorted: scale.domain(),
        inputs,
        outputs: inputs.map((x) => scale(x) ?? NaN),
        range: scale.range(),
        quantiles: [1, 4, 10].map((n) => ({n, quantiles: scale.quantiles(n)})),
      };
    }),
  };
}
