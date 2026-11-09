import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join, resolve} from "node:path";
import {readFileSync, writeFileSync, mkdirSync} from "node:fs";
import {cpus, loadavg} from "node:os";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "package.json"));
const d3 = {...require("d3"), ...require("d3-sankey")};
const d3Version = JSON.parse(readFileSync(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "tools", "golden", "node_modules", "d3", "package.json"), "utf8")).version;

const output = resolve(process.argv[2] ?? "out");
mkdirSync(output, {recursive: true});
const data = JSON.parse(readFileSync(join(output, "data.json"), "utf8"));

const Width = 640, Height = 360, Left = 48, Right = Width - 24, Bottom = Height - 30;

const N = (v) => String(Math.floor(v * 100 + 0.5) / 100);
const escape = (t) => t.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll("\"", "&quot;");

class Scene {
  constructor(width, height) { this.width = width; this.height = height; this.items = []; this.paths = []; this.texts = []; }
  path(cls, d, title = null, translate = null) { this.paths.push(d); this.items.push({kind: "path", cls, n: translate ?? [], text: d, extra: title}); }
  stroke(cls, d, width, title = null) { this.paths.push(d); this.items.push({kind: "path", cls, n: [width], text: d, extra: title}); }
  line(cls, x1, y1, x2, y2) { this.items.push({kind: "line", cls, n: [x1, y1, x2, y2]}); }
  text(cls, x, y, text, anchor = "start") { this.texts.push(text); this.items.push({kind: "text", cls, n: [x, y], text, extra: anchor}); }
  rect(cls, x, y, w, h) { this.items.push({kind: "rect", cls, n: [x, y, w, h]}); }
  cell(cls, x, y, w, h, fill) { this.texts.push(fill); this.items.push({kind: "rect", cls, n: [x, y, w, h], extra: fill}); }
  group(cls, x, y, markup, labels) { this.texts.push(...labels); this.items.push({kind: "g", cls, n: [x, y], text: markup}); }
  circle(cls, x, y, r) { this.items.push({kind: "circle", cls, n: [x, y, r]}); }
  axisBottom(positions, labels, y, x0, x1) {
    this.line("domain", x0, y, x1, y);
    for (let i = 0; i < positions.length; i++) {
      this.line("tick", positions[i], y, positions[i], y + 5);
      this.text("label", positions[i], y + 17, labels[i], "middle");
    }
  }
  gridLeft(positions, labels, x0, x1) {
    for (let i = 0; i < positions.length; i++) {
      this.line("grid", x0, positions[i], x1, positions[i]);
      this.text("label", x0 - 8, positions[i] + 4, labels[i], "end");
    }
  }
  legend(keys, x, y) {
    for (let i = 0; i < keys.length; i++) {
      this.rect("swatch s" + (i + 1), x, y - 9, 10, 10);
      this.text("legend", x + 14, y, keys[i]);
      x += 14 + keys[i].length * 6.6 + 16;
    }
  }
  toSvg(label) {
    let svg = `<svg class="chart" viewBox="0 0 ${N(this.width)} ${N(this.height)}" role="img" aria-label="${escape(label)}">`;
    for (const item of this.items) svg += "\n" + write(item);
    return svg + "\n</svg>";
  }
}

function write({kind, cls, n, text, extra}) {
  let s = `<${kind} class="${cls}"`;
  switch (kind) {
    case "path":
      s += ` d="${text}"`;
      if (n.length === 2) s += ` transform="translate(${N(n[0])},${N(n[1])})"`;
      else if (n.length === 1) s += ` stroke-width="${N(n[0])}"`;
      if (extra != null) return s + `><title>${escape(extra)}</title></path>`;
      break;
    case "line":
      s += ` x1="${N(n[0])}" y1="${N(n[1])}" x2="${N(n[2])}" y2="${N(n[3])}"`;
      break;
    case "text":
      s += ` x="${N(n[0])}" y="${N(n[1])}"`;
      if (extra !== "start") s += ` text-anchor="${extra}"`;
      return s + `>${escape(text)}</text>`;
    case "rect":
      s += ` x="${N(n[0])}" y="${N(n[1])}" width="${N(n[2])}" height="${N(n[3])}"` + (extra == null ? ` rx="2"` : ` fill="${extra}"`);
      break;
    case "g":
      return s + ` transform="translate(${N(n[0])},${N(n[1])})">${text}</g>`;
    default:
      s += ` cx="${N(n[0])}" cy="${N(n[1])}" r="${N(n[2])}"`;
  }
  return s + "/>";
}

