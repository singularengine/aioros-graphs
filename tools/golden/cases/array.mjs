import {
  ticks, tickIncrement, tickStep, bisectLeft, bisectRight, bisectCenter, extent, bin, thresholdSturges, thresholdScott,
  thresholdFreedmanDiaconis, quantile, quantileSorted, quantileIndex, sum, mean, median, min, max, minIndex, maxIndex,
  deviation, variance, cumsum, fsum, range, nice, group, rollup, bisector, flatGroup, flatRollup, index, fcumsum, Adder,
} from "d3-array";
import {version} from "../version.mjs";

export const d3Version = version("d3-array");

const ranges = [
  [0, 1, 10], [0, 1, 9], [0, 1, 8], [0, 1, 5], [0, 1, 2], [0, 1, 1], [-1, 0, 5], [-1, 1, 10], [1, 0, 10], [0, 10, 10],
  [0, 10, 3], [-10, 10, 5], [0.1, 0.9, 10], [0.001, 0.009, 4], [1e-12, 1e-11, 5], [1e12, 1e15, 5], [-1e15, 1e15, 7],
  [0, 0.3, 3], [0.5, 2.5, 4], [2, 2, 5], [1, 2, 0], [1, 2, -1], [1, 2, NaN], [0, 100, 1.5], [0, 100, 0.5],
  [12, 87, 10], [-0.25, 7.75, 6], [3.14159, 2.71828, 10], [0, 1e-300, 10], [1, 1.000000001, 10], [17, 19, 0.6],
  [0, 1, 30], [0, 1, 100], [-123456, 654321, 12], [0.987, 1.023, 10], [NaN, 1, 10],
];

const sorted = [1, 2, 2, 2, 3, 5, 8, 13, 21];

