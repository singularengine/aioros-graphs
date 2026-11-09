import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {writeFileSync} from "node:fs";

const here = dirname(fileURLToPath(import.meta.url));
const require = createRequire(join(here, "..", "golden", "package.json"));
const c = require("d3-scale-chromatic");
const target = join(here, "..", "..", "src", "Aioros.Graphs", "Colors", "SchemeData.cs");

const categorical = ["Category10", "Accent", "Dark2", "Observable10", "Paired", "Pastel1", "Pastel2", "Set1", "Set2", "Set3", "Tableau10"];
const brewer = ["BrBG", "PRGn", "PiYG", "PuOr", "RdBu", "RdGy", "RdYlBu", "RdYlGn", "Spectral", "Blues", "Greens", "Greys", "Oranges",
  "Purples", "Reds", "BuGn", "BuPu", "GnBu", "OrRd", "PuBuGn", "PuBu", "PuRd", "RdPu", "YlGnBu", "YlGn", "YlOrBr", "YlOrRd"];
const ramps = ["Viridis", "Magma", "Inferno", "Plasma"];

const hex = (colors) => colors.map((color) => color.slice(1)).join("");
const lines = [];
lines.push("namespace Aioros.Graphs.Colors;", "", "internal static class SchemeData", "{");
for (const name of categorical) lines.push(`    public const string ${name} = "${hex(c["scheme" + name])}";`, "");
for (const name of ramps) {
  const table = Array.from({length: 256}, (_, i) => c["interpolate" + name]((i + 0.5) / 256));
  lines.push(`    public const string ${name} = "${hex(table)}";`, "");
}
lines.push("    public static string[] Brewer(int scheme) => scheme switch", "    {");
brewer.forEach((name, index) => {
  const sizes = c["scheme" + name];
  const first = sizes.findIndex((s) => s != null);
  if (first !== 3) throw new Error(`${name} starts at ${first}`);
  const sets = sizes.slice(3).map((s) => `"${hex(s)}"`);
  lines.push(`        ${index === brewer.length - 1 ? "_" : index} => [${sets.join(", ")}],`);
});
lines.push("    };", "}", "");
writeFileSync(target, lines.join("\n"));
console.log(`wrote ${target}`);
