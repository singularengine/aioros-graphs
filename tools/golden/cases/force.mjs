import {forceSimulation, forceLink, forceManyBody, forceCenter, forceCollide, forceX, forceY} from "d3-force";
import {version} from "../version.mjs";

export const d3Version = version("d3-force");

function random(seed) {
  return () => {
    seed |= 0; seed = seed + 0x6D2B79F5 | 0;
    let t = Math.imul(seed ^ seed >>> 15, 1 | seed);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}

function connected(n, extra, seed) {
  const next = random(seed);
  const links = [];
  for (let i = 1; i < n; i++) links.push([Math.floor(next() * i), i]);
  while (links.length < n - 1 + extra) {
    const a = Math.floor(next() * n), b = Math.floor(next() * n);
    if (a !== b) links.push([a, b]);
  }
  return links;
}

const miserables = {n: 77, links: connected(77, 178, 7)};
const chain = {n: 5, links: [[0, 1], [1, 2], [2, 3], [3, 4]]};
const star = {n: 9, links: [[0, 1], [0, 2], [0, 3], [0, 4], [0, 5], [5, 6], [6, 7], [7, 8], [8, 5], [3, 3]]};
const coincident = {n: 6, x: [10, 10, 10, 10, 40, 40], y: [5, 5, 5, 5, -20, -20], links: [[0, 1], [1, 2], [2, 3], [3, 4], [4, 5], [5, 0]]};
const scattered = (() => {
  const next = random(11);
  const n = 60;
  return {n, x: Array.from({length: n}, () => Math.round(next() * 400 - 200)), y: Array.from({length: n}, () => Math.round(next() * 300 - 150)), links: connected(n, 30, 13)};
})();
const graphs = {chain, star, coincident, scattered, miserables};

const perNode = (n, f) => Array.from({length: n}, (_, i) => f(i));

function build(spec, n, links) {
  switch (spec.type) {
    case "link": {
      const force = forceLink(links.map(([source, target]) => ({source, target})));
      if (spec.distance != null) force.distance(Array.isArray(spec.distance) ? (_, i) => spec.distance[i] : spec.distance);
      if (spec.strength != null) force.strength(Array.isArray(spec.strength) ? (_, i) => spec.strength[i] : spec.strength);
      if (spec.iterations != null) force.iterations(spec.iterations);
      return force;
    }
    case "manyBody": {
      const force = forceManyBody();
      if (spec.strength != null) force.strength(Array.isArray(spec.strength) ? (_, i) => spec.strength[i] : spec.strength);
      if (spec.theta != null) force.theta(spec.theta);
      if (spec.distanceMin != null) force.distanceMin(spec.distanceMin);
      if (spec.distanceMax != null) force.distanceMax(spec.distanceMax);
      return force;
    }
    case "center": {
      const force = forceCenter(spec.x, spec.y);
      if (spec.strength != null) force.strength(spec.strength);
      return force;
    }
    case "collide": {
      const force = forceCollide(Array.isArray(spec.radius) ? (_, i) => spec.radius[i] : spec.radius);
      if (spec.strength != null) force.strength(spec.strength);
      if (spec.iterations != null) force.iterations(spec.iterations);
      return force;
    }
    case "x":
    case "y": {
      const force = (spec.type === "x" ? forceX : forceY)(Array.isArray(spec.target) ? (_, i) => spec.target[i] : spec.target);
      if (spec.strength != null) force.strength(Array.isArray(spec.strength) ? (_, i) => spec.strength[i] : spec.strength);
      return force;
    }
  }
  throw new Error(spec.type);
}

function nodesOf(graph, fixed) {
  return Array.from({length: graph.n}, (_, i) => {
    const node = {};
    if (graph.x) { node.x = graph.x[i]; node.y = graph.y[i]; }
    if (fixed && fixed.fx[i] != null) node.fx = fixed.fx[i];
    if (fixed && fixed.fy[i] != null) node.fy = fixed.fy[i];
    return node;
  });
}

const state = (simulation, nodes, ticks) => ({
  ticks, alpha: simulation.alpha(),
  x: nodes.map((d) => d.x), y: nodes.map((d) => d.y), vx: nodes.map((d) => d.vx), vy: nodes.map((d) => d.vy),
});

function simulate(graphName, steps, settings = {}, fixed = null, ticks = [1, 10, 300]) {
  const graph = graphs[graphName];
  const nodes = nodesOf(graph, fixed);
  const simulation = forceSimulation(nodes).stop();
  if (settings.alpha != null) simulation.alpha(settings.alpha);
  if (settings.alphaMin != null) simulation.alphaMin(settings.alphaMin);
  if (settings.alphaDecay != null) simulation.alphaDecay(settings.alphaDecay);
  if (settings.alphaTarget != null) simulation.alphaTarget(settings.alphaTarget);
  if (settings.velocityDecay != null) simulation.velocityDecay(settings.velocityDecay);
  for (const step of steps) simulation.force(step.name, step.force ? build(step.force, graph.n, graph.links) : null);
  const states = [];
  let done = 0;
  for (const t of ticks) {
    simulation.tick(t - done);
    done = t;
    states.push(state(simulation, nodes, t));
  }
  return {graph: graphName, n: graph.n, x: graph.x ?? null, y: graph.y ?? null, fx: fixed?.fx ?? null, fy: fixed?.fy ?? null, links: graph.links, settings, steps, states, nodes, simulation};
}

const clean = ({nodes, simulation, ...rest}) => rest;

const named = (type, extra = {}) => [{name: type, force: {type, ...extra}}];
const across = (names, steps) => names.map((g) => clean(simulate(g, steps)));

function initialCases() {
  const cases = [];
  for (const n of [0, 1, 7, 2000]) {
    const nodes = Array.from({length: n}, () => ({}));
    forceSimulation(nodes).stop();
    cases.push({n, x: null, y: null, fx: null, fy: null, nodes: nodes.map(({x, y, vx, vy}) => ({x, y, vx, vy}))});
  }
  const x = [1, NaN, 3, 4.5, NaN, -2], y = [2, 2, NaN, -7.25, NaN, 0];
  const fx = [null, 5, null, null, 9, null], fy = [null, null, null, 6, -1, null];
  for (const fixed of [false, true]) {
    const nodes = x.map((v, i) => ({x: v, y: y[i], ...(fixed && fx[i] != null ? {fx: fx[i]} : {}), ...(fixed && fy[i] != null ? {fy: fy[i]} : {})}));
    const simulation = forceSimulation(nodes).stop();
    cases.push({n: x.length, x, y, fx: fixed ? fx : null, fy: fixed ? fy : null, nodes: nodes.map(({x, y, vx, vy}) => ({x, y, vx, vy})),
      alpha: simulation.alpha(), alphaMin: simulation.alphaMin(), alphaDecay: simulation.alphaDecay(), velocityDecay: simulation.velocityDecay()});
  }
  return cases;
}

function runCases() {
  const cases = [];
  for (const settings of [{}, {alphaDecay: 0.05}, {alphaMin: 0.1}]) {
    const graph = graphs.miserables;
    const nodes = nodesOf(graph);
    const simulation = forceSimulation(nodes).stop();
    if (settings.alphaDecay != null) simulation.alphaDecay(settings.alphaDecay);
    if (settings.alphaMin != null) simulation.alphaMin(settings.alphaMin);
    simulation.force("link", build({type: "link"}, graph.n, graph.links)).force("charge", forceManyBody()).force("center", forceCenter());
    let ticks = 0;
    for (;;) {
      simulation.tick();
      ticks++;
      if (simulation.alpha() < simulation.alphaMin()) break;
    }
    cases.push({settings, ticks, state: state(simulation, nodes, ticks)});
  }
  return cases;
}

function findCases() {
  const {nodes, simulation} = simulate("miserables", [
    {name: "link", force: {type: "link"}}, {name: "charge", force: {type: "manyBody"}}, {name: "center", force: {type: "center", x: 0, y: 0}},
  ], {}, null, [300]);
  const cases = [];
  for (const [x, y] of [[0, 0], [100, -50], [-300, 200], [12.5, 7.25], [1e4, 1e4]]) {
    for (const radius of [null, 0, 5, 40, Infinity]) {
      const found = simulation.find(x, y, radius ?? undefined);
      cases.push({x, y, radius, index: found ? found.index : -1});
    }
  }
  return [{x: nodes.map((d) => d.x), y: nodes.map((d) => d.y), cases}];
}

export default function() {
  const all = ["chain", "star", "coincident", "scattered", "miserables"];
  const m = miserables;
  const s = scattered;
  const gallery = [
    {name: "link", force: {type: "link"}}, {name: "charge", force: {type: "manyBody"}}, {name: "center", force: {type: "center", x: 0, y: 0}},
  ];
  const fixedMiserables = {fx: perNode(m.n, (i) => (i % 9 === 0 ? i * 3 - 100 : null)), fy: perNode(m.n, (i) => (i % 9 === 0 || i % 13 === 1 ? 50 - i : null))};
  return {
    initial: initialCases(),
    link: [
      ...across(all, named("link")),
      ...across(["star", "miserables"], named("link", {distance: 12, strength: 0.3, iterations: 3})),
      clean(simulate("miserables", named("link", {distance: perNode(m.links.length, (i) => 10 + (i % 7) * 5), strength: perNode(m.links.length, (i) => (i % 5) / 4)}))),
    ],
    manyBody: [
      ...across(all, named("manyBody")),
      clean(simulate("scattered", named("manyBody", {strength: perNode(s.n, (i) => (i % 4) * 20 - 30)}))),
      clean(simulate("miserables", named("manyBody", {theta: 0}))),
      clean(simulate("miserables", named("manyBody", {theta: 1.5}))),
      clean(simulate("scattered", named("manyBody", {distanceMin: 5, distanceMax: 50}))),
    ],
    center: [
      ...across(["scattered", "coincident"], named("center", {x: 0, y: 0})),
      clean(simulate("scattered", named("center", {x: 320, y: 180, strength: 0.5}))),
    ],
    collide: [
      ...across(all, named("collide", {radius: 1})),
      ...across(["coincident", "scattered"], named("collide", {radius: 12, strength: 0.7, iterations: 3})),
      clean(simulate("scattered", named("collide", {radius: perNode(s.n, (i) => 3 + (i % 5) * 4)}))),
      clean(simulate("miserables", [...named("manyBody"), {name: "collide", force: {type: "collide", radius: 8}}])),
    ],
    position: [
      clean(simulate("scattered", [{name: "x", force: {type: "x", target: 0}}, {name: "y", force: {type: "y", target: 0}}])),
      clean(simulate("scattered", [
        {name: "x", force: {type: "x", target: perNode(s.n, (i) => (i % 6 === 0 ? NaN : i * 4 - 100)), strength: 0.3}},
        {name: "y", force: {type: "y", target: 25, strength: perNode(s.n, (i) => (i % 3) / 10)}},
      ])),
    ],
    combined: [
      clean(simulate("miserables", gallery)),
      clean(simulate("miserables", [...gallery, {name: "collide", force: {type: "collide", radius: 6}}, {name: "x", force: {type: "x", target: 0, strength: 0.05}}, {name: "y", force: {type: "y", target: 0, strength: 0.05}}])),
      clean(simulate("miserables", gallery, {}, fixedMiserables)),
      clean(simulate("miserables", gallery, {alphaTarget: 0.3})),
      clean(simulate("scattered", gallery, {velocityDecay: 0.2})),
      clean(simulate("scattered", gallery, {alpha: 0.5, alphaDecay: 0.05, alphaMin: 0.1})),
    ],
    named: [
      clean(simulate("scattered", [...gallery, {name: "charge", force: {type: "manyBody", strength: -80}}])),
      clean(simulate("scattered", [...gallery, {name: "link", force: null}])),
      clean(simulate("scattered", [{name: "charge", force: {type: "manyBody"}}, {name: "center", force: {type: "center", x: 10, y: 0}}, {name: "charge", force: null}, {name: "charge", force: {type: "manyBody", strength: -5}}])),
    ],
    run: runCases(),
    find: findCases(),
  };
}