function yGrid(scene, y, count, specifier) {
  const ticks = y.ticks(count);
  const format = y.tickFormat(count, specifier ?? undefined);
  scene.gridLeft(ticks.map(y), ticks.map(format), Left, Right);
}

function xTime(scene, x) {
  const ticks = x.ticks(Width / 100);
  const format = x.tickFormat();
  scene.axisBottom(ticks.map(x), ticks.map(format), Bottom, Left, Right);
}

function stack(s, order, offset) {
  const rows = s.rows.map((row) => Object.fromEntries(s.keys.map((k, i) => [k, row[i]])));
  return d3.stack().keys(s.keys).order(order).offset(offset)(rows);
}

const maxUpper = (series) => d3.max(series, (s) => d3.max(s, (d) => d[1]));

function stockLine(data) {
  const scene = new Scene(Width, Height);
  const s = data.stock;
  const x = d3.scaleUtc([s.dates[0], s.dates.at(-1)], [Left, Right]);
  const y = d3.scaleLinear(d3.extent(s.values), [Bottom, 20]).nice();
  yGrid(scene, y, Height / 50, null);
  scene.path("line s1", d3.line().x((d, i) => x(s.dates[i])).y((d) => y(d))(s.values));
  xTime(scene, x);
  scene.text("unit", 8, 12, "USD");
  return scene;
}

function visitorsArea(data) {
  const scene = new Scene(Width, Height);
  const s = data.visitors;
  const x = d3.scaleUtc([s.dates[0], s.dates.at(-1)], [Left, Right]);
  const y = d3.scaleLinear([0, d3.max(s.values)], [Bottom, 20]).nice();
  yGrid(scene, y, Height / 50, ".2~s");
  scene.path("area s1", d3.area().curve(d3.curveMonotoneX).x((d, i) => x(s.dates[i])).y0(y(0)).y1((d) => y(d))(s.values));
  scene.path("line s1", d3.line().curve(d3.curveMonotoneX).x((d, i) => x(s.dates[i])).y((d) => y(d))(s.values));
  xTime(scene, x);
  return scene;
}

function salesStack(data) {
  const scene = new Scene(Width, Height);
  const s = data.sales;
  const series = stack(s, d3.stackOrderNone, d3.stackOffsetNone);
  const x = d3.scaleUtc([s.dates[0], s.dates.at(-1)], [Left, Right]);
  const y = d3.scaleLinear([0, maxUpper(series)], [Bottom, 36]).nice();
  yGrid(scene, y, Height / 50, "$,f");
  const area = d3.area().x((d, i) => x(s.dates[i])).y0((d) => y(d[0])).y1((d) => y(d[1]));
  series.forEach((band, i) => scene.path("band s" + (i + 1), area(band), s.keys[i]));
  xTime(scene, x);
  scene.legend(s.keys, Left, 14);
  scene.text("unit", Right, 14, "thousand USD", "end");
  return scene;
}

function revenueBars(data) {
  const scene = new Scene(Width, Height);
  const s = data.revenue;
  const series = stack(s, d3.stackOrderNone, d3.stackOffsetNone);
  const x = d3.scaleBand(d3.range(s.rows.length), [Left, Right]).padding(0.2);
  const y = d3.scaleLinear([0, maxUpper(series)], [Bottom, 36]).nice();
  yGrid(scene, y, Height / 50, "$.2~s");
  series.forEach((band, i) => {
    const path = d3.pathRound(3);
    band.forEach((d, j) => path.rect(x(j), y(d[1]), x.bandwidth(), y(d[0]) - y(d[1])));
    scene.path("bar s" + (i + 1), path.toString(), s.keys[i]);
  });
  scene.line("domain", Left, Bottom, Right, Bottom);
  s.labels.forEach((label, j) => scene.text("label", x(j) + x.bandwidth() / 2, Bottom + 17, label, "middle"));
  scene.legend(s.keys, Left, 14);
  return scene;
}

