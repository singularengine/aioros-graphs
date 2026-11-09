import * as d3 from "d3-shape";
import {version} from "../version.mjs";

export const d3Version = version("d3-shape");

const curves = {
  linear: d3.curveLinear,
  monotoneX: d3.curveMonotoneX,
  monotoneY: d3.curveMonotoneY,
  natural: d3.curveNatural,
  step: d3.curveStep,
  stepBefore: d3.curveStepBefore,
  stepAfter: d3.curveStepAfter,
  basis: d3.curveBasis,
  cardinal: d3.curveCardinal,
  "cardinal:0.5": d3.curveCardinal.tension(0.5),
  catmullRom: d3.curveCatmullRom,
  "catmullRom:0": d3.curveCatmullRom.alpha(0),
  "catmullRom:1": d3.curveCatmullRom.alpha(1),
};

const series = {
  empty: [],
  one: [[10, 20]],
  two: [[10, 20], [30.5, 40.25]],
  three: [[0, 0], [1, 3], [2, 1]],
  wave: Array.from({length: 12}, (_, i) => [i * 23.7, 100 + 60 * Math.sin(i / 1.7)]),
  steps: [[0, 10], [10, 10], [20, 30], [30, 30], [40, 5], [50, 5]],
  coincident: [[0, 0], [0, 0], [5, 5], [5, 5], [9, 1]],
  vertical: [[0, 0], [0, 10], [0, 20], [3, 30]],
  gaps: [[0, 5], [1, NaN], [2, 7], [3, 8], [4, NaN], [5, NaN], [6, 2], [7, 3], [8, 9]],
  descending: [[50, 1], [40, 4], [30, 9], [20, 16], [10, 25]],
  fractions: [[1 / 3, 2 / 3], [Math.PI, Math.E], [1e-4, 12345.6789], [99.9995, 0.0005]],
};

function defined(p) {
  return !Number.isNaN(p[1]);
}

