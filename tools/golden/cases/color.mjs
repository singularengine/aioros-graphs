import {color, rgb, hsl, lab, hcl, lch, cubehelix, gray} from "d3-color";
import {version} from "../version.mjs";

export const d3Version = version("d3-color");

const named = ["aliceblue", "black", "white", "steelblue", "rebeccapurple", "yellowgreen", "grey", "darkslategray", "red", "lime", "blue",
  "orange", "gold", "navy", "teal", "silver", "fuchsia", "olive", "maroon", "coral", "tomato", "mediumvioletred", "lightgoldenrodyellow"];

const accepted = [
  "#f00", "#F0a", "#0f08", "#abcd", "#ff0000", "#4682B4", "#00000000", "#ff000080", "#12345678", "  #abc  ", "\n#fff\t",
  "rgb(255,0,0)", "rgb(255, 128, 0)", "rgb( 12 , 34 , 56 )", "rgb(300,-20,+7)", "rgb(010,0020,3)", "RGB(1,2,3)",
  "rgb(100%,0%,50%)", "rgb(12.5%, 33.3%, 1e1%)", "rgb(-10%, 110%, .5%)", "rgb(50%,50%,50%)",
  "rgba(255,0,0,0.5)", "rgba(255, 0, 0, 0)", "rgba(10,20,30,-1)", "rgba(10,20,30,2)", "rgba(1,2,3,1e-1)", "rgba(1,2,3,.25)",
  "rgba(100%,50%,0%,0.3)", "rgba(10%,20%,30%,0)",
  "hsl(120,50%,50%)", "hsl(0, 100%, 50%)", "hsl(-60,100%,50%)", "hsl(400,20%,70%)", "hsl(1e2,50%,50%)", "hsl(30.5, 10.25%, 90%)",
  "hsl(120,0%,40%)", "hsl(120,50%,0%)", "hsl(120,50%,100%)", "hsl(120,-10%,50%)", "hsl(120,150%,50%)", "hsl(120,50%,120%)",
  "hsla(240,100%,50%,0.75)", "hsla(240,100%,50%,0)", "hsla(10,20%,30%,1)", "hsla(10,20%,30%,1.5)",
  ...named, "AliceBlue", " Navy ", "transparent", "TRANSPARENT",
  "#fefefe", "#010101", "#808080", "#7f7f7f", "#3366cc", "#99cc33", "#c0ffee", "#bada55", "#e0e0e0", "#123", "#0a0b0c",
];

const rejected = ["#ff00f", "#ff00ff0", "ff0000", "#ggg", "#", "", " ", "rgb(1.5,2,3)", "rgb(1,2,3%)", "rgb(1,2)", "rgb(1,2,3,4)",
  "rgba(1,2,3)", "hsl(1,2,3)", "hsl(1%,2%,3%)", "hsla(1,2%,3%)", "notacolor", "red ish", "rgb (1,2,3)", "constructor", "#+12"];

const ks = [undefined, 1, 2.5, -1];

function channels(c) {
  if (c instanceof rgb) return {space: "rgb", values: [c.r, c.g, c.b, c.opacity]};
  if (c instanceof hsl) return {space: "hsl", values: [c.h, c.s, c.l, c.opacity]};
  if (c instanceof lab) return {space: "lab", values: [c.l, c.a, c.b, c.opacity]};
  if (c instanceof hcl) return {space: "hcl", values: [c.h, c.c, c.l, c.opacity]};
  return {space: "cubehelix", values: [c.h, c.s, c.l, c.opacity]};
}

function formats(c) {
  return {hex: c.formatHex(), hex8: c.formatHex8(), rgb: c.formatRgb(), hsl: c.formatHsl(), text: c.toString(), displayable: c.displayable()};
}

function shades(c) {
  return {brighter: ks.map((k) => c.brighter(k).formatRgb()), darker: ks.map((k) => c.darker(k).formatRgb()),
    brighterValues: ks.map((k) => channels(c.brighter(k)).values), darkerValues: ks.map((k) => channels(c.darker(k)).values)};
}

function clamped(c) {
  return c.clamp ? channels(c.clamp()).values : null;
}

function describe(c) {
  return {...channels(c), ...formats(c), ...shades(c), clamp: clamped(c)};
}

function brief(c) {
  return {...channels(c), ...formats(c), brighter: ks.map((k) => c.brighter(k).formatRgb()), darker: ks.map((k) => c.darker(k).formatRgb()), clamp: clamped(c)};
}

function conversions(c) {
  return {hsl: brief(hsl(c)), lab: brief(lab(c)), hcl: brief(hcl(c)), cubehelix: brief(cubehelix(c)), rgb: brief(rgb(c))};
}

export default function() {
  const parse = accepted.map((specifier) => {
    const c = color(specifier);
    return {specifier, ...describe(c), conversions: conversions(c)};
  });
  const rejectedCases = rejected.map((specifier) => ({specifier, parsed: color(specifier) != null, rgb: channels(rgb(specifier)).values}));
  const constructed = [];
  const quads = [[255, 0, 0, 1], [300, -20, 127.5, 0.5], [NaN, 20, 30, 1], [12.4, 12.5, 12.6, 0], [-0.4, 255.4, 255.5, 1.2], [100, 50, 25, NaN],
    [0, 0, 0, 1], [1, 1, 1, 1], [0.5, 0.5, 0.5, 1], [200, 0.3, 0.6, 1], [-30, 1.5, 0.2, 1], [720.5, 0.5, 0.25, 0.75], [NaN, NaN, 0.5, 1],
    [50, 30, -20, 1], [75, 0, 0, 1], [120, 40, 60, 1], [-100, 0.75, 0.35, 1], [300, 0.5, 0, 1], [80, 1.5, 0.8, 1]];
  for (const [p, q, r, a] of quads) {
    constructed.push({kind: "rgb", input: [p, q, r, a], ...describe(rgb(p, q, r, a)), conversions: conversions(rgb(p, q, r, a))});
    constructed.push({kind: "hsl", input: [p, q, r, a], ...describe(hsl(p, q, r, a)), conversions: conversions(hsl(p, q, r, a))});
    constructed.push({kind: "lab", input: [p, q, r, a], ...describe(lab(p, q, r, a)), conversions: conversions(lab(p, q, r, a))});
    constructed.push({kind: "hcl", input: [p, q, r, a], ...describe(hcl(p, q, r, a)), conversions: conversions(hcl(p, q, r, a))});
    constructed.push({kind: "lch", input: [p, q, r, a], ...describe(lch(p, q, r, a)), conversions: conversions(lch(p, q, r, a))});
    constructed.push({kind: "cubehelix", input: [p, q, r, a], ...describe(cubehelix(p, q, r, a)), conversions: conversions(cubehelix(p, q, r, a))});
    constructed.push({kind: "gray", input: [p, q, r, a], ...describe(gray(p, a)), conversions: conversions(gray(p, a))});
  }
  return {parse, rejected: rejectedCases, constructed};
}