function browserDonut(data) {
  const scene = new Scene(Width, Height);
  const s = data.browsers;
  let total = 0;
  for (const value of s.values) total += value;
  const percent = d3.format(".1%");
  const slices = d3.pie().padAngle(0.02)(s.values);
  const cx = 200, cy = 180;
  const arc = d3.arc().innerRadius(90).outerRadius(160).cornerRadius(4);
  const label = d3.arc().innerRadius(125).outerRadius(125);
  for (const slice of slices) scene.path("slice s" + (slice.index + 1), arc(slice), s.names[slice.index], [cx, cy]);
  for (const slice of slices) {
    if (slice.endAngle - slice.startAngle > 0.3) {
      const [lx, ly] = label.centroid(slice);
      scene.text("slice-name", cx + lx, cy + ly - 3, s.names[slice.index], "middle");
      scene.text("slice-value", cx + lx, cy + ly + 11, percent(s.values[slice.index] / total), "middle");
    }
  }
  for (let i = 0; i < s.names.length; i++) {
    const row = 96 + i * 26;
    scene.rect("swatch s" + (i + 1), 420, row - 10, 12, 12);
    scene.text("legend", 440, row, s.names[i]);
    scene.text("legend-value", 600, row, percent(s.values[i] / total), "end");
  }
  return scene;
}

function streamgraph(data) {
  const scene = new Scene(Width, Height);
  const s = data.stream;
  const series = stack(s, d3.stackOrderInsideOut, d3.stackOffsetWiggle);
  const n = s.rows.length;
  const x = d3.scaleLinear([0, n - 1], [16, Width - 16]);
  let min = Infinity, max = -Infinity;
  for (const band of series) for (const d of band) { min = Math.min(min, d[0]); max = Math.max(max, d[1]); }
  const y = d3.scaleLinear([min, max], [Bottom - 8, 12]);
  const area = d3.area().curve(d3.curveBasis).x((d, j) => x(s.dates[j])).y0((d) => y(d[0])).y1((d) => y(d[1]));
  series.forEach((band, i) => scene.path("stream s" + (i + 1), area(band), s.keys[i]));
  const ticks = x.ticks(Width / 80);
  const format = x.tickFormat(Width / 80);
  scene.axisBottom(ticks.map(x), ticks.map(format), Bottom, 16, Width - 16);
  return scene;
}

function curveGallery(data) {
  const scene = new Scene(Width, Height);
  const p = data.curves;
  const curves = [
    ["curveLinear", d3.curveLinear], ["curveMonotoneX", d3.curveMonotoneX], ["curveNatural", d3.curveNatural],
    ["curveStep", d3.curveStep], ["curveCardinal", d3.curveCardinal], ["curveCatmullRom", d3.curveCatmullRom],
  ];
  for (let k = 0; k < curves.length; k++) {
    const px = k % 3 * (Width / 3);
    const py = Math.floor(k / 3) * (Height / 2);
    const x = d3.scaleLinear([0, 8], [px + 16, px + Width / 3 - 16]);
    const y = d3.scaleLinear([0, 10], [py + Height / 2 - 12, py + 30]);
    const xs = p.x.map(x), ys = p.y.map(y);
    scene.line("grid", px + 16, py + Height / 2 - 12, px + Width / 3 - 16, py + Height / 2 - 12);
    scene.text("panel", px + 16, py + 18, curves[k][0]);
    scene.path("line s1", d3.line().curve(curves[k][1]).x((d, i) => xs[i]).y((d, i) => ys[i])(xs));
    for (let i = 0; i < xs.length; i++) scene.circle("dot", xs[i], ys[i], 3);
  }
  return scene;
}

