import * as i from "d3-interpolate";
import {color, rgb, hsl} from "d3-color";
import {version} from "../version.mjs";

export const d3Version = version("d3-interpolate");

const ts = [0, 1, 0.5, 0.25, 0.1, 0.3, 1 / 3, 0.7, 0.9, 0.999, -0.5, 1.5, 2, -1e-9, NaN];
const colorTs = [0, 1, 0.5, 0.25, 0.1, 0.3, 1 / 3, 0.7, 0.9, 0.999, -0.25, 1.25];

const numberPairs = [[0, 1], [1, 0], [-3, 7.5], [2, 2], [0.1, 0.2], [1e20, -1e20], [0, Infinity], [-Infinity, 5], [NaN, 3], [3, NaN], [-0, 0]];

const colorPairs = [
  ["red", "blue"], ["steelblue", "brown"], ["#ffffff", "#000000"], ["white", "steelblue"], ["black", "gray"], ["rgba(255,0,0,0.5)", "#00ff00"],
  ["transparent", "red"], ["red", "transparent"], ["hsl(30,50%,50%)", "hsl(330,50%,50%)"], ["hsl(10,80%,40%)", "hsl(250,60%,70%)"],
  ["#808080", "orange"], ["purple", "#e0e0e0"], ["hsla(120,100%,50%,0.2)", "hsl(300,100%,50%)"], ["yellow", "navy"], ["#3366cc", "#99cc33"],
];

const colorLists = [["red"], ["red", "blue"], ["#440154", "#21918c", "#fde725"], ["red", "yellow", "green", "cyan", "blue", "magenta"],
  ["white", "notacolor", "black"], ["rgba(0,0,255,0.5)", "hsl(120,50%,50%)", "orange", "#123456"]];

function tag(v) {
  if (v === undefined) return ["u"];
  if (v === null) return ["null"];
  if (typeof v === "boolean") return ["b", v];
  if (typeof v === "number") return ["n", v];
  if (typeof v === "string") return ["s", v];
  if (v instanceof Date) return ["d", +v];
  if (v instanceof Float64Array) return ["f", Array.from(v)];
  if (Array.isArray(v)) return ["a", v.map(tag)];
  if (v instanceof color) {
    const h = v.constructor.name === "Hsl";
    return ["c", h ? "hsl" : "rgb", ...(h ? [v.h, v.s, v.l] : [v.r, v.g, v.b]), v.opacity];
  }
  return ["o", Object.keys(v).map((k) => [k, tag(v[k])])];
}

const stringPairs = [["", ""], ["foo", "bar"], ["12px", "24px"], ["translate(10,20) scale(2)", "translate(30,-5) scale(0.5)"], ["M0,0L10,10", "M5,5L20,30Z"],
  ["1e3", "-2.5e-2"], [".5", "5."], ["a1b2c3", "x10y"], ["10 20 30", "1"], ["rgb", "1 2"], ["-.5e+3x", "+7e2x"], ["top 1", "top 1"], ["1 2 3", "1 2 3 4"],
  ["abc", "123"], ["0.1 0.2", "0.30000000000000004 1e21"], ["1e400", "2"], ["--1", "-+2"], ["1.2.3", "4.5.6"], ["5e", "7E+"], ["1 2", "x"], ["", "a1"],
  ["3", ""], ["1px 2px", "a 5px b 6px c"], ["0.000001", "0.0000001"], ["123456789012345678901", "1"]];

const datePairs = [[0, 1e12], [Date.UTC(2024, 0, 1), Date.UTC(2024, 11, 31)], [-1e15, 8.64e15], [5, 5], [0, 1]];

const valuePairs = () => [
  [0, 10], ["5", 10], [" 0x1f ", 10], ["", 10], ["1e400", 1], [null, 10], [undefined, 10], [true, 10], [[1, 2], 5], [[7], 3], [new Date(1000), 2000],
  [{a: 1}, 3], ["red", "blue"], ["#fff", "steelblue"], [5, "red"], ["notacolor", "red"], [["red"], "blue"], ["a1", "b2"], [3, "x9"], [null, "x9"],
  [true, "a1"], [[1, [2, 3]], "4,5,6"], [{a: 1}, "x[object Object]1"], [new Float64Array([1.5, 2]), "0 0"], [null, null], [1, true], [1, undefined],
  [new Date(0), new Date(1e12)], [0, new Date(1000)], ["1970", new Date(0)], [new Float64Array([1, 2]), new Float64Array([3, 4, 5])],
  [[1, 2], new Float64Array([3, 4])], ["12", new Float64Array([30, 40])], [null, new Float64Array([1])], [[1, "a", "red"], [2, "b9", "blue", 4]],
  [{x: 1, y: "red"}, {x: 3, y: "blue", z: 5}], [{}, {1: 2, b: 3, 0: 1}], [[5], {0: 10, length: 3}], [null, {a: 1}], [{a: [1, 2]}, {a: [3, 4]}],
  [{a: {b: 1}}, {a: {b: 2}}], [rgb(10, 20, 30), "white"], ["red", hsl(120, 0.5, 0.5)], [rgb(10, 20, 30, 0.5), 5], [[1, 2], "3"], ["ab", [1, 2, 3]],
  [5, ["x1"]], [{0: 4, 2: 8}, [10, 20, 30]], [{b: 1, a: 2, 10: 3, 2: 4}, {a: 5, 2: 6, c: 7, 10: 8, b: 9}], [rgb(10, 20, 30), {r: 100, g: 0}],
  [hsl(30, 0.5, 0.5), {h: 90, l: 1}], [[3], {length: 5}], [new Float64Array([1, 2]), {1: 10}], ["xyz", ["a", "b"]], ["7", {0: 1}],
];

