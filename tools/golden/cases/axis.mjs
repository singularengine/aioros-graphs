import {axisTop, axisRight, axisBottom, axisLeft} from "d3-axis";
import {spawnSync} from "node:child_process";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {render} from "./local/dom.mjs";
import {scaleLinear, scaleBand, scalePoint, scaleUtc, scaleLog, scalePow, scaleSymlog} from "d3-scale";
import {version} from "../version.mjs";

export const d3Version = version("d3-axis");

const orientations = {top: axisTop, right: axisRight, bottom: axisBottom, left: axisLeft};

function configure(axis, options) {
  if (options.tickSize != null) axis.tickSize(options.tickSize);
  if (options.tickSizeInner != null) axis.tickSizeInner(options.tickSizeInner);
  if (options.tickSizeOuter != null) axis.tickSizeOuter(options.tickSizeOuter);
  if (options.tickPadding != null) axis.tickPadding(options.tickPadding);
  if (options.offset != null) axis.offset(options.offset);
  return axis;
}

const settings = [
  {}, {tickSize: 0}, {tickSizeInner: -480, tickSizeOuter: 0}, {tickSizeInner: 10, tickSizeOuter: 3, tickPadding: 8},
  {offset: 0}, {tickPadding: -2, offset: 1.25},
];

export default function() {
  const linear = [];
  const linearScales = [
    {domain: [0, 1], range: [0, 960]}, {domain: [-1.5, 123456.7], range: [500, 0]}, {domain: [10, -10], range: [20.25, 300.5]},
    {domain: [0.001, 0.0042], range: [0, 300]}, {domain: [3, 3], range: [0, 100]}, {domain: [0, 100, 1000], range: [0, 50, 600]},
  ];
  const tickArguments = [[], [5], [20, "$,.2f"], [4, ".1%"], [10, "~s"], [3, "+e"]];
  for (const [orient, make] of Object.entries(orientations)) {
    for (const s of linearScales) {
      for (const args of tickArguments) {
        for (const options of s === linearScales[1] && args === tickArguments[0] ? settings : settings.slice(0, 1)) {
          const scale = scaleLinear(s.domain, s.range);
          linear.push({orient, ...s, tickArguments: args, options, ...render(configure(make(scale).ticks(...args), options))});
        }
      }
    }
  }
  const values = [];
  for (const [orient, make] of Object.entries(orientations)) {
    for (const tickValues of [[0, 0.25, 0.5, 0.999], [], [-1, 2, NaN]]) {
      for (const options of settings.slice(0, 3)) {
        const scale = scaleLinear([0, 1], [0, 400]);
        values.push({orient, domain: [0, 1], range: [0, 400], tickValues, tickArguments: [10, ".3f"], options,
          ...render(configure(make(scale).ticks(10, ".3f").tickValues(tickValues), options))});
      }
    }
  }
  const band = [];
  const keys = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat & Sun"];
  const bandScales = [
    {range: [0, 600], padding: 0, round: false}, {range: [0, 600], padding: 0.2, round: true}, {range: [600, 13], padding: 0.35, round: false},
    {range: [0, 97], paddingInner: 0.1, paddingOuter: 0.4, round: true, align: 0.2}, {range: [0, 1], padding: 0, round: false},
  ];
  for (const [orient, make] of Object.entries(orientations)) {
    for (const s of bandScales) {
      for (const options of s === bandScales[1] ? settings : settings.slice(0, 1)) {
        for (const count of [keys.length, 1, 0]) {
          const scale = scaleBand(keys.slice(0, count), s.range).round(s.round).align(s.align ?? 0.5);
          if (s.padding != null) scale.padding(s.padding);
          if (s.paddingInner != null) scale.paddingInner(s.paddingInner).paddingOuter(s.paddingOuter);
          band.push({orient, keys: keys.slice(0, count), ...s, options, ...render(configure(make(scale), options))});
        }
      }
    }
  }
  const point = [];
  for (const [orient, make] of Object.entries(orientations)) {
    for (const s of [{range: [0, 600], padding: 0, round: false}, {range: [10, 590], padding: 0.5, round: true}, {range: [300, 0], padding: 1.5, round: false}]) {
      for (const options of settings.slice(0, 2)) {
        const scale = scalePoint(keys, s.range).padding(s.padding).round(s.round);
        point.push({orient, keys, ...s, options, ...render(configure(make(scale), options))});
      }
    }
  }
  const utc = [];
  const spans = [
    [Date.UTC(2023, 0, 1), Date.UTC(2024, 11, 31)], [Date.UTC(2024, 0, 1), Date.UTC(2024, 11, 31)], [Date.UTC(2024, 2, 10, 6), Date.UTC(2024, 2, 12, 18)],
    [Date.UTC(2020, 0, 1, 0, 0, 5), Date.UTC(2020, 0, 1, 0, 3, 0)], [Date.UTC(1990, 0, 1), Date.UTC(2030, 0, 1)],
  ];
  for (const [orient, make] of Object.entries(orientations)) {
    for (const domain of spans) {
      for (const args of [[], [8], [5, "%Y-%m-%d"]]) {
        for (const options of settings.slice(0, 2)) {
          const scale = scaleUtc(domain, [40, 616]);
          utc.push({orient, domain, range: [40, 616], tickArguments: args, options, ...render(configure(make(scale).ticks(...args), options))});
        }
      }
    }
  }
  const continuous = [];
  const kinds = [
    {kind: "log", domain: [1, 10000], make: (d) => scaleLog(d, [500, 0])}, {kind: "log", domain: [0.05, 830], make: (d) => scaleLog(d, [0, 600])},
    {kind: "log2", domain: [1, 4096], make: (d) => scaleLog(d, [0, 600]).base(2)}, {kind: "log", domain: [-1000, -1], make: (d) => scaleLog(d, [0, 300])},
    {kind: "sqrt", domain: [0, 1e6], make: (d) => scalePow(d, [0, 400]).exponent(0.5)}, {kind: "pow3", domain: [-2, 2], make: (d) => scalePow(d, [300, 0]).exponent(3)},
    {kind: "symlog", domain: [-1e4, 1e4], make: (d) => scaleSymlog(d, [0, 640])},
  ];
  for (const [orient, make] of Object.entries(orientations)) {
    for (const k of kinds) {
      for (const args of [[], [5], [10, "$,.0f"], [3, ".2~s"]]) {
        const scale = k.make(k.domain);
        continuous.push({orient, kind: k.kind, domain: k.domain, range: scale.range(), tickArguments: args, options: {},
          ...render(make(scale).ticks(...args))});
      }
    }
  }
  const time = [];
  const helper = join(dirname(fileURLToPath(import.meta.url)), "local", "axis-zone.mjs");
  for (const zone of ["Europe/Berlin", "America/Sao_Paulo", "Asia/Kolkata"]) {
    const result = spawnSync(process.execPath, [helper], {env: {...process.env, TZ: zone}, maxBuffer: 1 << 26});
    if (result.status !== 0) throw new Error(result.stderr.toString());
    time.push(...JSON.parse(result.stdout.toString()).map((c) => ({zone, ...c})));
  }
  return {linear, values, band, point, utc, continuous, time};
}