class Element {
  constructor(name) { this.ownerDocument = fakeDocument; this.namespaceURI = "http://www.w3.org/2000/svg"; this.tagName = name; this.attributes = []; this.childNodes = []; this.parentNode = null; this.text = null; }
  setAttribute(name, value) { const a = this.attributes.find((x) => x.name === name); if (a) a.value = String(value); else this.attributes.push({name, value: String(value)}); }
  getAttribute(name) { const a = this.attributes.find((x) => x.name === name); return a ? a.value : null; }
  set textContent(value) { this.childNodes = []; this.text = String(value); }
  get textContent() { return this.text ?? this.childNodes.map((c) => c.textContent).join(""); }
  insertBefore(child, next) { const i = next == null ? -1 : this.childNodes.indexOf(next); if (i < 0) this.childNodes.push(child); else this.childNodes.splice(i, 0, child); child.parentNode = this; return child; }
  appendChild(child) { return this.insertBefore(child, null); }
  removeChild(child) { this.childNodes = this.childNodes.filter((c) => c !== child); child.parentNode = null; return child; }
  matches(selector) { return selector.startsWith(".") ? (this.getAttribute("class") ?? "").split(" ").includes(selector.slice(1)) : this.tagName === selector; }
  querySelectorAll(selector) { const found = []; const visit = (node) => { for (const c of node.childNodes) { if (c.matches(selector)) found.push(c); visit(c); } }; visit(this); return found; }
  querySelector(selector) { return this.querySelectorAll(selector)[0] ?? null; }
  get outerHTML() {
    const escape = (t, attribute) => { const e = t.replaceAll("&", "&amp;").replaceAll("\u00a0", "&nbsp;"); return attribute ? e.replaceAll("\"", "&quot;") : e.replaceAll("<", "&lt;").replaceAll(">", "&gt;"); };
    const attributes = this.attributes.map((a) => ` ${a.name}="${escape(a.value, true)}"`).join("");
    const inner = this.text != null ? escape(this.text, false) : this.childNodes.map((c) => c.outerHTML).join("");
    return `<${this.tagName}${attributes}>${inner}</${this.tagName}>`;
  }
}
const fakeDocument = {createElementNS: (uri, name) => new Element(name)};
fakeDocument.documentElement = new Element("svg");

function axisMarkup(axis) {
  const g = new Element("g");
  d3.select(g).call(axis);
  return {markup: g.outerHTML, labels: g.querySelectorAll(".tick").map((t) => t.querySelector("text").textContent)};
}

function histogram(data) {
  const scene = new Scene(Width, Height);
  const bins = d3.bin()(data.heights);
  const x = d3.scaleLinear([bins[0].x0, bins.at(-1).x1], [Left, Right]);
  let max = 0;
  for (const bin of bins) max = Math.max(max, bin.length);
  const y = d3.scaleLinear([0, max], [Bottom, 20]).nice();
  yGrid(scene, y, Height / 50, null);
  const path = d3.pathRound(3);
  for (const b of bins) path.rect(x(b.x0) + 1, y(b.length), Math.max(0, x(b.x1) - x(b.x0) - 1), y(0) - y(b.length));
  scene.path("bar s1", path.toString());
  const ticks = x.ticks(Width / 80);
  const format = x.tickFormat(Width / 80);
  scene.axisBottom(ticks.map(x), ticks.map(format), Bottom, Left, Right);
  scene.text("unit", Right, Bottom - 6, "height (cm)", "end");
  return scene;
}

function scatter(data) {
  const scene = new Scene(Width, Height);
  const f = data.flowers;
  const x = d3.scaleLinear(d3.extent(f.x), [Left, Right]).nice();
  const y = d3.scaleLinear(d3.extent(f.y), [Bottom, 36]).nice();
  const r = d3.scaleSqrt([0, d3.max(f.size)], [0, 7]);
  const types = [d3.symbolCircle, d3.symbolStar, d3.symbolTriangle];
  yGrid(scene, y, Height / 50, ".1f");
  for (let i = 0; i < f.x.length; i++) {
    const radius = r(f.size[i]);
    scene.path("symbol s" + (f.group[i] + 1), d3.symbol(types[f.group[i]], radius * radius * 4)(), null, [x(f.x[i]), y(f.y[i])]);
  }
  const ticks = x.ticks(Width / 80);
  const format = x.tickFormat(Width / 80, ".1f");
  scene.axisBottom(ticks.map(x), ticks.map(format), Bottom, Left, Right);
  scene.legend(f.keys, Left, 14);
  return scene;
}

