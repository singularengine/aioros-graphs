import {readFileSync, readdirSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {formatLocale, formatDefaultLocale} from "d3-format";
import {timeFormatLocale, timeFormatDefaultLocale} from "d3-time-format";
import {scaleLinear, scaleUtc} from "d3-scale";
import {version} from "../version.mjs";

export const d3Version = version("d3-format");

const modules = join(dirname(fileURLToPath(import.meta.url)), "..", "node_modules");

function definitions(module) {
  const folder = join(modules, module, "locale");
  return readdirSync(folder).filter((f) => f.endsWith(".json")).sort().map((f) => ({name: f.replace(/\.json$/, ""), definition: JSON.parse(readFileSync(join(folder, f), "utf8"))}));
}

const values = [0, -0, 1, -1, 0.5, -1234.5678, 1234567.891, 12345678, 0.00042, 1e21, NaN, Infinity, -Infinity, 0.995, 42];
const specifiers = ["", ",", ",.2f", "$,.2f", "($,.2f", "$.3s", ".1%", "~%", ".2p", "+,d", "020,.2f", "^20,.1f", ",.3~s", "e", ".3g", "x", "c", "08,d", "$"];
const dates = [0, -1, 951825845678, 1709210096123, Date.UTC(2023, 0, 31, 12), Date.UTC(2024, 11, 31, 23, 59, 59, 999), Date.UTC(1969, 11, 31, 23, 59, 59, 500), Date.UTC(-500, 3, 3, 3, 3, 3, 3), Date.UTC(2021, 0, 3, 15, 4, 5)];
const timeSpecifiers = ["%c", "%x", "%X", "%a %A %b %B %p", "%-d %B %Y %I:%M %p", "%U %W %V %g %G"];
const tickDomains = [[0, 1, 10], [-1e6, 3e6, 8], [0, 1234.5, 10], [0.001, 0.009, 4]];
const tickSpecifiers = [undefined, "$,.2f", "s", ".1%", ",.2r"];
const utcDomains = [[Date.UTC(2024, 0, 1), Date.UTC(2025, 0, 1)], [Date.UTC(2024, 2, 1, 6), Date.UTC(2024, 2, 3, 18)], [Date.UTC(2024, 0, 1), Date.UTC(2024, 0, 1, 0, 0, 2)]];

export default function() {
  const numberLocales = definitions("d3-format");
  const timeLocales = definitions("d3-time-format");
  const numberFormats = [];
  const prefixFormats = [];
  const tickFormats = [];
  for (const {name, definition} of numberLocales) {
    const locale = formatLocale(definition);
    for (const specifier of specifiers) numberFormats.push({locale: name, specifier, outputs: values.map((v) => locale.format(specifier)(v))});
    for (const [specifier, reference] of [[",.2", 1.3e6], ["$.1", 2.5e-5]]) prefixFormats.push({locale: name, specifier, reference, outputs: values.map((v) => locale.formatPrefix(specifier, reference)(v))});
    formatDefaultLocale(definition);
    for (const [start, stop, count] of tickDomains) {
      const scale = scaleLinear().domain([start, stop]);
      for (const specifier of tickSpecifiers) {
        const ticks = scale.ticks(count);
        tickFormats.push({locale: name, start, stop, count, specifier: specifier ?? null, ticks, labels: ticks.map(scale.tickFormat(count, specifier))});
      }
    }
  }
  formatDefaultLocale({thousands: ",", grouping: [3], currency: ["$", ""]});
  const timeFormats = [];
  const multiScale = [];
  for (const {name, definition} of timeLocales) {
    const locale = timeFormatLocale(definition);
    for (const specifier of timeSpecifiers) timeFormats.push({locale: name, specifier, outputs: dates.map((d) => locale.utcFormat(specifier)(new Date(d)))});
    timeFormatDefaultLocale(definition);
    for (const domain of utcDomains) {
      const scale = scaleUtc().domain(domain);
      const ticks = scale.ticks(10);
      multiScale.push({locale: name, ticks: ticks.map(Number), labels: ticks.map(scale.tickFormat()), formatted: ticks.map(scale.tickFormat(10, "%c"))});
    }
  }
  timeFormatDefaultLocale({dateTime: "%x, %X", date: "%-m/%-d/%Y", time: "%-I:%M:%S %p", periods: ["AM", "PM"],
    days: ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"], shortDays: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"],
    months: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
    shortMonths: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]});
  return {values: [{values}], dates: [{dates}], numberLocales, timeLocales, numberFormats, prefixFormats, tickFormats, timeFormats, multiScale};
}
