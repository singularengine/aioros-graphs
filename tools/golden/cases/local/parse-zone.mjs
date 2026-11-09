import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "package.json"));
const {timeParse, utcFormat} = require("d3-time-format");

function ms(d) { return d == null ? null : +d; }

const hour = 36e5;
const transitions = [];
for (const year of [1996, 2021, 2024]) {
  let previous = new Date(Date.UTC(year, 0, 1)).getTimezoneOffset();
  for (let u = Date.UTC(year, 0, 1); u < Date.UTC(year + 1, 0, 1); u += hour / 4) {
    const offset = new Date(u).getTimezoneOffset();
    if (offset !== previous) {
      let lo = u - hour / 4, hi = u;
      while (hi - lo > 1) { const mid = Math.floor((lo + hi) / 2); if (new Date(mid).getTimezoneOffset() === previous) lo = mid; else hi = mid; }
      transitions.push({at: hi, before: -previous * 6e4, after: -offset * 6e4});
      previous = offset;
    }
  }
}

const walls = [Date.UTC(2024, 1, 29, 13, 5, 9, 7), Date.UTC(1999, 11, 31, 23, 59, 59, 999), Date.UTC(2021, 0, 3, 7), Date.UTC(1970, 0, 1)];
for (const {at, before, after} of transitions.slice(0, 4)) {
  for (const k of [-hour, -1, 0, hour / 4, hour / 2, hour, 2 * hour]) walls.push(at + Math.min(before, after) + k);
}

const full = utcFormat("%Y-%m-%d %H:%M:%S.%L");
const inputs = walls.map((w) => full(new Date(w))).concat(["0050-02-29 12:00:00.000", "0004-02-29 00:00:00.000", "0001-01-01 00:00:00.000",
  "0099-12-31 23:59:59.999", "0000-03-01 01:00:00.000", "1900-01-01 00:00:00.000", "1883-11-18 12:00:00.000"]);

const cases = [];
const parse = timeParse("%Y-%m-%d %H:%M:%S.%L");
cases.push({specifier: "%Y-%m-%d %H:%M:%S.%L", inputs, outputs: inputs.map((t) => ms(parse(t)))});

const others = {
  "%Y-%m-%dT%H:%M:%S%Z": ["2024-03-31T02:30:00+0100", "2024-03-31T02:30:00Z", "2024-10-27T02:30:00-05:00"],
  "%c": ["3/31/2024, 2:30:00 AM", "10/27/2024, 2:30:00 AM", "11/3/2024, 1:30:00 AM", "3/10/2024, 2:30:00 AM", "2/18/2024, 0:00:00 AM", "1/1/50, 12:00:00 PM"],
  "%Y %U %w": ["2024 13 0", "2024 00 0", "2024 44 0", "1996 13 0"],
  "%Y %W %u": ["2024 13 7", "2024 00 1", "2024 43 7"],
  "%G-W%V-%u": ["2024-W13-7", "2024-W01-1", "2024-W44-7", "2021-W01-1", "0050-W09-1"],
  "%y-%m-%d": ["24-03-31", "68-02-29", "69-01-01"],
  "%j %Y": ["091 2024", "301 2024", "001 0050"],
  "%Q": ["1711846800000"],
  "%s.%L": ["1711846800.250"],
  "%Y-%m-%d": ["2024-03-31", "2024-10-27", "2024-11-03", "2024-03-10", "0001-01-01", "0099-02-29", "2018-11-04", "1996-10-27"],
};
for (const [specifier, texts] of Object.entries(others)) {
  const p = timeParse(specifier);
  cases.push({specifier, inputs: texts, outputs: texts.map((t) => ms(p(t)))});
}

const meanTime = Date.UTC(1970, 0, 1);
const kept = process.env.TZ === "UTC" ? cases : cases.map(({specifier, inputs, outputs}) => {
  const keep = outputs.map((o) => o === null || o >= meanTime);
  return {specifier, inputs: inputs.filter((_, i) => keep[i]), outputs: outputs.filter((_, i) => keep[i])};
});

process.stdout.write(JSON.stringify(kept));