function radial(data) {
  const cx = 320, cy = 184;
  const scene = new Scene(Width, Height);
  const t = data.temperatures;
  const x = d3.scaleUtc([t.dates[0], t.dates.at(-1)], [0, 2 * Math.PI]);
  const y = d3.scaleRadial([d3.min(t.low), d3.max(t.high)], [40, 150]);
  const ticks = y.ticks(5);
  const format = y.tickFormat(5, "+");
  for (const tick of ticks) scene.circle("ring", cx, cy, y(tick));
  const area = d3.areaRadial().curve(d3.curveLinearClosed).angle((d, i) => x(t.dates[i])).innerRadius((d, i) => y(t.low[i])).outerRadius((d, i) => y(t.high[i]));
  scene.path("radial s1", area(t.dates), null, [cx, cy]);
  for (const tick of ticks) scene.text("label", cx, cy - y(tick) - 2, format(tick) + "°", "middle");
  const month = d3.utcFormat("%b");
  for (const m of x.ticks(d3.utcMonth)) {
    const a = x(m);
    scene.text("label", cx + 166 * Math.sin(a), cy - 166 * Math.cos(a) + 4, month(m), "middle");
  }
  return scene;
}

function heatmap(data) {
  const scene = new Scene(Width, Height);
  const g = data.anomalies;
  const x = d3.scaleBand(d3.range(g.columns.length), [Left, Right]);
  const y = d3.scaleBand(d3.range(g.months.length), [16, Bottom]);
  let max = 0;
  for (const row of g.rows) for (const value of row) max = Math.max(max, Math.abs(value));
  const color = d3.scaleDiverging([max, 0, -max], d3.interpolateRdBu);
  for (let i = 0; i < g.months.length; i++) {
    for (let j = 0; j < g.columns.length; j++) scene.cell("cell", x(j), y(i), x.bandwidth(), y.bandwidth(), color(g.rows[i][j]));
    scene.text("label", Left - 6, y(i) + y.bandwidth() / 2 + 4, g.months[i], "end");
  }
  for (let j = 0; j < g.columns.length; j += 5) scene.text("label", x(j) + x.bandwidth() / 2, Bottom + 17, String(g.columns[j]), "middle");
  return scene;
}

function logLine(data) {
  const scene = new Scene(Width, Height);
  const s = data.transistors;
  const x = d3.scaleLinear([s.dates[0], s.dates.at(-1)], [Left + 12, Right]);
  const y = d3.scaleLog(d3.extent(s.values), [Bottom, 20]).nice();
  scene.path("line s1", d3.line().curve(d3.curveMonotoneX).x((d, i) => x(s.dates[i])).y((d) => y(d))(s.values));
  const left = axisMarkup(d3.axisLeft(y).ticks(10, "~s"));
  const bottom = axisMarkup(d3.axisBottom(x).ticks(Width / 80, "d"));
  scene.group("axis", Left, 0, left.markup, left.labels);
  scene.group("axis", 0, Bottom, bottom.markup, bottom.labels);
  return scene;
}

function category(data, node) {
  while (node.depth > 1) node = node.parent;
  return data.children.indexOf(node.data);
}

function circle(x, y, r) {
  const p = d3.pathRound(3);
  p.arc(x, y, r, 0, 2 * Math.PI);
  return p + "";
}

function treemap(data) {
  const scene = new Scene(Width, Height);
  const summed = d3.hierarchy(data.software).sum((d) => d.value);
  const root = d3.treemap().tile(d3.treemapSquarify).size([Width, Height]).padding(1).round(true)(summed.sort((a, b) => b.height - a.height || b.value - a.value));
  for (const leaf of root.leaves()) {
    const p = d3.pathRound(3);
    p.rect(leaf.x0, leaf.y0, leaf.x1 - leaf.x0, leaf.y1 - leaf.y0);
    scene.path("tile s" + (category(data.software, leaf) + 1), p + "", leaf.data.name);
    if (leaf.x1 - leaf.x0 > 64 && leaf.y1 - leaf.y0 > 16) scene.text("tile-name", leaf.x0 + 4, leaf.y0 + 13, leaf.data.name);
  }
  return scene;
}

