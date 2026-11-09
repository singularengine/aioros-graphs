import {sankey, sankeyLeft, sankeyRight, sankeyCenter, sankeyJustify, sankeyLinkHorizontal} from "d3-sankey";
import {version} from "../version.mjs";

export const d3Version = version("d3-sankey");

function random(seed) {
  return () => {
    seed |= 0; seed = seed + 0x6D2B79F5 | 0;
    let t = Math.imul(seed ^ seed >>> 15, 1 | seed);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}

function energy() {
  const next = random(7);
  const layers = [6, 9, 12, 10, 7, 4];
  const starts = [];
  let count = 0;
  for (const size of layers) { starts.push(count); count += size; }
  const links = [];
  const seen = new Set();
  for (let l = 0; l < layers.length - 1; l++) {
    for (let k = 0; k < layers[l]; k++) {
      const source = starts[l] + k;
      const fanout = 1 + Math.floor(next() * 3);
      for (let f = 0; f < fanout; f++) {
        const skip = next() < 0.2 && l + 2 < layers.length ? 2 : 1;
        const target = starts[l + skip] + Math.floor(next() * layers[l + skip]);
        if (seen.has(source * 1000 + target)) continue;
        seen.add(source * 1000 + target);
        links.push([source, target, Math.round(next() * 1000) / 10 + 0.5]);
      }
    }
  }
  for (let k = 0; k < layers[layers.length - 1]; k++) {
    const target = starts[layers.length - 1] + k;
    if (!links.some(([, t]) => t === target)) links.push([starts[layers.length - 2], target, 3.25]);
  }
  return {nodes: count, links};
}

const graphs = {
  energy: energy(),
  chain: {nodes: 3, links: [[0, 1, 5], [1, 2, 5]]},
  diamond: {nodes: 4, links: [[0, 1, 3], [0, 2, 7], [1, 3, 3], [2, 3, 7]]},
  sources: {nodes: 6, links: [[0, 3, 10], [1, 3, 4], [2, 4, 6], [3, 5, 9], [4, 5, 6], [1, 5, 2]]},
  skips: {nodes: 6, links: [[0, 1, 4], [1, 2, 4], [2, 3, 4], [0, 3, 2], [4, 3, 1], [0, 5, 1], [5, 2, 1]]},
  isolated: {nodes: 5, links: [[0, 1, 2], [1, 3, 2]]},
  zeros: {nodes: 5, links: [[0, 1, 0], [0, 2, 3], [1, 3, NaN], [2, 3, 3], [2, 4, 0]]},
  fixed: {nodes: 4, links: [[0, 1, 2], [0, 2, 2], [1, 3, 1], [2, 3, 2]], fixed: [NaN, 5, NaN, 9]},
  nolinks: {nodes: 3, links: []},
  single: {nodes: 1, links: []},
  fan: {nodes: 7, links: [[0, 1, 1], [0, 2, 2], [0, 3, 3], [0, 4, 4], [0, 5, 5], [0, 6, 6]]},
};

const aligns = {justify: sankeyJustify, left: sankeyLeft, right: sankeyRight, center: sankeyCenter};

const settings = [
  {},
  {extent: [[1, 5], [959, 495]], nodeWidth: 15, nodePadding: 10},
  {extent: [[1, 5], [959, 495]], nodeWidth: 15, nodePadding: 10, align: "left"},
  {extent: [[1, 5], [959, 495]], nodeWidth: 15, nodePadding: 10, align: "right"},
  {extent: [[1, 5], [959, 495]], nodeWidth: 15, nodePadding: 10, align: "center"},
  {extent: [[0, 0], [640, 400]], nodeWidth: 20, nodePadding: 40, iterations: 0},
  {extent: [[0, 0], [640, 400]], nodeWidth: 20, nodePadding: 40, iterations: 1},
  {extent: [[0, 0], [640, 400]], nodePadding: 2.5, iterations: 32},
  {extent: [[10, 20], [300, 60]], nodeWidth: 4, nodePadding: 30},
];

const nodeSorts = {
  input: null,
  valueDescending: (a, b) => b.value - a.value,
  indexDescending: (a, b) => b.index - a.index,
};

const linkSorts = {
  input: null,
  valueDescending: (a, b) => b.value - a.value,
};

function graphOf(graph, ids) {
  return {
    nodes: Array.from({length: graph.nodes}, (_, i) => {
      const node = ids ? {id: ids[i]} : {};
      if (graph.fixed && !Number.isNaN(graph.fixed[i])) node.fixedValue = graph.fixed[i];
      return node;
    }),
    links: graph.links.map(([source, target, value]) => ({
      source: ids ? ids[source] : source, target: ids ? ids[target] : target, value,
    })),
  };
}

function layout(graph, setting, ids) {
  const generator = sankey();
  if (setting.extent) generator.extent(setting.extent);
  if (setting.nodeWidth !== undefined) generator.nodeWidth(setting.nodeWidth);
  if (setting.nodePadding !== undefined) generator.nodePadding(setting.nodePadding);
  if (setting.iterations !== undefined) generator.iterations(setting.iterations);
  if (setting.align) generator.nodeAlign(aligns[setting.align]);
  if (setting.nodeSort !== undefined) generator.nodeSort(nodeSorts[setting.nodeSort]);
  if (setting.linkSort !== undefined) generator.linkSort(linkSorts[setting.linkSort]);
  if (ids) generator.nodeId((d) => d.id);
  const result = generator(graphOf(graph, ids));
  const shape = sankeyLinkHorizontal();
  return {
    nodes: result.nodes.map((n) => ({
      x0: n.x0, x1: n.x1, y0: n.y0, y1: n.y1, value: n.value, depth: n.depth, height: n.height, layer: n.layer,
      sourceLinks: n.sourceLinks.map((l) => l.index), targetLinks: n.targetLinks.map((l) => l.index),
    })),
    links: result.links.map((l) => ({y0: l.y0, y1: l.y1, width: l.width, path: shape(l)})),
    joined: result.links.map((l) => shape(l)).join(""),
  };
}

function cases(names, settingList, extra = {}) {
  const list = [];
  for (const name of names) {
    for (const setting of settingList) {
      const graph = graphs[name];
      const options = {...setting, ...extra};
      list.push({graph: name, nodes: graph.nodes, links: graph.links, fixed: graph.fixed ?? null, options, ids: extra.ids ?? null,
        result: layout(graph, options, extra.ids)});
    }
  }
  return list;
}

function error(run) {
  try {
    run();
    return null;
  } catch (e) {
    return e.message;
  }
}

const cycles = {
  self: {nodes: 2, links: [[0, 1, 1], [1, 1, 1]]},
  pair: {nodes: 2, links: [[0, 1, 1], [1, 0, 1]]},
  behind: {nodes: 4, links: [[0, 1, 1], [1, 2, 1], [2, 3, 1], [3, 1, 1]]},
};

export default function() {
  const layouts = cases(Object.keys(graphs), settings);
  const idCases = [];
  for (const name of ["energy", "diamond", "sources"]) {
    const graph = graphs[name];
    const ids = Array.from({length: graph.nodes}, (_, i) => `n${(i * 7) % graph.nodes}`);
    idCases.push({graph: name, nodes: graph.nodes, links: graph.links, fixed: null, options: settings[1], ids, result: layout(graph, settings[1], ids)});
  }
  const dupIds = ["a", "b", "a", "c"];
  idCases.push({graph: "duplicate", nodes: 4, links: [[1, 2, 3], [2, 3, 3]], fixed: null, options: settings[1], ids: dupIds,
    result: layout({nodes: 4, links: [[1, 2, 3], [2, 3, 3]]}, settings[1], dupIds)});
  return {
    layouts,
    ids: idCases,
    nodeSorts: Object.keys(nodeSorts).flatMap((sort) => cases(["energy", "sources", "skips", "fixed"], [settings[1], settings[7]], {nodeSort: sort})),
    linkSorts: Object.keys(linkSorts).flatMap((sort) => cases(["energy", "sources", "skips", "fan"], [settings[1], settings[7]], {linkSort: sort})),
    cycles: Object.entries(cycles).map(([name, graph]) => ({graph: name, nodes: graph.nodes, links: graph.links, error: error(() => layout(graph, {}))})),
  };
}