export default function() {
  const number = numberPairs.map(([a, b]) => ({a, b, ts, values: ts.map((t) => i.interpolateNumber(a, b)(t)), rounded: ts.map((t) => i.interpolateRound(a, b)(t))}));
  const hue = [[0, 90], [350, 10], [10, 350], [0, 270], [90, -90], [0, 180], [720, 10], [NaN, 40], [40, NaN], [NaN, NaN], [-30, 400], [5, 5]]
    .map(([a, b]) => ({a, b, ts, values: ts.map((t) => i.interpolateHue(a, b)(t))}));
  const basisLists = [[5], [0, 10], [1, 3, 2], [0, 10, -5, 20, 7], [2, 2, 2, 2], [0, 100, 0, 100, 0, 100, 0]];
  const basis = basisLists.map((values) => ({values, ts, open: ts.map((t) => i.interpolateBasis(values)(t)), closed: ts.map((t) => i.interpolateBasisClosed(values)(t))}));
  const discrete = [[1], [1, 2, 3], [10, 20, 30, 40, 50]].map((values) => ({values, ts: ts.filter((t) => !Number.isNaN(t)), outputs: ts.filter((t) => !Number.isNaN(t)).map((t) => i.interpolateDiscrete(values)(t))}));
  const numberArray = [[[0, 1, 2], [10, 20, 30]], [[0, 1], [10, 20, 30]], [[0, 1, 2], [10, 20]], [[], [1, 2]], [[5], []], [[NaN, 1], [2, 3]]]
    .map(([a, b]) => ({a, b, ts, values: ts.map((t) => Array.from(i.interpolateNumberArray(Float64Array.from(a), Float64Array.from(b))(t)))}));
  const colors = colorPairs.map(([a, b]) => ({a, b, ts: colorTs, outputs: {
    rgb: colorTs.map((t) => i.interpolateRgb(a, b)(t)),
    rgbGamma: colorTs.map((t) => i.interpolateRgb.gamma(2.2)(a, b)(t)),
    hsl: colorTs.map((t) => i.interpolateHsl(a, b)(t)),
    hslLong: colorTs.map((t) => i.interpolateHslLong(a, b)(t)),
    lab: colorTs.map((t) => i.interpolateLab(a, b)(t)),
    hcl: colorTs.map((t) => i.interpolateHcl(a, b)(t)),
    hclLong: colorTs.map((t) => i.interpolateHclLong(a, b)(t)),
    cubehelix: colorTs.map((t) => i.interpolateCubehelix(a, b)(t)),
    cubehelixLong: colorTs.map((t) => i.interpolateCubehelixLong(a, b)(t)),
    cubehelixGamma: colorTs.map((t) => i.interpolateCubehelix.gamma(3)(a, b)(t)),
    cubehelixLongGamma: colorTs.map((t) => i.interpolateCubehelixLong.gamma(3)(a, b)(t)),
  }}));
  const rgbBasis = colorLists.map((list) => ({colors: list, ts: colorTs, open: colorTs.map((t) => i.interpolateRgbBasis(list)(t)), closed: colorTs.map((t) => i.interpolateRgbBasisClosed(list)(t))}));
  const piecewise = [
    {numbers: [0, 10, 5, 100], ts: colorTs, values: colorTs.map((t) => i.piecewise(i.interpolateNumber, [0, 10, 5, 100])(t))},
    {numbers: [3, 4], ts: colorTs, values: colorTs.map((t) => i.piecewise(i.interpolateNumber, [3, 4])(t))},
    {colors: ["red", "green", "blue"], ts: colorTs, outputs: colorTs.map((t) => i.piecewise(i.interpolateRgb, ["red", "green", "blue"])(t))},
    {colors: ["#fff", "steelblue", "#000", "orange"], ts: colorTs, outputs: colorTs.map((t) => i.piecewise(i.interpolateRgb, ["#fff", "steelblue", "#000", "orange"])(t))},
  ];
  const quantize = [1, 2, 5].map((n) => ({n, numbers: i.quantize(i.interpolateNumber(-2, 8), n), colors: i.quantize(i.interpolateRgb("red", "blue"), n)}));
  const zoomPairs = [[[0, 0, 100], [100, 50, 100]], [[50, 50, 200], [50, 50, 20]], [[0, 0, 1], [300, 200, 60]], [[10, 20, 30], [10, 20, 30]], [[-50, 10, 400], [80, -30, 25]]];
  const zoom = [];
  for (const rho of [null, 1, Math.SQRT2, 4]) {
    for (const [p0, p1] of zoomPairs) {
      const z = rho == null ? i.interpolateZoom(p0, p1) : i.interpolateZoom.rho(rho)(p0, p1);
      zoom.push({rho, p0, p1, ts: colorTs, duration: z.duration, values: colorTs.map((t) => z(t))});
    }
  }
  const string = stringPairs.map(([a, b]) => ({a, b, ts, outputs: ts.map((t) => i.interpolateString(a, b)(t))}));
  const date = datePairs.map(([a, b]) => ({a, b, ts, values: ts.map((t) => +i.interpolateDate(new Date(a), new Date(b))(t))}));
  const value = valuePairs().map(([a, b]) => {
    let f;
    try { f = i.interpolate(a, b); } catch (e) { return {a: tag(a), b: tag(b), error: e.constructor.name}; }
    return {a: tag(a), b: tag(b), ts: colorTs, outputs: colorTs.map((t) => tag(f(t)))};
  });
  return {number, hue, basis, discrete, numberArray, colors, rgbBasis, piecewise, quantize, zoom, string, date, value};
}