function pack(data) {
  const scene = new Scene(Width, Height);
  const root = d3.pack().size([Width - 2, Height - 2]).padding(3)(d3.hierarchy(data.software).sum((d) => d.value).sort((a, b) => b.value - a.value));
  for (const node of root.descendants()) {
    const cls = node.children ? "group-circle depth" + node.depth : "bubble s" + (category(data.software, node) + 1);
    scene.path(cls, circle(node.x, node.y, node.r), node.data.name, [1, 1]);
  }
  for (const node of root.leaves()) {
    if (node.r > 20) scene.text("bubble-name", node.x + 1, node.y + 4, node.data.name.slice(0, Math.min(node.data.name.length, Math.trunc(node.r / 3.5))), "middle");
  }
  return scene;
}

function sunburst(data) {
  const radius = Height / 2 - 4;
  const scene = new Scene(Width, Height);
  const root = d3.partition().size([2 * Math.PI, radius])(d3.hierarchy(data.software).sum((d) => d.value).sort((a, b) => b.value - a.value));
  const arc = d3.arc().startAngle((d) => d.x0).endAngle((d) => d.x1).padAngle((d) => Math.min((d.x1 - d.x0) / 2, 0.005))
    .padRadius(radius / 2).innerRadius((d) => d.y0).outerRadius((d) => d.y1 - 1);
  for (const node of root.descendants()) {
    if (!node.depth) continue;
    scene.path("sector s" + (category(data.software, node) + 1) + " depth" + node.depth, arc(node), node.data.name, [Width / 2, Height / 2]);
  }
  for (const node of root.children) {
    const [cx, cy] = arc.centroid(node);
    scene.text("sector-name", Width / 2 + cx, Height / 2 + cy + 4, node.data.name, "middle");
  }
  return scene;
}

function tidyTree(data) {
  const scene = new Scene(Width, Height);
  const root = d3.tree().size([Height - 40, Width - 200])(d3.hierarchy(data.packages));
  const link = d3.linkHorizontal().x((d) => d.y).y((d) => d.x);
  for (const l of root.links()) scene.path("link", link(l), null, [90, 20]);
  for (const node of root.descendants()) {
    const inner = !!node.children;
    scene.circle(inner ? "node inner" : "node", 90 + node.y, 20 + node.x, 2.5);
    scene.text("label", 90 + node.y + (inner ? -6 : 6), 20 + node.x + 3, node.data.name, inner ? "end" : "start");
  }
  return scene;
}

function chordDiagram(data) {
  const outerRadius = Height / 2 - 30, innerRadius = outerRadius - 10;
  const scene = new Scene(Width, Height);
  const {names, values} = data.trade;
  const n = names.length;
  const matrix = Array.from({length: n}, (_, i) => values.slice(i * n, i * n + n));
  const chords = d3.chord().padAngle(10 / innerRadius).sortSubgroups(d3.descending).sortChords(d3.descending)(matrix);
  const ribbon = d3.ribbon().radius(innerRadius - 1).padAngle(1 / innerRadius);
  for (const c of chords) {
    const p = d3.pathRound(3);
    ribbon.context(p)(c);
    scene.path("ribbon s" + (c.source.index + 1), p + "", names[c.source.index] + " → " + names[c.target.index], [Width / 2, Height / 2]);
  }
  const arc = d3.arc().innerRadius(innerRadius).outerRadius(outerRadius);
  for (const g of chords.groups) {
    scene.path("chord-group s" + (g.index + 1), arc(g), names[g.index], [Width / 2, Height / 2]);
    const angle = (g.startAngle + g.endAngle) / 2;
    scene.text("label", Width / 2 + (outerRadius + 8) * Math.sin(angle), Height / 2 - (outerRadius + 8) * Math.cos(angle) + 4, names[g.index], angle > Math.PI ? "end" : "start");
  }
  return scene;
}

function sankeyDiagram(data) {
  const scene = new Scene(Width, Height);
  const e = data.energy;
  const {nodes, links} = d3.sankey().nodeWidth(15).nodePadding(10).nodeAlign(d3.sankeyJustify).extent([[1, 5], [Width - 1, Height - 5]])({
    nodes: e.names.map((name) => ({name})),
    links: e.sources.map((s, i) => ({source: s, target: e.targets[i], value: e.values[i]})),
  });
  for (const node of nodes) {
    const p = d3.pathRound(3);
    p.rect(node.x0, node.y0, node.x1 - node.x0, node.y1 - node.y0);
    scene.path("flow-node s" + (node.index % 8 + 1), p + "", node.name);
  }
  const path = d3.sankeyLinkHorizontal();
  for (const link of links) scene.stroke("flow s" + (link.source.index % 8 + 1), path(link), Math.max(1, link.width), link.source.name + " → " + link.target.name);
  for (const node of nodes) {
    const left = node.x0 < Width / 2;
    scene.text("label", left ? node.x1 + 6 : node.x0 - 6, (node.y1 + node.y0) / 2 + 4, node.name, left ? "start" : "end");
  }
  return scene;
}