function random(seed) {
  return () => {
    seed |= 0; seed = seed + 0x6D2B79F5 | 0;
    let t = Math.imul(seed ^ seed >>> 15, 1 | seed);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}

const next = random(42);
const normal = Array.from({length: 1000}, () => Math.round((next() + next() + next() + next() - 2) * 1e4) / 100);
const wide = Array.from({length: 700}, () => next() * 1e6 - 3e5);

const samples = [
  [], [NaN], [5], [5, 5, 5], [3, 1, 2], [NaN, 5, -2, NaN, 7.5], [-0, 0, -0], [1e308, -1e308, 1e308], [0.1, 0.2, 0.3, 0.4],
  [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], [10, 9, 8, 7, 6, 5, 4, 3, 2, 1], [-Infinity, 1, Infinity], [0.001, 0.002, 0.0035, 0.0041],
  [-3.5, -1.25, 0, 2.75, 9.125, 9.125, 14], normal.slice(0, 200), normal, wide,
];

const thresholdGenerators = {sturges: thresholdSturges, scott: thresholdScott, freedmanDiaconis: thresholdFreedmanDiaconis};

function bins(values, thresholds, domain) {
  const b = bin().value((d) => d.v);
  if (thresholds !== undefined) b.thresholds(typeof thresholds === "string" ? thresholdGenerators[thresholds] : thresholds);
  if (domain !== undefined) b.domain(domain);
  return b(values.map((v, i) => ({v, i}))).map((x) => ({x0: x.x0 ?? NaN, x1: x.x1 ?? NaN, members: x.map((d) => d.i)}));
}

function binCases() {
  const cases = [];
  for (const values of samples) {
    for (const thresholds of [undefined, "sturges", "scott", "freedmanDiaconis", 1, 3, 7, 20, 0, [0, 2, 4], [5, 1e3, -1]]) {
      cases.push({values, thresholds: thresholds ?? null, bins: bins(values, thresholds)});
    }
    for (const domain of [[0, 10], [-5, 5], [2, 2.5]]) {
      for (const thresholds of [undefined, 4, [1, 2, 3]]) cases.push({values, thresholds: thresholds ?? null, domain, bins: bins(values, thresholds, domain)});
    }
  }
  return cases;
}

const keys = ["b", "a", "b", null, "c", "a", "b", null, "", "a"];
const weights = [1, 2, 3, 4, 5, 6, NaN, 8, 9, 10];
const numberKeys = [1, NaN, 2, -0, 0, NaN, 1, 2.5];
const objects = [{x: 1}, {x: 2}, {x: 2}, {x: 4}, {x: 7.5}, {x: 11}];
const byX = bisector((d) => d.x);

const levelSets = [
  [["b", "a", "b", null, "c", "a", "b", null, "", "a"], ["x", "y", "x", "x", "y", "y", "x", "", "", null]],
  [["b", "a", "b", null, "c", "a", "b", null, "", "a"], ["x", "y", "x", "x", "y", "y", "x", "", "", null], ["1", "1", "2", "1", "2", "2", "1", "1", "2", "1"]],
  [[1, NaN, 2, -0, 0, NaN, 1, 2.5], [0, 0, 1, 1, -0, NaN, 1, 0]],
  [[], []],
  [["only"], ["one"], ["leaf"]],
];

function nestTree(map) {
  return Array.from(map, ([key, value]) => value instanceof Map ? {key, children: nestTree(value)} : {key, leaf: value});
}

function items(levels) {
  return levels[0].map((_, i) => ({i, v: weights[i % weights.length], keys: levels.map((l) => l[i])}));
}

function accessors(levels) {
  return levels.map((_, k) => (d) => d.keys[k]);
}

const reducers = {sum: (m) => sum(m, (d) => d.v), count: (m) => m.length, median: (m) => median(m, (d) => d.v) ?? NaN};

function stripMembers(g) {
  return g.children ? {key: g.key, children: g.children.map(stripMembers)} : {key: g.key, members: g.leaf.map((d) => d.i)};
}

function stripValues(g) {
  return g.children ? {key: g.key, children: g.children.map(stripValues)} : {key: g.key, value: g.leaf};
}

function nestedGroupCases() {
  return levelSets.map((levels) => {
    const data = items(levels);
    return {
      levels,
      groups: nestTree(group(data, ...accessors(levels))).map(stripMembers),
      flat: flatGroup(data, ...accessors(levels)).map((row) => ({keys: row.slice(0, -1), members: row.at(-1).map((d) => d.i)})),
    };
  });
}

function nestedRollupCases() {
  return levelSets.flatMap((levels) => Object.entries(reducers).map(([reducer, reduce]) => {
    const data = items(levels);
    return {
      levels, values: data.map((d) => d.v), reducer,
      groups: nestTree(rollup(data, reduce, ...accessors(levels))).map(stripValues),
      flat: flatRollup(data, reduce, ...accessors(levels)).map((row) => ({keys: row.slice(0, -1), value: row.at(-1)})),
    };
  }));
}

function indexCase(keys) {
  try {
    return {keys, index: Array.from(index(keys.map((k, i) => ({k, i})), (d) => d.k), ([key, d]) => ({key, index: d.i}))};
  } catch (e) {
    return {keys, error: e.message};
  }
}

function adderSums(values) {
  const adder = new Adder();
  return values.map((v) => +adder.add(+v || 0));
}

export default function() {
  return {
    ticks: ranges.map(([start, stop, count]) => ({start, stop, count, ticks: ticks(start, stop, count)})),
    increments: ranges.map(([start, stop, count]) => ({start, stop, count, increment: tickIncrement(start, stop, count), step: tickStep(start, stop, count)})),
    bisect: [0, 1, 1.5, 2, 2.5, 4, 6.5, 21, 22, NaN, -Infinity].map((x) => ({
      values: sorted, x, left: bisectLeft(sorted, x), right: bisectRight(sorted, x), center: bisectCenter(sorted, x),
    })),
    quantile: samples.flatMap((values) => [-1, 0, 0.1, 0.25, 0.5, 0.75, 0.9, 1, 2, NaN, 1 / 3].map((p) => {
      const sortedValues = values.filter((v) => !isNaN(v)).sort((a, b) => a - b);
      return {values, p, quantile: quantile(values, p) ?? NaN, quantileSorted: quantileSorted(sortedValues, p) ?? NaN, quantileIndex: quantileIndex(values, p) ?? -2};
    })),
    statistics: samples.map((values) => ({
      values, sum: sum(values), mean: mean(values) ?? NaN, median: median(values) ?? NaN, min: min(values) ?? NaN, max: max(values) ?? NaN,
      minIndex: minIndex(values), maxIndex: maxIndex(values), variance: variance(values) ?? NaN, deviation: deviation(values) ?? NaN,
      cumsum: Array.from(cumsum(values)), fsum: fsum(values), extent: extent(values).map((v) => v ?? NaN),
      sturges: thresholdSturges(values), scott: thresholdScott(values, ...extent(values)), freedmanDiaconis: thresholdFreedmanDiaconis(values, ...extent(values)),
    })),
    bin: binCases(),
    range: [[0, 5, 1], [0, 1, 0.1], [5, 0, -1], [0, 5, -1], [0, 0, 1], [1, 2, 0], [0.5, 3.75, 0.25], [-1e3, 1e3, 333], [0, 1, NaN], [NaN, 3, 1], [0, 3.000001, 1]]
      .map(([start, stop, step]) => ({start, stop, step, values: range(start, stop, step)})),
    nice: ranges.map(([start, stop, count]) => ({start, stop, count, nice: nice(start, stop, count)})),
    group: [
      {keys, groups: Array.from(group(keys.map((k, i) => ({k, i})), (d) => d.k), ([key, members]) => ({key, members: members.map((d) => d.i)}))},
      {keys: numberKeys, groups: Array.from(group(numberKeys.map((k, i) => ({k, i})), (d) => d.k), ([key, members]) => ({key, members: members.map((d) => d.i)}))},
    ],
    rollup: ["sum", "mean", "count", "min", "max", "median"].map((reducer) => ({
      keys, values: weights, reducer,
      groups: Array.from(rollup(keys.map((k, i) => ({k, v: weights[i]})), (members) => reducer === "count" ? members.length : ({sum, mean, min, max, median})[reducer](members, (d) => d.v) ?? NaN, (d) => d.k),
        ([key, value]) => ({key, value})),
    })),
    bisector: [0, 1, 1.5, 2, 3, 4, 7.5, 11, 12, NaN, -Infinity].map((x) => ({
      keys: objects.map((d) => d.x), x, left: byX.left(objects, x), right: byX.right(objects, x), center: byX.center(objects, x),
    })),
    extent: [[3, 1, 2], [NaN, 5, -2, NaN], [], [NaN], [-0, 0], [1e308, -Infinity, 7]].map((values) => ({values, extent: extent(values)})),
    nestedGroup: nestedGroupCases(),
    nestedRollup: nestedRollupCases(),
    index: [["a", "b", "c", null, ""], [1, NaN, -0, 2.5], [], ["a", "b", "a"], [NaN, 1, NaN], [0, -0]].map(indexCase),
    fcumsum: samples.concat([[1e100, 1, -1e100, 1e-100, 0.1, 0.2, 0.3], [0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1], [1, 1e16, -1e16, NaN, 1]])
      .map((values) => ({values, fcumsum: Array.from(fcumsum(values)), adder: adderSums(values)})),
  };
}
