import {readdir, writeFile, mkdir} from "node:fs/promises";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const out = join(here, "..", "..", "tests", "Aioros.Graphs.Tests", "Golden");

function replacer(key, value) {
  if (typeof value === "number" && !Number.isFinite(value)) return String(value);
  if (Object.is(value, -0)) return "-0";
  return value;
}

await mkdir(out, {recursive: true});
for (const file of (await readdir(join(here, "cases"))).filter((f) => f.endsWith(".mjs")).sort()) {
  const module = await import(join(here, "cases", file));
  const name = file.replace(/\.mjs$/, "");
  const cases = module.default();
  const groups = Object.entries(cases).map(([group, list]) =>
    `  ${JSON.stringify(group)}: [\n${list.map((c) => "   " + JSON.stringify(c, replacer)).join(",\n")}\n  ]`);
  const text = `{\n "generator": ${JSON.stringify(`tools/golden/cases/${file}`)},\n "d3": ${JSON.stringify(module.d3Version ?? null)},\n "cases": {\n${groups.join(",\n")}\n }\n}\n`;
  JSON.parse(text);
  await writeFile(join(out, `${name}.json`), text);
  console.log(`${name}.json: ${Object.entries(cases).map(([k, v]) => `${k} ${v.length}`).join(", ")}`);
}
