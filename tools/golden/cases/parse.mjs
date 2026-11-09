import {spawnSync} from "node:child_process";
import {readFileSync, readdirSync} from "node:fs";
import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {utcFormat, utcParse, timeFormatLocale} from "d3-time-format";
import {version} from "../version.mjs";

export const d3Version = version("d3-time-format");

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "package.json"));
const localeDirectory = join(dirname(require.resolve("d3-time-format")), "..", "locale");
const helper = join(dirname(fileURLToPath(import.meta.url)), "local", "parse-zone.mjs");

function ms(d) { return d == null ? null : +d; }

const dates = [0, Date.UTC(2024, 1, 29, 13, 5, 9, 7), Date.UTC(1999, 11, 31, 23, 59, 59, 999), Date.UTC(2023, 0, 1), Date.UTC(1969, 6, 20, 20, 17, 40),
  Date.UTC(2005, 0, 2, 0, 0, 0, 123), Date.UTC(2020, 11, 31, 12), Date.UTC(2021, 0, 3, 7), Date.UTC(2026, 9, 13, 23, 0, 59, 500)];
const ancient = [new Date(Date.UTC(2000, 0, 1)), new Date(Date.UTC(2000, 0, 1))];
ancient[0].setUTCFullYear(1); ancient[1].setUTCFullYear(68, 1, 29);
dates.push(+ancient[0], +ancient[1]);

const specifiers = [
  "%Y-%m-%d", "%Y-%m-%dT%H:%M:%S.%LZ", "%Y-%m-%dT%H:%M:%S%Z", "%m/%d/%Y %I:%M %p", "%a %b %e %Y", "%A, %B %-d, %Y", "%d.%m.%y",
  "%j %Y", "%Y %U %w", "%Y %W %u", "%Y %U %a", "%Y %W", "%Y %U", "%G-W%V-%u", "%G-W%V-%w", "%G-W%V", "%Q", "%s", "%s.%L", "%s %f",
  "%q %Y", "%Y q%q %m", "%H:%M:%S.%f", "%c", "%x", "%X", "%%Y %Y", "%-m/%_d/%0Y", "%Y%m%d", "%y", "%b %Y", "%B", "%I %p", "%e", "%Z",
  "%Y %Z", "", "%K %Y", "%Y %", "%Y %-", "%L", "%H%M", "%A", "%a %d", "%p %I:%M",
];

const odd = ["", " ", "2024", "2024-13-01", "2024-02-30", "2024-00-00", "1/2/3", "12/31/1999 12:00 am", "12/31/1999 12:00 PM", "12/31/1999 00:00 pm",
  "  7", "9999-99-99", "+0530", "Z", "-08:00", "x+05", "+05", "-0000", "abc", "Sun", "sunday", "SEPTEMBER", "Septem", "1e3", "١٢", "2024 00 0",
  "2024 53 6", "2024-W00-1", "2024-W54-1", "2024-W53-7", "2024-W01", "2020-W53-0", "8640000000000001", "8640000000000000", "99999999999999999999",
  "1234567890.123", "1234567890 654321", "4 2024", "2024 q4 12", "0 2024", "5 2024", "% 2024", "%Y 2024", "1/ 2/ 2024", " 12/31/1999",
  "23:59:60.999999", "24:00:00.000000", "366 2024", "000 2024", "999 2023", "pm 12:30", "AM 7:05", "Am 12:00", "THURSDAY", "thu 01",
  "2024 +01:3", "2024 +0130x", "2024 Z", "2024 z", "2024 +2400", "68", "69", "00", "99", "2024-02-29T12:30:00+05:30", "2024-02-29T12:30:00-0800",
  "2024-02-29T12:30:00Z", "2024-02-29T12:30:00.123Z", "1999-12-31T23:59:59.999+1400", "20240229", "202402291", "12", " 12", "1 PM",
];

function mutate(text) {
  return [text, text.toLowerCase(), text.toUpperCase(), text + " ", "x" + text, text.slice(0, -1)];
}

function utcCases() {
  return specifiers.map((specifier) => {
    const format = utcFormat(specifier), parse = utcParse(specifier);
    const inputs = [...new Set(dates.flatMap((d) => mutate(format(new Date(d)))).concat(odd))];
    return {specifier, inputs, outputs: inputs.map((text) => ms(parse(text)))};
  });
}

function localeCases() {
  const cases = [];
  for (const file of readdirSync(localeDirectory).filter((f) => f.endsWith(".json")).sort()) {
    const name = file.replace(/\.json$/, "");
    const locale = timeFormatLocale(JSON.parse(readFileSync(join(localeDirectory, file), "utf8")));
    for (const specifier of ["%c", "%x", "%X", "%A %-d %B %Y", "%a %b %d %I %p", "%B", "%b"]) {
      const format = locale.utcFormat(specifier), parse = locale.utcParse(specifier);
      const inputs = [...new Set(dates.flatMap((d) => mutate(format(new Date(d)))))];
      cases.push({locale: name, specifier, inputs, outputs: inputs.map((text) => ms(parse(text)))});
    }
  }
  return cases;
}

const zones = ["UTC", "Europe/Berlin", "America/Sao_Paulo", "Australia/Lord_Howe", "America/New_York", "Asia/Kolkata"];

function localCases() {
  const cases = [];
  for (const zone of zones) {
    const result = spawnSync(process.execPath, [helper], {env: {...process.env, TZ: zone}, maxBuffer: 1 << 28});
    if (result.status !== 0) throw new Error(result.stderr.toString());
    cases.push(...JSON.parse(result.stdout.toString()).map((c) => ({zone, ...c})));
  }
  return cases;
}

export default function() {
  return {utc: utcCases(), locales: localeCases(), local: localCases()};
}
