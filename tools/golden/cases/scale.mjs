import {scaleLinear, scaleBand, scalePoint, scaleUtc} from "d3-scale";
import {utcDay, utcHour, utcMonth, utcMinute, utcYear, utcWeek} from "d3-time";
import {version} from "../version.mjs";

export const d3Version = version("d3-scale");

const linears = [
  {domain: [0, 1], range: [0, 1]},
  {domain: [0, 100], range: [0, 960]},
  {domain: [0, 100], range: [500, 0]},
  {domain: [-1.7, 23.4], range: [10, 310]},
  {domain: [10, 0], range: [0, 300]},
  {domain: [3, 3], range: [0, 100]},
  {domain: [0, 10], range: [5, 5]},
  {domain: [0, 100], range: [0, 960], clamp: true},
  {domain: [100, 0], range: [0, 960], clamp: true},
  {domain: [0, 100], range: [0.5, 959.5], round: true},
  {domain: [-1, 0, 1], range: [0, 100, 400]},
  {domain: [1, 0, -1], range: [0, 100, 400]},
  {domain: [0, 1, 2, 3], range: [0, 10, 20]},
  {domain: [3, 2, 1, 0], range: [0, 10, 20]},
  {domain: [-1, 0, 1], range: [0, 100, 400], clamp: true},
  {domain: [0.000123, 0.000987], range: [0, 1]},
  {domain: [1e9, 3.2e9], range: [0, 800]},
  {domain: [0.1, 0.9], range: [0, 1], nice: 10},
  {domain: [12.3, 87.6], range: [0, 1], nice: 10},
  {domain: [87.6, 12.3], range: [0, 1], nice: 5},
  {domain: [-0.0013, 0.0127], range: [0, 1], nice: 3},
  {domain: [1.1, 10.9, 100.5], range: [0, 1, 2], nice: 10},
  {domain: [0, 0.97], range: [0, 1], nice: 2},
  {domain: [3, 3], range: [0, 1], nice: 10},
  {domain: [1e-20, 9.9e-20], range: [0, 1], nice: 10},
];

const inputs = [-50, -1, -0.5, 0, 0.25, 0.5, 1, 2, 3, 3.3333, 10, 12.5, 50, 99.9, 100, 150, 1e9, 2e9, 0.0005, NaN, Infinity, -Infinity];

function linear(config) {
  const scale = scaleLinear().domain(config.domain);
  if (config.round) scale.rangeRound(config.range); else scale.range(config.range);
  if (config.clamp) scale.clamp(true);
  if (config.nice != null) scale.nice(config.nice);
  return {
    ...config,
    niceDomain: scale.domain(),
    inputs,
    outputs: inputs.map((x) => scale(x)),
    inverted: inputs.map((y) => scale.invert(y)),
    ticks: [1, 2, 5, 10, 20].map((count) => ({count, ticks: scale.ticks(count)})),
  };
}

const bands = [
  {count: 3, range: [0, 960]},
  {count: 5, range: [0, 100], paddingInner: 0.1},
  {count: 5, range: [0, 100], paddingOuter: 0.25},
  {count: 5, range: [0, 100], padding: 0.2},
  {count: 5, range: [100, 0], padding: 0.2},
  {count: 7, range: [0, 640], padding: 0.1, round: true},
  {count: 7, range: [0.5, 640.5], paddingInner: 0.3, paddingOuter: 0.15, align: 0},
  {count: 7, range: [0, 640], paddingInner: 0.3, paddingOuter: 0.15, align: 1},
  {count: 4, range: [0, 100], align: 2},
  {count: 4, range: [0, 100], paddingInner: 1.5},
  {count: 1, range: [10, 20]},
  {count: 0, range: [0, 100]},
  {count: 12, range: [0, 333], padding: 0.05, round: true},
];

function band(config) {
  const keys = Array.from({length: config.count}, (_, i) => `k${i}`);
  const scale = scaleBand().domain(keys);
  if (config.round) scale.rangeRound(config.range); else scale.range(config.range);
  if (config.padding != null) scale.padding(config.padding);
  if (config.paddingInner != null) scale.paddingInner(config.paddingInner);
  if (config.paddingOuter != null) scale.paddingOuter(config.paddingOuter);
  if (config.align != null) scale.align(config.align);
  return {...config, step: scale.step(), bandwidth: scale.bandwidth(), values: keys.map((k) => scale(k)), unknown: scale("missing") ?? NaN};
}

const points = [
  {count: 3, range: [0, 960]},
  {count: 5, range: [0, 100], padding: 0.5},
  {count: 5, range: [100, 0], padding: 1},
  {count: 6, range: [0, 641], padding: 0.3, round: true},
  {count: 4, range: [0, 100], padding: 0.5, align: 0},
  {count: 1, range: [0, 100]},
  {count: 1, range: [0, 100], padding: 0.5},
  {count: 0, range: [0, 100]},
];

