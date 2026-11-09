import {chord, chordDirected, chordTranspose, ribbon, ribbonArrow} from "d3-chord";
import {ascending, descending} from "d3-array";
import {path, pathRound} from "d3-path";
import {version} from "../version.mjs";

export const d3Version = version("d3-chord");

function random(seed) {
  return () => {
    seed |= 0; seed = seed + 0x6D2B79F5 | 0;
    let t = Math.imul(seed ^ seed >>> 15, 1 | seed);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}

const next = random(7);

const matrices = {
  example: [[11975, 5871, 8916, 2868], [1951, 10048, 2060, 6171], [8010, 16145, 8090, 8045], [1013, 990, 940, 6907]],
  ties: Array.from({length: 8}, () => Array.from({length: 8}, () => Math.floor(next() * 5))),
  selfLoops: [[5, 0, 2], [0, 3, 0], [2, 0, 0]],
  emptyRow: [[0, 0, 0], [1, 0, 2], [3, 4, 0]],
  zeros: [[0, 0], [0, 0]],
  single: [[7]],
  none: [],
  nan: [[1, NaN, 2], [3, 4, 5], [6, 7, 8]],
  negative: [[1, -2, 3], [4, 5, -6], [7, 8, 9]],
  asymmetric: [[0, 10, 0, 1], [2, 0, 8, 0], [0, 0, 0, 5], [9, 1, 1, 0]],
};

const orders = {none: null, ascending, descending};

const settings = [
  ...[0, 0.05, 2, -1, NaN].map((padAngle) => ({padAngle, sortGroups: "none", sortSubgroups: "none", sortChords: "none"})),
  ...[
    ["ascending", "none", "none"], ["descending", "none", "none"], ["none", "ascending", "none"], ["none", "descending", "none"],
    ["none", "none", "ascending"], ["none", "none", "descending"], ["descending", "descending", "descending"],
    ["ascending", "descending", "ascending"],
  ].map(([sortGroups, sortSubgroups, sortChords]) => ({padAngle: 0.05, sortGroups, sortSubgroups, sortChords})),
];

const kinds = {chord, directed: chordDirected, transpose: chordTranspose};

const end = (g) => [g.index, g.startAngle, g.endAngle, g.value];

function layout(kind, matrix, s) {
  const c = kinds[kind]().padAngle(s.padAngle);
  if (orders[s.sortGroups]) c.sortGroups(orders[s.sortGroups]);
  if (orders[s.sortSubgroups]) c.sortSubgroups(orders[s.sortSubgroups]);
  if (orders[s.sortChords]) c.sortChords(orders[s.sortChords]);
  return c(matrix);
}

function chordCases(kind) {
  const cases = [];
  for (const [name, matrix] of Object.entries(matrices)) {
    for (const s of settings) {
      const chords = layout(kind, matrix, s);
      cases.push({
        name, matrix: matrix.flat(), n: matrix.length, ...s,
        groups: chords.groups.map(end), chords: chords.map((d) => [...end(d.source), ...end(d.target)]),
      });
    }
  }
  return cases;
}

const make = (digits) => digits == null ? path() : pathRound(digits);

function draw(ribbons, padAngle, headRadius, digits) {
  const context = make(digits);
  const r = (headRadius == null ? ribbon() : ribbonArrow().headRadius(headRadius)).padAngle(padAngle).context(context);
  for (const [sr, ss, se, tr, ts, te] of ribbons) {
    r.sourceRadius(sr).targetRadius(tr)({source: {startAngle: ss, endAngle: se}, target: {startAngle: ts, endAngle: te}});
  }
  return context + "";
}

function standalone([sr, ss, se, tr, ts, te], padAngle, headRadius) {
  const r = (headRadius == null ? ribbon() : ribbonArrow().headRadius(headRadius)).padAngle(padAngle).sourceRadius(sr).targetRadius(tr);
  return r({source: {startAngle: ss, endAngle: se}, target: {startAngle: ts, endAngle: te}});
}

function ribbonSets() {
  const sets = [];
  const ends = (chords, sr, tr) => chords.map((d) => [sr, d.source.startAngle, d.source.endAngle, tr, d.target.startAngle, d.target.endAngle]);
  const example = layout("chord", matrices.example, settings[0]);
  const padded = layout("chord", matrices.example, settings[1]);
  for (const ribbon of ends(example, 100, 100)) sets.push({single: true, ribbons: [ribbon]});
  sets.push({ribbons: ends(padded, 240.5, 240.5)});
  sets.push({ribbons: ends(layout("chord", matrices.selfLoops, settings[1]), 100, 80)});
  sets.push({ribbons: ends(layout("directed", matrices.asymmetric, settings[1]), 240.5, 230)});
  sets.push({single: true, ribbons: [[100, 2, 1, 90, 4, 3.5]]});
  sets.push({single: true, ribbons: [[100, 1, 1.5, 100, 1, 1.5]]});
  sets.push({single: true, ribbons: [[100, 0, 0, 100, 3, 3.01]]});
  return sets;
}

function ribbonCases(arrow) {
  const cases = [];
  const heads = arrow ? [10, 0, 25] : [null];
  for (const set of ribbonSets()) {
    for (const padAngle of [0, 0.02, 1]) {
      for (const headRadius of heads) {
        for (const digits of [null, 3]) {
          cases.push({ribbons: set.ribbons, padAngle, headRadius, digits, path: draw(set.ribbons, padAngle, headRadius, digits)});
        }
        if (set.single) cases.push({ribbons: set.ribbons, padAngle, headRadius, digits: null, standalone: true, path: standalone(set.ribbons[0], padAngle, headRadius)});
      }
    }
  }
  return cases;
}

export default function() {
  return {
    chords: chordCases("chord"),
    directed: chordCases("directed"),
    transpose: chordCases("transpose"),
    ribbons: ribbonCases(false),
    arrows: ribbonCases(true),
  };
}
