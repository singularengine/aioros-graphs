import {
  hierarchy, stratify, tree, cluster, treemap, pack, packSiblings, packEnclose, partition,
  treemapSquarify, treemapResquarify, treemapBinary, treemapDice, treemapSlice, treemapSliceDice,
} from "d3-hierarchy";
import {packSiblingsRandom} from "../node_modules/d3-hierarchy/src/pack/siblings.js";
import lcg from "../node_modules/d3-hierarchy/src/lcg.js";
import {version} from "../version.mjs";

export const d3Version = version("d3-hierarchy");

function random(seed) {
  return () => {
    seed |= 0; seed = seed + 0x6D2B79F5 | 0;
    let t = Math.imul(seed ^ seed >>> 15, 1 | seed);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}

function number(next, zeros, missing) {
  const r = next();
  if (r < zeros) return 0;
  if (r < zeros + missing) return undefined;
  return Math.round(next() * 1e5) / 100;
}

function randomTree(seed, size, {maxChildren = 6, maxDepth = 6, zeros = 0, missing = 0, inner = 0, stop = 0.25} = {}) {
  const next = random(seed);
  let count = 1;
  const root = {};
  const queue = [[root, 0]];
  while (queue.length && count < size) {
    const [node, depth] = queue.shift();
    if (depth >= maxDepth) continue;
    const k = Math.min(size - count, 1 + Math.floor(next() * maxChildren));
    if (depth > 0 && next() < stop) continue;
    node.children = Array.from({length: k}, () => ({}));
    count += k;
    for (const child of node.children) queue.push([child, depth + 1]);
  }
  let index = 0;
  (function name(node) {
    node.i = index++;
    if (node.children) node.children.forEach(name);
    else node.value = number(next, zeros, missing);
    if (node.children && next() < inner) node.value = number(next, 0, 0);
  })(root);
  return root;
}

function chain(length) {
  let node = {value: 3};
  for (let i = 1; i < length; i++) node = {children: [node]};
  let index = 0;
  (function name(n) { n.i = index++; if (n.children) n.children.forEach(name); })(node);
  return node;
}

function flat(count, seed) {
  const next = random(seed);
  return {i: 0, children: Array.from({length: count}, (_, k) => ({i: k + 1, value: Math.round(next() * 1000) / 10}))};
}

const trees = {
  single: {i: 0, value: 5},
  oneChild: {i: 0, children: [{i: 1, value: 2}]},
  three: {i: 0, children: [{i: 1, value: 1}, {i: 2, value: 2}, {i: 3, value: 3}]},
  chain: chain(6),
  flat: flat(40, 7),
  small: randomTree(1, 12),
  medium: randomTree(2, 60),
  zeros: randomTree(3, 45, {zeros: 0.3, missing: 0.1, inner: 0.2}),
  deep: randomTree(4, 120, {maxChildren: 2, maxDepth: 14, stop: 0.02}),
  large: randomTree(5, 400, {maxChildren: 8}),
  wide: flat(100, 9),
};

const byIndex = (nodes, f) => {
  const out = [];
  for (const node of nodes) out[node.data.i] = f(node);
  return out;
};

function structure(name) {
  const root = hierarchy(trees[name]);
  const nodes = root.descendants();
  const preOrder = [], postOrder = [];
  root.eachBefore((n) => preOrder.push(n.data.i));
  root.eachAfter((n) => postOrder.push(n.data.i));
  return {
    tree: name,
    parent: byIndex(nodes, (n) => n.parent ? n.parent.data.i : -1),
    depth: byIndex(nodes, (n) => n.depth),
    height: byIndex(nodes, (n) => n.height),
    children: byIndex(nodes, (n) => (n.children ?? []).map((c) => c.data.i)),
    descendants: nodes.map((n) => n.data.i),
    leaves: root.leaves().map((n) => n.data.i),
    preOrder,
    postOrder,
    links: root.links().map((l) => [l.source.data.i, l.target.data.i]),
    ancestors: byIndex(nodes, (n) => n.ancestors().map((a) => a.data.i)),
  };
}

function nodePaths(name) {
  const root = hierarchy(trees[name]);
  const nodes = root.descendants();
  const byI = [];
  for (const n of nodes) byI[n.data.i] = n;
  const next = random(11);
  const last = nodes.length - 1;
  const pairs = [[0, 0], [last, 0], [0, last], [last, last]];
  for (let k = 0; k < 6; k++) pairs.push([Math.floor(next() * nodes.length), Math.floor(next() * nodes.length)]);
  return {tree: name, pairs: pairs.map(([a, b]) => ({from: a, to: b, path: byI[a].path(byI[b]).map((n) => n.data.i)}))};
}

function values(name) {
  const summed = hierarchy(trees[name]).sum((d) => d.value);
  const counted = hierarchy(trees[name]).count();
  return {
    tree: name,
    values: byIndex(summed.descendants(), (n) => n.data.value ?? NaN),
    sum: byIndex(summed.descendants(), (n) => n.value),
    count: byIndex(counted.descendants(), (n) => n.value),
  };
}

const comparators = {
  valueDescending: (a, b) => b.value - a.value,
  valueAscending: (a, b) => a.value - b.value,
  heightThenValue: (a, b) => b.height - a.height || b.value - a.value,
  nanForThirds: (a, b) => (a.data.i % 3 === 0 ? NaN : b.value - a.value),
};

function sorts(name, comparator) {
  const root = hierarchy(trees[name]).sum((d) => d.value).sort(comparators[comparator]);
  return {tree: name, comparator, children: byIndex(root.descendants(), (n) => (n.children ?? []).map((c) => c.data.i))};
}

function summed(name, sort) {
  const root = hierarchy(trees[name]).sum((d) => d.value);
  return sort ? root.sort(comparators[sort]) : root;
}

const radialSeparation = (a, b) => (a.parent === b.parent ? 1 : 2) / a.depth;

const nodeSettings = [
  {},
  {size: [960, 500]},
  {nodeSize: [12, 80]},
  {size: [2 * Math.PI, 300], separation: "radial"},
  {nodeSize: [1, 1], separation: "radial"},
  {size: [960, 500], sort: "heightThenValue"},
];

function nodeLayout(make, name, settings) {
  const layout = make();
  if (settings.size) layout.size(settings.size);
  if (settings.nodeSize) layout.nodeSize(settings.nodeSize);
  if (settings.separation) layout.separation(radialSeparation);
  const root = layout(summed(name, settings.sort));
  const nodes = root.descendants();
  return {tree: name, ...settings, x: byIndex(nodes, (n) => n.x), y: byIndex(nodes, (n) => n.y)};
}

const tilings = {
  squarify: treemapSquarify,
  squarify1: treemapSquarify.ratio(1),
  squarify3: treemapSquarify.ratio(3),
  "squarify0.5": treemapSquarify.ratio(0.5),
  resquarify: treemapResquarify,
  binary: treemapBinary,
  dice: treemapDice,
  slice: treemapSlice,
  sliceDice: treemapSliceDice,
};

const paddings = [
  {},
  {padding: 1},
  {paddingInner: 2, paddingOuter: 3},
  {paddingInner: 1, paddingTop: 18, paddingRight: 2, paddingBottom: 4, paddingLeft: 6},
];

function configure(layout, settings) {
  for (const key of ["padding", "paddingInner", "paddingOuter", "paddingTop", "paddingRight", "paddingBottom", "paddingLeft"]) {
    if (settings[key] !== undefined) layout[key](settings[key]);
  }
  if (settings.round) layout.round(true);
  return layout;
}

function rectangles(root) {
  const nodes = root.descendants();
  return {x0: byIndex(nodes, (n) => n.x0), y0: byIndex(nodes, (n) => n.y0), x1: byIndex(nodes, (n) => n.x1), y1: byIndex(nodes, (n) => n.y1)};
}

function treemapCase(name, tiling, settings, size, sort) {
  const layout = configure(treemap().tile(tilings[tiling]).size(size), settings);
  return {tree: name, tiling, size, sort: sort ?? null, ...settings, ...rectangles(layout(summed(name, sort)))};
}

function treemapCases() {
  const cases = [];
  for (const tiling of Object.keys(tilings)) {
    for (const settings of paddings) {
      for (const round of [false, true]) cases.push(treemapCase("medium", tiling, {...settings, round}, [960, 500], "heightThenValue"));
    }
    for (const name of ["single", "three", "flat", "zeros", "large", "wide"]) cases.push(treemapCase(name, tiling, {}, [960, 500], "valueDescending"));
    cases.push(treemapCase("medium", tiling, {}, [960, 500]));
    for (const size of [[10, 10], [100, 0]]) {
      for (const settings of paddings) cases.push(treemapCase("small", tiling, settings, size, "valueDescending"));
    }
  }
  return cases;
}

function resquarifyCases() {
  const cases = [];
  for (const [first, second] of [["resquarify", "resquarify"], ["resquarify", "squarify"], ["resquarify", "resquarify2"]]) {
    for (const name of ["medium", "zeros", "large"]) {
      const data = structuredClone(trees[name]);
      const root = hierarchy(data).sum((d) => d.value).sort(comparators.valueDescending);
      treemap().tile(treemapResquarify).size([960, 500]).padding(1)(root);
      const next = random(name.length * 31);
      const values = [];
      root.each((n) => { values[n.data.i] = n.children ? NaN : Math.round(next() * 1e5) / 100; });
      root.sum((d) => (d.children ? 0 : values[d.i]));
      const tile = second === "resquarify2" ? treemapResquarify.ratio(2) : tilings[second];
      treemap().tile(tile).size([960, 500]).padding(1)(root);
      cases.push({tree: name, first, second, values, ...rectangles(root)});
    }
  }
  return cases;
}

function radii(name) {
  return byIndex(hierarchy(trees[name]).sum((d) => d.value).descendants(), (n) => Math.sqrt(n.value) * 3);
}

const packSettings = [
  {size: [960, 960]},
  {size: [800, 600], padding: 3},
  {size: [960, 960], radius: true},
  {size: [500, 500], radius: true, padding: 2},
  {size: [960, 960], sort: "valueDescending", padding: 1},
];

function packCases() {
  const cases = [];
  for (const name of ["single", "oneChild", "three", "small", "medium", "zeros", "large", "flat"]) {
    for (const settings of packSettings) {
      const layout = pack().size(settings.size);
      if (settings.padding !== undefined) layout.padding(settings.padding);
      const leafRadii = settings.radius ? radii(name) : null;
      if (leafRadii) layout.radius((n) => leafRadii[n.data.i]);
      const root = layout(summed(name, settings.sort));
      const nodes = root.descendants();
      cases.push({tree: name, ...settings, radii: leafRadii, x: byIndex(nodes, (n) => n.x), y: byIndex(nodes, (n) => n.y), r: byIndex(nodes, (n) => n.r)});
    }
  }
  return cases;
}

function radiusLists() {
  const next = random(21);
  return [
    [], [5], [5, 3], [1, 2, 3], [3, 3, 3, 3], [0, 0, 4, 0, 2], [10, 1, 1, 1, 1, 1, 1, 1],
    Array.from({length: 20}, () => 1 + Math.floor(next() * 20)),
    Array.from({length: 100}, () => Math.round(next() * 5000) / 100),
    Array.from({length: 300}, () => Math.sqrt(next() * 100)),
  ];
}

function packSiblingsCases() {
  return radiusLists().map((list) => {
    const circles = list.map((r) => ({r}));
    packSiblings(circles);
    const again = list.map((r) => ({r}));
    const radius = packSiblingsRandom(again, lcg());
    return {radii: list, x: circles.map((c) => c.x), y: circles.map((c) => c.y), radius};
  });
}

function circleLists() {
  const next = random(33);
  const scatter = (n) => Array.from({length: n}, () => ({x: Math.round(next() * 1e4) / 100, y: Math.round(next() * 1e4) / 100, r: Math.round(next() * 1e3) / 100}));
  return [
    [],
    [{x: 1, y: 2, r: 3}],
    [{x: 0, y: 0, r: 1}, {x: 10, y: 0, r: 2}],
    [{x: 0, y: 0, r: 1}, {x: 10, y: 0, r: 2}, {x: 5, y: 8, r: 1.5}],
    [{x: 0, y: 0, r: 10}, {x: 1, y: 1, r: 1}, {x: -2, y: 3, r: 2}],
    [{x: 3, y: 3, r: 2}, {x: 3, y: 3, r: 2}, {x: 3, y: 3, r: 2}],
    [{x: 0, y: 0, r: 0}, {x: 4, y: 0, r: 0}, {x: 2, y: 2, r: 0}, {x: 2, y: -1, r: 0}],
    scatter(10),
    scatter(50),
    scatter(200),
  ];
}

function packEncloseCases() {
  return circleLists().map((circles) => {
    const e = packEnclose(circles);
    return {circles: circles.map((c) => [c.x, c.y, c.r]), enclose: e ? [e.x, e.y, e.r] : null};
  });
}

const partitionSettings = [
  {},
  {size: [960, 500]},
  {size: [2 * Math.PI, 300]},
  {size: [960, 500], padding: 1},
  {size: [960, 500], padding: 1, round: true},
  {size: [2 * Math.PI, 300], sort: "heightThenValue"},
];

function partitionCases() {
  const cases = [];
  for (const name of ["single", "three", "chain", "small", "medium", "zeros", "large"]) {
    for (const settings of partitionSettings) {
      const layout = partition();
      if (settings.size) layout.size(settings.size);
      if (settings.padding !== undefined) layout.padding(settings.padding);
      if (settings.round) layout.round(true);
      cases.push({tree: name, ...settings, ...rectangles(layout(summed(name, settings.sort)))});
    }
  }
  return cases;
}

const stratifyRows = [
  [{id: "a"}, {id: "b", parentId: "a"}, {id: "c", parentId: "a"}, {id: "d", parentId: "b"}],
  [{id: "d", parentId: "b"}, {id: "c", parentId: "a"}, {id: "b", parentId: "a"}, {id: "a"}],
  [{id: "r", parentId: ""}, {id: "x", parentId: "r"}, {id: "", parentId: "r"}, {parentId: "x"}],
  [{id: "only"}],
  ...[3, 4].map((seed) => {
    const next = random(seed);
    const rows = [{id: "n0"}];
    for (let i = 1; i < 200; i++) rows.push({id: "n" + i, parentId: "n" + Math.floor(next() * i)});
    for (let i = rows.length - 1; i > 0; i--) {
      const j = Math.floor(next() * (i + 1));
      [rows[i], rows[j]] = [rows[j], rows[i]];
    }
    return rows;
  }),
];

function stratified(root, rows) {
  const row = new Map(rows.map((d, i) => [d, i]));
  const nodes = root.descendants();
  return nodes.map((n) => ({
    id: n.id ?? null,
    row: n.data === null ? -1 : row.get(n.data),
    parent: n.parent ? n.parent.id ?? null : null,
    parentRow: n.parent ? (n.parent.data === null ? -1 : row.get(n.parent.data)) : -2,
    depth: n.depth,
    height: n.height,
    children: (n.children ?? []).map((c) => (c.data === null ? c.id : row.get(c.data))),
  }));
}

function stratifyCases() {
  return stratifyRows.map((rows) => ({
    ids: rows.map((d) => d.id ?? null),
    parentIds: rows.map((d) => d.parentId ?? null),
    nodes: stratified(stratify()(rows), rows),
  }));
}

function error(run) {
  try {
    run();
    return null;
  } catch (e) {
    return e.message;
  }
}

const rejectedRows = [
  [{id: "a"}, {id: "b", parentId: "x"}],
  [{id: "r"}, {id: "a", parentId: "r"}, {id: "a", parentId: "r"}, {id: "b", parentId: "a"}],
  [{id: "a"}, {id: "b"}],
  [],
  [{id: "a", parentId: "b"}, {id: "b", parentId: "a"}],
  [{id: "r"}, {id: "a", parentId: "b"}, {id: "b", parentId: "a"}],
];

function stratifyErrorCases() {
  return rejectedRows.map((rows) => ({
    ids: rows.map((d) => d.id ?? null),
    parentIds: rows.map((d) => d.parentId ?? null),
    error: error(() => stratify()(rows)),
  }));
}

const pathLists = [
  ["a/b/c", "a/b/d", "a/e"],
  ["/", "/a", "/a/b"],
  ["a/", "a/b/", "a/b\\/c", "a/b\\\\/d"],
  ["x"],
  ["/root/a/b", "/root/a/c", "/root/d"],
  ["/flare/analytics/cluster/AgglomerativeCluster", "/flare/analytics/cluster/CommunityStructure", "/flare/analytics/graph/BetweennessCentrality",
    "/flare/animate/Easing", "/flare/animate/interpolate/ArrayInterpolator", "/flare/animate/interpolate/ColorInterpolator", "/flare/data/DataField",
    "/flare/display/DirtySprite", "/flare/vis/axis/Axes", "/flare/vis/axis/Axis", "/flare/vis/operator/layout/TreeMapLayout"],
  ["/a/b", "/a", "/c/d/e"],
];

function pathCases() {
  return pathLists.map((list) => {
    const rows = list.map((p) => ({p}));
    return {paths: list, nodes: stratified(stratify().path((d) => d.p)(rows), rows)};
  });
}

const parentLists = [
  [-1, 0, 0, 1],
  [2, 2, -1, 0, 0, 3],
  [-1],
  [1, 2, 3, -1],
];

const rejectedParents = [[5, -1], [-1, -1], [1, 0], [-1, 2, 1]];

function parentRows(list) {
  return list.map((p, i) => ({id: i, parentId: p < 0 ? null : p}));
}

function parentCases() {
  return [
    ...parentLists.map((list) => {
      const rows = parentRows(list);
      return {parents: list, nodes: stratified(stratify()(rows), rows), error: null};
    }),
    ...rejectedParents.map((list) => ({parents: list, nodes: null, error: error(() => stratify()(parentRows(list)))})),
  ];
}

export default function() {
  return {
    structure: Object.keys(trees).map(structure),
    trees: Object.entries(trees).map(([name, data]) => ({name, data})),
    nodePaths: ["three", "chain", "medium", "deep", "large"].map(nodePaths),
    values: Object.keys(trees).map(values),
    sorts: ["three", "flat", "small", "medium", "zeros", "large", "wide"].flatMap((name) =>
      Object.keys(comparators).filter((c) => c !== "nanForThirds" || name !== "wide").map((c) => sorts(name, c))),
    stratify: stratifyCases(),
    stratifyErrors: stratifyErrorCases(),
    paths: pathCases(),
    parents: parentCases(),
    tree: ["single", "oneChild", "three", "chain", "flat", "small", "medium", "deep", "large"].flatMap((name) => nodeSettings.map((s) => nodeLayout(tree, name, s))),
    cluster: ["single", "oneChild", "three", "chain", "flat", "small", "medium", "deep", "large"].flatMap((name) => nodeSettings.map((s) => nodeLayout(cluster, name, s))),
    treemap: treemapCases(),
    resquarify: resquarifyCases(),
    pack: packCases(),
    packSiblings: packSiblingsCases(),
    packEnclose: packEncloseCases(),
    partition: partitionCases(),
  };
}
