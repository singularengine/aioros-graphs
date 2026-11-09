import {createRequire} from "node:module";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {readFileSync} from "node:fs";

const require = createRequire(join(dirname(fileURLToPath(import.meta.url)), "..", "..", "package.json"));
const t = require("d3-time");
const {timeFormat, timeFormatLocale} = require("d3-time-format");
const {scaleTime} = require("d3-scale");

function ms(d) { return d == null ? null : +d; }

const hour = 36e5, day = 864e5;
const transitions = [];
for (const year of [1996, 2008, 2017, 2018, 2021, 2022, 2024]) {
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
const shown = transitions.slice(0, 4);

const dates = [0, -1, 1, 1234567890123, Date.UTC(2023, 0, 31, 12), Date.UTC(2024, 1, 29, 6, 30), Date.UTC(2024, 11, 31, 23, 59, 59, 999),
  Date.UTC(1999, 11, 31, 22), Date.UTC(2030, 5, 15, 3, 3, 3, 3), Date.UTC(1970, 0, 4, 12), NaN];
for (const {at} of shown) for (const k of [-hour, -1, 0, hour / 2, day]) dates.push(at + k);

const intervals = {
  second: t.timeSecond, minute: t.timeMinute, hour: t.timeHour, day: t.timeDay, week: t.timeSunday, monday: t.timeMonday,
  tuesday: t.timeTuesday, wednesday: t.timeWednesday, thursday: t.timeThursday, friday: t.timeFriday, saturday: t.timeSaturday,
  month: t.timeMonth, year: t.timeYear,
};

const fields = dates.map((d) => {
  const x = new Date(d);
  return {date: d, offset: x.getTimezoneOffset(), year: x.getFullYear(), month: x.getMonth(), day: x.getDate(), weekday: x.getDay(),
    hours: x.getHours(), minutes: x.getMinutes(), seconds: x.getSeconds(), milliseconds: x.getMilliseconds()};
});

const compose = [];
for (const {at, before, after} of shown) {
  const walls = [at + before - hour, at + before, at + Math.min(before, after) + Math.abs(after - before) / 2, at + after, at + Math.max(before, after), at + after + hour];
  for (const wall of walls) {
    const w = new Date(wall);
    const args = [w.getUTCFullYear(), w.getUTCMonth(), w.getUTCDate(), w.getUTCHours(), w.getUTCMinutes(), w.getUTCSeconds(), w.getUTCMilliseconds()];
    compose.push({fields: args, date: +new Date(...args)});
  }
}
for (const args of [[2024, 13, -40, 30, -5, 70, 1500], [1970, 0, 1], [2000, 1, 29, 12], [50, 0, 1], [275760, 8, 13], [NaN, 0, 1], [1970, 0, 1, 1e12]]) {
  compose.push({fields: args, date: ms(new Date(...args))});
}

const interval = [];
for (const [name, i] of Object.entries(intervals)) {
  for (const step of [1, 3]) {
    const every = step === 1 ? i : i.every(step);
    if (!every) continue;
    for (const date of dates) {
      interval.push({name, step, date, floor: ms(every.floor(date)), ceil: ms(every.ceil(date)), round: ms(every.round(date)),
        offset: [-3, -1, 0, 1, 2.7, 13].map((k) => ms(every.offset(date, k)))});
    }
  }
}

const counts = [];
for (const [name, i] of Object.entries(intervals)) {
  for (let k = 0; k + 1 < dates.length; k++) counts.push({name, start: dates[k], end: dates[k + 1], count: i.count(dates[k], dates[k + 1])});
}

const spans = shown.map(({at}) => [at - 3 * day, at + 3 * day]).concat([[Date.UTC(2023, 0, 31), Date.UTC(2023, 7, 31)], [Date.UTC(1999, 5, 15, 13), Date.UTC(2003, 2, 1)]]);
const ranges = [];
for (const [name, i] of Object.entries(intervals)) {
  for (const [start, stop] of spans) {
    for (const step of [1, 2]) {
      if (i.count(start, stop) / step > 80) continue;
      ranges.push({name, start, stop, step, values: i.range(start, stop, step).map(ms)});
    }
  }
}

const tickSpans = spans.concat(shown.map(({at}) => [at - 3 * hour, at + 2 * hour]), [[0, 86400000 * 365 * 30], [Date.UTC(2024, 9, 1), Date.UTC(2024, 10, 30)]]);
const ticks = [];
for (const [start, stop] of tickSpans) {
  for (const count of [1, 5, 10, 13, 50]) {
    ticks.push({start, stop, count, ticks: t.timeTicks(start, stop, count).map(ms), reversed: t.timeTicks(stop, start, count).map(ms), hasInterval: t.timeTickInterval(start, stop, count) != null});
  }
}

const scale = tickSpans.map(([start, stop]) => {
  const s = scaleTime().domain([start, stop]).range([0, 960]);
  const counts = [3, 10].map((count) => {
    const values = s.ticks(count);
    return {count, ticks: values.map(ms), labels: values.map(s.tickFormat(count))};
  });
  const explicit = Object.entries({day: [t.timeDay, day], hour: [t.timeHour, hour], month: [t.timeMonth, 30 * day], minute15: [t.timeMinute.every(15), hour / 4], week: [t.timeWeek, 7 * day]})
    .filter(([, [, duration]]) => Math.abs(stop - start) / duration <= 1000).map(([name, [i]]) => [name, i]).map(([interval, i]) => ({interval, ticks: s.ticks(i).map(ms)}));
  const nice = [10, 5].map((count) => ({count, domain: s.copy().nice(count).domain().map(ms)}));
  const niceInterval = s.copy().nice(t.timeDay).domain().map(ms);
  const outputs = [start, stop, (start + stop) / 2, start - day].map((x) => ({x, y: s(x), inverted: ms(s.invert(s(x)))}));
  const formatted = counts[1].ticks.map((v) => s.tickFormat(10, "%Y-%m-%d %H:%M %Z")(new Date(v)));
  return {domain: [start, stop], range: [0, 960], counts, explicit, nice, niceInterval, outputs, formatted};
});

const specifiers = ["%a %A %b %B %d %e %H %I %j %L %m %M %p %q %S %u %w %y %Y", "%Z", "%Q %s", "%-I:%M %p", "%Y-%m-%dT%H:%M:%S.%L%Z", "%f",
  "%U %W %V %g %G", "%c", "%x %X"];
const formats = specifiers.map((specifier) => ({specifier, outputs: dates.map((d) => timeFormat(specifier)(new Date(d)))}));
const multi = scaleTime().tickFormat();
const multiScale = dates.concat(ticks.flatMap((x) => x.ticks).slice(0, 120)).map((date) => ({date, text: multi(new Date(date))}));

const localeFormats = [];
for (const name of ["es-MX", "de-DE", "ja-JP"]) {
  const locale = timeFormatLocale(JSON.parse(readFileSync(join(dirname(require.resolve("d3-time-format")), "..", "locale", `${name}.json`), "utf8")));
  for (const specifier of ["%c", "%A %-d %B %Y %p %Z"]) localeFormats.push({locale: name, specifier, outputs: dates.map((d) => locale.format(specifier)(new Date(d)))});
}

process.stdout.write(JSON.stringify({localeFormats, transitions: shown, fields, compose, interval, counts, ranges, ticks, scale, formats, multiScale}));
