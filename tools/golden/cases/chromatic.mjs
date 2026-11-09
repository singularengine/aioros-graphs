import * as c from "d3-scale-chromatic";
import {version} from "../version.mjs";

export const d3Version = version("d3-scale-chromatic");

const categorical = ["Category10", "Accent", "Dark2", "Observable10", "Paired", "Pastel1", "Pastel2", "Set1", "Set2", "Set3", "Tableau10"];
const brewer = ["BrBG", "PRGn", "PiYG", "PuOr", "RdBu", "RdGy", "RdYlBu", "RdYlGn", "Spectral", "Blues", "Greens", "Greys", "Oranges",
  "Purples", "Reds", "BuGn", "BuPu", "GnBu", "OrRd", "PuBuGn", "PuBu", "PuRd", "RdPu", "YlGnBu", "YlGn", "YlOrBr", "YlOrRd"];
const others = ["Viridis", "Magma", "Inferno", "Plasma", "Cividis", "Turbo", "Warm", "Cool", "Rainbow", "Sinebow", "CubehelixDefault"];

const ts = [];
for (let k = 0; k <= 10; k++) ts.push(k / 10);
ts.push(0.05, 0.123, 0.5 + 1e-9, 1 / 3, 2 / 3, 0.999, 1 / 256, 255 / 256, -0.2, 1.3, 2.75, -1.25);

export default function() {
  const schemes = categorical.map((name) => ({name, colors: c["scheme" + name]}));
  const brewerSchemes = brewer.map((name) => ({name, sizes: Array.from({length: 13}, (_, k) => c["scheme" + name][k] ?? [])}));
  const interpolators = brewer.concat(others).map((name) => ({name, ts, outputs: ts.map((t) => c["interpolate" + name](t))}));
  return {categorical: schemes, brewer: brewerSchemes, interpolators};
}
