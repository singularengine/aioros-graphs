import {scaleOrdinal, scaleQuantize, scaleQuantile, scaleThreshold, scaleImplicit} from "d3-scale";
import {version} from "../version.mjs";

export const d3Version = version("d3-scale");

const letters = (n) => Array.from({length: n}, (_, i) => String.fromCharCode(97 + i));

const ordinals = [
  {domain: ["x", "y", "z"], range: ["red", "green", "blue"], probes: ["x", "y", "z", "w", "x", "v", "w"]},
  {domain: ["x", "y", "z", "x", "u"], range: ["red", "green"], probes: ["u", "z", "y", "q", "r", "s"]},
  {domain: [], range: ["a", "b", "c"], probes: ["k3", "k1", "k3", "k2", "k5", "k4"]},
  {domain: ["x", "y"], range: ["red", "green", "blue"], unknown: "gray", probes: ["x", "y", "z", "y", "w"]},
  {domain: ["", "0", "1"], range: ["a"], probes: ["1", "", "2", "0"]},
];

const quantizes = [
  {domain: [0, 1], range: letters(2)},
  {domain: [0, 100], range: letters(5)},
  {domain: [100, 0], range: letters(4)},
  {domain: [-3.7, 12.1], range: letters(7)},
  {domain: [5, 5], range: letters(3)},
  {domain: [0, 1], range: letters(1)},
  {domain: [0.13, 96.2], range: letters(6), nice: 10},
  {domain: [-1234, 5678], range: letters(9), nice: 5},
];

const quantiles = [
  {domain: [3, 6, 7, 8, 8, 10, 13, 15, 16, 20], range: letters(4)},
  {domain: [3, 6, 7, 8, 8, 9, 10, 13, 15, 16, 20], range: letters(3)},
  {domain: [NaN, 10, -5, 2.5, NaN, 7, 0, -0], range: letters(2)},
  {domain: [1], range: letters(5)},
  {domain: [], range: letters(3)},
  {domain: Array.from({length: 200}, (_, i) => (i * 73 % 199) / 3), range: letters(10)},
  {domain: [1, 2, 3], range: []},
];

const thresholds = [
  {domain: [0.5], range: letters(2)},
  {domain: [0, 1], range: letters(3)},
  {domain: [10, 20, 30, 40], range: letters(3)},
  {domain: [10, 20], range: letters(5)},
  {domain: [-1, 0, 1], range: letters(4)},
  {domain: [], range: letters(1)},
];

const inputs = [-1e4, -1234, -10, -5, -3.7, -1, -0.5, 0, 0.1, 0.25, 0.5, 0.75, 1, 2, 3, 5, 7.5, 8, 10, 12.1, 13, 20, 33.3, 50, 66.7, 99.9, 100, 150, 5678, NaN, Infinity, -Infinity];

export default function() {
  return {
    ordinal: ordinals.map((config) => {
      const scale = scaleOrdinal(config.domain, config.range);
      if (config.unknown !== undefined) scale.unknown(config.unknown);
      const outputs = config.probes.map((p) => scale(p) ?? null);
      return {...config, unknown: config.unknown ?? null, outputs, finalDomain: scale.domain(), implicit: scale.unknown() === scaleImplicit};
    }),
    quantize: quantizes.map((config) => {
      const scale = scaleQuantize(config.domain, config.range);
      if (config.nice != null) scale.nice(config.nice);
      return {
        ...config, niceDomain: scale.domain(), thresholds: scale.thresholds(), inputs, outputs: inputs.map((x) => scale(x) ?? null),
        extents: config.range.concat(["missing"]).map((y) => scale.invertExtent(y)),
        ticks: [10, 5, 1].map((count) => ({count, ticks: scale.ticks(count), labels: scale.ticks(count).map(scale.tickFormat(count))})),
      };
    }),
    quantile: quantiles.map((config) => {
      const scale = scaleQuantile(config.domain, config.range);
      return {
        ...config, sorted: scale.domain(), quantiles: scale.quantiles(), inputs, outputs: inputs.map((x) => scale(x) ?? null),
        extents: config.range.concat(["missing"]).map((y) => scale.invertExtent(y).map((v) => v ?? NaN)),
      };
    }),
    threshold: thresholds.map((config) => {
      const scale = scaleThreshold(config.domain, config.range);
      return {
        ...config, inputs, outputs: inputs.map((x) => scale(x) ?? null),
        extents: config.range.concat(["missing"]).map((y) => scale.invertExtent(y).map((v) => v ?? NaN)),
      };
    }),
  };
}
