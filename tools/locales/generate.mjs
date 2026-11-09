import {readFileSync, readdirSync, writeFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const modules = join(here, "..", "golden", "node_modules");
const target = join(here, "..", "..", "src", "Aioros.Graphs", "Formatting");

function definitions(module) {
  const folder = join(modules, module, "locale");
  return readdirSync(folder).filter((f) => f.endsWith(".json")).sort().map((f) => [f.replace(/\.json$/, ""), JSON.parse(readFileSync(join(folder, f), "utf8"))]);
}

function text(value) {
  let s = "\"";
  for (const c of value) {
    const code = c.codePointAt(0);
    if (c === "\"" || c === "\\") s += "\\" + c;
    else if (code < 0x20 || code === 0x7f || /[\p{Z}\p{Cf}]/u.test(c) && c !== " ") s += "\\u" + code.toString(16).padStart(4, "0");
    else s += c;
  }
  return s + "\"";
}

function list(values) {
  return "[" + values.map(text).join(", ") + "]";
}

const numbers = definitions("d3-format").map(([name, d]) => {
  const args = [text(d.decimal ?? "."), d.thousands === undefined ? "null" : text(d.thousands), "[" + (d.grouping ?? []).join(", ") + "]",
    text(d.currency?.[0] ?? ""), text(d.currency?.[1] ?? ""), d.numerals ? list(d.numerals) : "null", text(d.percent ?? "%"), text(d.minus ?? "−"), text(d.nan ?? "NaN")];
  return `        ${text(name)} => new(${args.join(", ")}),`;
});

writeFileSync(join(target, "NumberLocales.cs"), `namespace Aioros.Graphs.Formatting;

internal static class NumberLocales
{
    public static IReadOnlyList<string> Names { get; } = ${list(definitions("d3-format").map(([name]) => name))};

    public static NumberLocale? Create(string name) => name switch
    {
${numbers.join("\n")}
        _ => null,
    };
}
`);

const times = definitions("d3-time-format").map(([name, d]) =>
  `        ${text(name)} => new(${[text(d.dateTime), text(d.date), text(d.time), list(d.periods), list(d.days), list(d.shortDays), list(d.months), list(d.shortMonths)].join(", ")}),`);

writeFileSync(join(target, "TimeLocales.cs"), `namespace Aioros.Graphs.Formatting;

internal static class TimeLocales
{
    public static IReadOnlyList<string> Names { get; } = ${list(definitions("d3-time-format").map(([name]) => name))};

    public static TimeLocale? Create(string name) => name switch
    {
${times.join("\n")}
        _ => null,
    };
}
`);
console.log(`${numbers.length} number locales, ${times.length} time locales`);