function forceGraph(data) {
  const scene = new Scene(Width, Height);
  const net = data.network;
  const n = net.group.length;
  const nodes = Array.from({length: n}, () => ({}));
  const links = net.sources.map((s, i) => ({source: s, target: net.targets[i]}));
  const simulation = d3.forceSimulation(nodes).force("link", d3.forceLink(links)).force("charge", d3.forceManyBody().strength(-60))
    .force("x", d3.forceX()).force("y", d3.forceY()).stop();
  simulation.tick(300);
  const p = d3.pathRound(3);
  for (const l of links) {
    p.moveTo(l.source.x, l.source.y);
    p.lineTo(l.target.x, l.target.y);
  }
  scene.path("edge", p + "", null, [Width / 2, Height / 2]);
  for (let i = 0; i < n; i++) scene.path("vertex s" + (net.group[i] + 1), circle(nodes[i].x, nodes[i].y, i < 6 ? 7 : 4.5), null, [Width / 2, Height / 2]);
  return scene;
}

const charts = [
  ["line", "Daily close, two years", stockLine],
  ["area", "Weekly visitors, 2024", visitorsArea],
  ["stacked-area", "Monthly sales by category", salesStack],
  ["stacked-bars", "Quarterly revenue by region", revenueBars],
  ["donut", "Browser share", browserDonut],
  ["streamgraph", "Streamgraph of eight series", streamgraph],
  ["curves", "One series, six curves", curveGallery],
  ["histogram", "Heights of 1,000 adults", histogram],
  ["scatter", "Sepal size by species", scatter],
  ["radial", "Daily temperature range, 2023", radial],
  ["heatmap", "Monthly temperature anomalies", heatmap],
  ["log-line", "Transistors per chip", logLine],
  ["treemap", "Treemap of a software library", treemap],
  ["pack", "Circle packing", pack],
  ["sunburst", "Sunburst", sunburst],
  ["tidy-tree", "Tidy tree of the packages", tidyTree],
  ["chord", "Trade between regions", chordDiagram],
  ["sankey", "Energy flows", sankeyDiagram],
  ["force", "Force-directed graph", forceGraph],
];

const now = () => process.hrtime.bigint();
const ms = (from) => Number(now() - from) / 1e6;

function measure(run) {
  const warm = now();
  while (ms(warm) < 300) run();
  let iterations = 1;
  for (;;) {
    const start = now();
    for (let i = 0; i < iterations; i++) run();
    if (ms(start) >= 10) break;
    iterations *= 2;
  }
  const samples = [];
  for (let k = 0; k < 11; k++) {
    const start = now();
    for (let i = 0; i < iterations; i++) run();
    samples.push(Number(now() - start) / iterations);
  }
  samples.sort((a, b) => a - b);
  return {medianNs: samples[5], iterations};
}

const load = [loadavg()[0]];
const results = [];
for (const [id, title, draw] of charts) {
  const scene = draw(data);
  const {medianNs, iterations} = measure(() => draw(data));
  results.push({id, medianNs, iterations, samples: 11, paths: scene.paths, texts: scene.texts, svg: scene.toSvg(title)});
  console.log(`${id.padEnd(14)} ${(medianNs / 1000).toFixed(1).padStart(10)} µs  (${iterations} iterations per sample)`);
}
load.push(loadavg()[0]);

writeFileSync(join(output, "d3.json"), JSON.stringify({
  runtime: `d3 ${d3Version} on Node ${process.version}`,
  machine: `${cpus()[0].model}, ${cpus().length} threads`,
  date: new Date().toISOString().replace(/\.\d+Z$/, "Z"),
  loadAverage: load,
  charts: results,
}));
console.log(`wrote ${join(output, "d3.json")}`);