export default function() {
  const lines = [], areas = [];
  for (const [curveName, curve] of Object.entries(curves)) {
    for (const [seriesName, data] of Object.entries(series)) {
      for (const digits of [3, null]) {
        const line = d3.line().curve(curve).defined(defined).digits(digits);
        lines.push({curve: curveName, series: seriesName, digits, x: data.map((p) => p[0]), y: data.map((p) => p[1]), path: line(data)});
        const area = d3.area().curve(curve).defined(defined).digits(digits).y0((p, i) => 200 - i * 3).y1((p) => p[1]);
        areas.push({curve: curveName, series: seriesName, digits, x0: data.map((p) => p[0]), y0: data.map((p, i) => 200 - i * 3), y1: data.map((p) => p[1]), path: area(data)});
      }
    }
    const data = series.wave;
    const shifted = d3.area().curve(curve).x0((p) => p[0]).x1((p) => p[0] + 5).y0(150).y1((p) => p[1]);
    areas.push({curve: curveName, series: "wave-x1-constant-y0", digits: 3, x0: data.map((p) => p[0]), x1: data.map((p) => p[0] + 5), y0: [150], y1: data.map((p) => p[1]), path: shifted(data)});
  }

  const arcSettings = [
    {innerRadius: 0, outerRadius: 100, startAngle: 0, endAngle: Math.PI / 2},
    {innerRadius: 50, outerRadius: 100, startAngle: 0, endAngle: Math.PI / 2},
    {innerRadius: 0, outerRadius: 100, startAngle: 0, endAngle: 2 * Math.PI},
    {innerRadius: 60, outerRadius: 100, startAngle: 0, endAngle: 2 * Math.PI},
    {innerRadius: 60, outerRadius: 100, startAngle: 1, endAngle: 1 + 2 * Math.PI + 0.5},
    {innerRadius: 100, outerRadius: 50, startAngle: 0, endAngle: 1},
    {innerRadius: 0, outerRadius: 0, startAngle: 0, endAngle: 1},
    {innerRadius: 0, outerRadius: 100, startAngle: Math.PI / 2, endAngle: 0},
    {innerRadius: 0, outerRadius: 100, startAngle: 0, endAngle: 4},
    {innerRadius: 30, outerRadius: 100, startAngle: 0.3, endAngle: 2.1, padAngle: 0.04},
    {innerRadius: 30, outerRadius: 100, startAngle: 2.1, endAngle: 0.3, padAngle: 0.04},
    {innerRadius: 30, outerRadius: 100, startAngle: 0.3, endAngle: 0.33, padAngle: 0.1},
    {innerRadius: 0, outerRadius: 100, startAngle: 0.3, endAngle: 2.1, padAngle: 0.04, padRadius: 200},
    {innerRadius: 0, outerRadius: 100, startAngle: 0, endAngle: 1.2, cornerRadius: 10},
    {innerRadius: 40, outerRadius: 100, startAngle: 0, endAngle: 1.2, cornerRadius: 10},
    {innerRadius: 40, outerRadius: 100, startAngle: 0, endAngle: 4, cornerRadius: 10},
    {innerRadius: 40, outerRadius: 100, startAngle: 0, endAngle: 0.2, cornerRadius: 25},
    {innerRadius: 40, outerRadius: 100, startAngle: 0, endAngle: 0.05, cornerRadius: 25},
    {innerRadius: 40, outerRadius: 100, startAngle: 0, endAngle: 1.2, cornerRadius: 100},
    {innerRadius: 40, outerRadius: 100, startAngle: 0.5, endAngle: 2.5, cornerRadius: 8, padAngle: 0.03},
    {innerRadius: 40, outerRadius: 100, startAngle: 2.5, endAngle: 0.5, cornerRadius: 8, padAngle: 0.03},
    {innerRadius: 0, outerRadius: 100, startAngle: 0, endAngle: 6, cornerRadius: 5, padAngle: 0.02},
    {innerRadius: 80, outerRadius: 100, startAngle: 0, endAngle: 2 * Math.PI, cornerRadius: 5},
  ];
  const arcs = [];
  for (const settings of arcSettings) {
    for (const digits of [3, null]) {
      const arc = d3.arc().digits(digits).cornerRadius(settings.cornerRadius ?? 0);
      if (settings.padRadius != null) arc.padRadius(settings.padRadius);
      arcs.push({...settings, digits, path: arc(settings), centroid: arc.centroid(settings)});
    }
  }

  const pieValues = {
    simple: [1, 2, 3],
    ties: [5, 1, 5, 2, 1],
    zeros: [0, 3, 0, 2],
    negative: [4, -1, 3],
    nan: [2, NaN, 1],
    allZero: [0, 0],
    empty: [],
    many: Array.from({length: 9}, (_, i) => ((i * 37) % 11) + 0.5),
  };
  const pieSettings = [
    {},
    {sort: "none"},
    {sort: "ascending"},
    {startAngle: -Math.PI / 2, endAngle: Math.PI / 2},
    {startAngle: Math.PI, endAngle: -Math.PI},
    {startAngle: 0, endAngle: 10},
    {padAngle: 0.02},
    {padAngle: 0.02, startAngle: 1, endAngle: 0},
    {padAngle: 3},
  ];
  const pies = [];
  for (const [name, values] of Object.entries(pieValues)) {
    for (const settings of pieSettings) {
      const pie = d3.pie();
      if (settings.sort === "none") pie.sortValues(null);
      if (settings.sort === "ascending") pie.sortValues((a, b) => a < b ? -1 : a > b ? 1 : a >= b ? 0 : NaN);
      if (settings.startAngle != null) pie.startAngle(settings.startAngle);
      if (settings.endAngle != null) pie.endAngle(settings.endAngle);
      if (settings.padAngle != null) pie.padAngle(settings.padAngle);
      pies.push({values: name, settings, input: values, slices: pie(values).map(({index, value, startAngle, endAngle, padAngle}) => ({index, value, startAngle, endAngle, padAngle}))});
    }
  }

  const matrices = {
    simple: [[1, 2, 3], [2, 1, 0], [3, 4, 5], [0, 2, 1]],
    signs: [[1, -2, 3], [-2, 1, -1], [3, -4, 0], [-1, 2, 1]],
    gaps: [[1, NaN, 3], [2, 1, NaN], [NaN, 4, 5]],
    single: [[5]],
    empty: [],
    peaks: [[0, 1, 9, 2], [5, 0, 1, 1], [1, 7, 0, 3], [2, 2, 2, 8], [9, 0, 3, 1]],
  };
  const orders = {none: d3.stackOrderNone, reverse: d3.stackOrderReverse, ascending: d3.stackOrderAscending, descending: d3.stackOrderDescending, insideOut: d3.stackOrderInsideOut, appearance: d3.stackOrderAppearance};
  const offsets = {none: d3.stackOffsetNone, expand: d3.stackOffsetExpand, diverging: d3.stackOffsetDiverging, silhouette: d3.stackOffsetSilhouette, wiggle: d3.stackOffsetWiggle};
  const stacks = [];
  for (const [name, rows] of Object.entries(matrices)) {
    const keys = rows.length ? rows[0].map((_, i) => i) : [0, 1];
    for (const [orderName, order] of Object.entries(orders)) {
      for (const [offsetName, offset] of Object.entries(offsets)) {
        const result = d3.stack().keys(keys).order(order).offset(offset)(rows);
        stacks.push({
          values: name, order: orderName, offset: offsetName, series: keys.length, rows,
          stackOrder: result.map((s) => s.index).reduce((o, position, series) => (o[position] = series, o), []),
          lower: result.map((s) => s.map((p) => p[0])),
          upper: result.map((s) => s.map((p) => p[1])),
        });
      }
    }
  }

  return {lines, areas, arcs, pies, stacks};
}
