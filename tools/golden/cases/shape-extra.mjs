import * as d3 from "d3-shape";
import {path, pathRound} from "d3-path";
import {version} from "../version.mjs";

export const d3Version = version("d3-shape");

const curves = {
  linearClosed: d3.curveLinearClosed,
  basisClosed: d3.curveBasisClosed,
  basisOpen: d3.curveBasisOpen,
  bundle: d3.curveBundle,
  "bundle:0.5": d3.curveBundle.beta(0.5),
  "bundle:1": d3.curveBundle.beta(1),
  "bundle:0": d3.curveBundle.beta(0),
  cardinalClosed: d3.curveCardinalClosed,
  "cardinalClosed:0.5": d3.curveCardinalClosed.tension(0.5),
  cardinalOpen: d3.curveCardinalOpen,
  "cardinalOpen:0.5": d3.curveCardinalOpen.tension(0.5),
  catmullRomClosed: d3.curveCatmullRomClosed,
  "catmullRomClosed:0": d3.curveCatmullRomClosed.alpha(0),
  "catmullRomClosed:1": d3.curveCatmullRomClosed.alpha(1),
  catmullRomOpen: d3.curveCatmullRomOpen,
  "catmullRomOpen:0": d3.curveCatmullRomOpen.alpha(0),
  "catmullRomOpen:1": d3.curveCatmullRomOpen.alpha(1),
  bumpX: d3.curveBumpX,
  bumpY: d3.curveBumpY,
};

const radialCurves = {
  linear: d3.curveLinear, monotoneX: d3.curveMonotoneX, natural: d3.curveNatural, basis: d3.curveBasis, cardinal: d3.curveCardinal,
  catmullRom: d3.curveCatmullRom, step: d3.curveStep, linearClosed: d3.curveLinearClosed, basisClosed: d3.curveBasisClosed,
  basisOpen: d3.curveBasisOpen, cardinalClosed: d3.curveCardinalClosed, catmullRomOpen: d3.curveCatmullRomOpen, bundle: d3.curveBundle,
  bumpX: d3.curveBumpX,
};

const series = {
  empty: [],
  one: [[10, 20]],
  two: [[10, 20], [30.5, 40.25]],
  three: [[0, 0], [1, 3], [2, 1]],
  four: [[0, 0], [10, 30], [20, 10], [30, 25]],
  wave: Array.from({length: 12}, (_, i) => [i * 23.7, 100 + 60 * Math.sin(i / 1.7)]),
  coincident: [[0, 0], [0, 0], [5, 5], [5, 5], [9, 1]],
  gaps: [[0, 5], [1, NaN], [2, 7], [3, 8], [4, NaN], [5, NaN], [6, 2], [7, 3], [8, 9], [9, 4]],
  fractions: [[1 / 3, 2 / 3], [Math.PI, Math.E], [1e-4, 12345.6789], [99.9995, 0.0005]],
};

const radialSeries = {
  empty: [],
  one: [[0.5, 20]],
  two: [[0, 10], [Math.PI / 2, 40.25]],
  spiral: Array.from({length: 13}, (_, i) => [i * Math.PI / 6, 20 + i * 7.5]),
  gaps: [[0, 5], [0.7, NaN], [1.4, 7], [2.1, 8], [2.8, NaN], [3.5, 30], [4.2, 31], [4.9, 12]],
  negative: [[-1, 50], [-0.5, -20], [0, 33.3], [6.5, 41]],
};

const defined = (p) => !Number.isNaN(p[1]);
const make = (digits) => digits == null ? path() : pathRound(digits);

function translated(context, dx, dy) {
  return {
    moveTo: (x, y) => context.moveTo(x + dx, y + dy),
    lineTo: (x, y) => context.lineTo(x + dx, y + dy),
    closePath: () => context.closePath(),
    arc: (x, y, r, a0, a1, ccw) => context.arc(x + dx, y + dy, r, a0, a1, ccw),
    rect: (x, y, w, h) => context.rect(x + dx, y + dy, w, h),
  };
}

const symbolTypes = {
  circle: d3.symbolCircle, cross: d3.symbolCross, diamond: d3.symbolDiamond, square: d3.symbolSquare, star: d3.symbolStar,
  triangle: d3.symbolTriangle, wye: d3.symbolWye, plus: d3.symbolPlus, times: d3.symbolTimes, triangle2: d3.symbolTriangle2,
  asterisk: d3.symbolAsterisk, square2: d3.symbolSquare2, diamond2: d3.symbolDiamond2,
};
const nameOf = (type) => Object.keys(symbolTypes).find((k) => symbolTypes[k] === type);