function point(config) {
  const keys = Array.from({length: config.count}, (_, i) => `k${i}`);
  const scale = scalePoint().domain(keys);
  if (config.round) scale.rangeRound(config.range); else scale.range(config.range);
  if (config.padding != null) scale.padding(config.padding);
  if (config.align != null) scale.align(config.align);
  return {...config, step: scale.step(), bandwidth: scale.bandwidth(), values: keys.map((k) => scale(k)), unknown: scale("missing") ?? NaN};
}

const formats = [
  {domain: [0, 1], count: 10},
  {domain: [0, 1], count: 10, specifier: "+%"},
  {domain: [0, 100], count: 5, specifier: "$,.2f"},
  {domain: [-1e6, 3e6], count: 8},
  {domain: [-1e6, 3e6], count: 8, specifier: "s"},
  {domain: [0.001, 0.009], count: 4, specifier: "e"},
  {domain: [12.3, 87.6], count: 10, specifier: "r"},
  {domain: [100, 0], count: 3, specifier: ".1f"},
  {domain: [0, 1e-7], count: 5, specifier: "~s"},
];

function linearFormat(config) {
  const scale = scaleLinear().domain(config.domain);
  const ticks = scale.ticks(config.count);
  const format = scale.tickFormat(config.count, config.specifier);
  return {...config, ticks, labels: ticks.map(format)};
}

const day = 864e5;
const utcs = [
  {domain: [Date.UTC(2000, 0, 1), Date.UTC(2000, 0, 2)], range: [0, 960]},
  {domain: [Date.UTC(2024, 1, 27, 22), Date.UTC(2024, 2, 2, 5)], range: [0, 600]},
  {domain: [Date.UTC(2024, 0, 1, 0, 0, 0, 0), Date.UTC(2024, 0, 1, 0, 0, 1, 500)], range: [0, 100]},
  {domain: [Date.UTC(2010, 5, 15), Date.UTC(2026, 9, 12)], range: [800, 0]},
  {domain: [Date.UTC(1969, 11, 25), Date.UTC(1970, 0, 7, 12)], range: [0, 300], clamp: true},
  {domain: [Date.UTC(2026, 9, 12, 13, 7), Date.UTC(2026, 9, 12, 9, 41)], range: [0, 500], round: true},
  {domain: [Date.UTC(2020, 0, 1), Date.UTC(2020, 6, 1), Date.UTC(2021, 0, 1)], range: [0, 400, 500]},
  {domain: [Date.UTC(1900, 0, 1), Date.UTC(2100, 0, 1)], range: [0, 1000]},
];
const intervals = {day: [utcDay, 864e5], hour: [utcHour, 36e5], month: [utcMonth, 2592e6], minute15: [utcMinute.every(15), 9e5], year: [utcYear, 31536e6], week: [utcWeek, 6048e5]};
const instants = [-Infinity, NaN, 0, 0.25, 0.5, 1, 1.5].map((t) => t);

function utc(config) {
  const scale = scaleUtc().domain(config.domain.map((t) => new Date(t)));
  if (config.round) scale.rangeRound(config.range); else scale.range(config.range);
  if (config.clamp) scale.clamp(true);
  const [d0, d1] = [config.domain[0], config.domain[config.domain.length - 1]];
  const inputs = instants.map((t) => Number.isFinite(t) ? d0 + (d1 - d0) * t : t);
  const ticks = [3, 5, 10, 20].map((count) => {
    const values = scale.ticks(count);
    return {count, ticks: values.map(Number), labels: values.map(scale.tickFormat(count))};
  });
  const explicit = Object.entries(intervals).filter(([, [, duration]]) => Math.abs(d1 - d0) / duration <= 2000).map(([name, [interval]]) => ({interval: name, ticks: scale.ticks(interval).map(Number)}));
  const nice = [10, 5].map((count) => ({count, domain: scale.copy().nice(count).domain().map(Number)}));
  const range = config.range;
  const outputs = inputs.map((x) => { const y = scale(new Date(x)); return y === undefined ? NaN : y; });
  const inverted = [range[0], range[range.length - 1], (range[0] + range[range.length - 1]) / 2, -50, 1e4].map((y) => ({y, x: +scale.invert(y)}));
  const formatted = ticks[2].ticks.map((t) => scale.tickFormat(10, "%Y-%m-%d %H:%M")(new Date(t)));
  return {...config, inputs, outputs, inverted, ticks, explicit, nice, formatted};
}

export default function() {
  return {linear: linears.map(linear), band: bands.map(band), point: points.map(point), linearFormat: formats.map(linearFormat), utc: utcs.map(utc)};
}