export default function() {
  const lines = [], areas = [];
  for (const [curveName, curve] of Object.entries(curves)) {
    for (const [seriesName, data] of Object.entries(series)) {
      for (const digits of [3, null]) {
        const line = d3.line().curve(curve).defined(defined).digits(digits);
        lines.push({curve: curveName, series: seriesName, digits, x: data.map((p) => p[0]), y: data.map((p) => p[1]), path: line(data)});
        if (curveName.startsWith("bundle")) continue;
        const area = d3.area().curve(curve).defined(defined).digits(digits).y0((p, i) => 200 - i * 3).y1((p) => p[1]);
        areas.push({curve: curveName, series: seriesName, digits, x0: data.map((p) => p[0]), y0: data.map((p, i) => 200 - i * 3), y1: data.map((p) => p[1]), path: area(data)});
      }
    }
  }

  let bundleAreaThrows = false;
  try { d3.area().curve(d3.curveBundle)(series.two); } catch { bundleAreaThrows = true; }
  const bundleArea = [{throws: bundleAreaThrows, empty: d3.area().curve(d3.curveBundle)([])}];

  const symbols = [];
  for (const [name, type] of Object.entries(symbolTypes)) {
    for (const size of [undefined, 0, 1, 2.5, 30.25, 100, 1000, -1]) {
      for (const digits of [3, null]) {
        symbols.push({type: name, size: size ?? null, digits, path: d3.symbol(type, size).digits(digits)()});
      }
    }
  }

  const centers = [[0, 0], [100.5, 20.25], [-13.3, 7e3]];
  const translatedSymbols = [];
  for (const [name, type] of Object.entries(symbolTypes)) {
    for (const digits of [3, null]) {
      const context = make(digits);
      for (const [dx, dy] of centers) type.draw(translated(context, dx, dy), 50);
      translatedSymbols.push({type: name, size: 50, digits, centers, path: context + ""});
    }
  }

  const lists = [{fill: d3.symbolsFill.map(nameOf), stroke: d3.symbolsStroke.map(nameOf)}];

  const endpoints = [
    [[0, 0], [100, 50]], [[10.5, 200.25], [300, 20]], [[5, 5], [5, 5]], [[0, 0], [0, 80]], [[-40, 12], [1 / 3, -7]],
  ];
  const linkCurves = {horizontal: null, vertical: null, linear: d3.curveLinear, step: d3.curveStep, monotoneX: d3.curveMonotoneX,
    natural: d3.curveNatural, basis: d3.curveBasis, bumpX: d3.curveBumpX, catmullRom: d3.curveCatmullRom};
  const links = [];
  for (const [name, curve] of Object.entries(linkCurves)) {
    const factory = () => name === "horizontal" ? d3.linkHorizontal() : name === "vertical" ? d3.linkVertical() : d3.link(curve);
    for (const digits of [3, null]) {
      for (const [source, target] of endpoints) {
        links.push({curve: name, digits, source, target, path: factory().digits(digits)({source, target})});
      }
      const context = make(digits);
      const shared = factory().context(context);
      for (const [source, target] of endpoints) shared({source, target});
      links.push({curve: name, digits, many: endpoints, path: context + ""});
    }
  }

  const radialEnds = [[[0, 0], [Math.PI / 2, 100]], [[1, 30], [2.5, 80.5]], [[-0.3, 40], [4, 40]], [[0.25, 10], [0.25, 10]]];
  const radialLinks = [];
  for (const digits of [3, null]) {
    for (const [source, target] of radialEnds) {
      radialLinks.push({digits, source, target, path: d3.linkRadial().digits(digits)({source, target})});
    }
  }

  const radialLines = [], radialAreas = [];
  for (const [curveName, curve] of Object.entries(radialCurves)) {
    for (const [seriesName, data] of Object.entries(radialSeries)) {
      for (const digits of [3, null]) {
        const line = d3.lineRadial().curve(curve).defined(defined).digits(digits);
        radialLines.push({curve: curveName, series: seriesName, digits, angle: data.map((p) => p[0]), radius: data.map((p) => p[1]), path: line(data)});
        if (curveName === "bundle") continue;
        const constant = d3.areaRadial().curve(curve).defined(defined).digits(digits).innerRadius(15).outerRadius((p) => p[1]);
        radialAreas.push({curve: curveName, series: seriesName, digits, startAngle: data.map((p) => p[0]), innerRadius: [15], outerRadius: data.map((p) => p[1]), path: constant(data)});
        const varying = d3.areaRadial().curve(curve).defined(defined).digits(digits)
          .startAngle((p) => p[0]).endAngle((p) => p[0] + 0.1).innerRadius((p, i) => 5 + i).outerRadius((p) => p[1]);
        radialAreas.push({curve: curveName, series: seriesName, digits, startAngle: data.map((p) => p[0]), endAngle: data.map((p) => p[0] + 0.1),
          innerRadius: data.map((p, i) => 5 + i), outerRadius: data.map((p) => p[1]), path: varying(data)});
      }
    }
  }

  const pointRadial = [];
  for (const angle of [0, 1, -1, Math.PI / 2, Math.PI, 2 * Math.PI, 10.5, 1e-9]) {
    for (const radius of [0, 1, 37.5, -10]) pointRadial.push({angle, radius, point: d3.pointRadial(angle, radius)});
  }

  return {lines, areas, bundleArea, symbols, translatedSymbols, lists, links, radialLinks, radialLines, radialAreas, pointRadial};
}
