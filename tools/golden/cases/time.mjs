import * as t from "d3-time";
import {utcFormat} from "d3-time-format";
import {scaleUtc} from "d3-scale";
import {version} from "../version.mjs";

export const d3Version = version("d3-time");

const dates = [
  0, -1, 1, 951782400000, 951868799999, 951825845678, 1078012800000, 1709210096123, 1704067199999, 1704067200000,
  -2208988800001, -62135596800000, -86400000 * 365.25 * 3000, 253402300799999, 1e15, -1e15, 1234567890123, 1696118400000.5,
  Date.UTC(2023, 0, 31, 12), Date.UTC(2024, 11, 31, 23, 59, 59, 999), Date.UTC(2000, 1, 29, 6, 30), Date.UTC(1900, 1, 28, 18),
  Date.UTC(1969, 11, 31, 23, 59, 59, 500), Date.UTC(2021, 6, 4, 0, 0, 0), Date.UTC(99, 4, 5), Date.UTC(-500, 3, 3, 3, 3, 3, 3),
  Date.UTC(2020, 11, 31), Date.UTC(2021, 0, 3), Date.UTC(2015, 11, 28), Date.UTC(2027, 0, 1), Date.UTC(2026, 0, 4, 23), Date.UTC(2009, 11, 31),
];

const intervals = {
  millisecond: t.utcMillisecond, second: t.utcSecond, minute: t.utcMinute, hour: t.utcHour, day: t.utcDay, unixDay: t.unixDay,
  week: t.utcSunday, monday: t.utcMonday, tuesday: t.utcTuesday, wednesday: t.utcWednesday, thursday: t.utcThursday,
  friday: t.utcFriday, saturday: t.utcSaturday, month: t.utcMonth, year: t.utcYear,
};

function ms(d) { return d == null ? null : +d; }

export default function() {
  const fields = dates.map((d) => {
    const x = new Date(d), c = new Date(0);
    c.setUTCFullYear(x.getUTCFullYear(), x.getUTCMonth() + 14, x.getUTCDate() - 40);
    c.setUTCHours(x.getUTCHours() + 30, -5, 70, 1500);
    return {date: d, year: x.getUTCFullYear(), month: x.getUTCMonth(), day: x.getUTCDate(), weekday: x.getUTCDay(),
      hours: x.getUTCHours(), minutes: x.getUTCMinutes(), seconds: x.getUTCSeconds(), milliseconds: x.getUTCMilliseconds(),
      composed: +c};
  });
  const interval = [];
  for (const [name, i] of Object.entries(intervals)) {
    for (const step of [1, 2, 3, 5, 7, 15, 25, 100]) {
      const every = step === 1 ? i : i.every(step);
      if (!every) continue;
      for (const date of dates) {
        interval.push({name, step, date, floor: ms(every.floor(date)), ceil: ms(every.ceil(date)), round: ms(every.round(date)),
          offset: [-3, -1, 0, 1, 2.7, 13].map((k) => ms(every.offset(date, k)))});
      }
    }
  }
  const every = [];
  for (const [name, i] of Object.entries(intervals)) {
    for (const step of [0, -1, NaN, Infinity, 0.5, 1, 1.9, 4]) every.push({name, step, exists: i.every(step) != null});
  }
  const counts = [];
  for (const [name, i] of Object.entries(intervals)) {
    for (let k = 0; k + 1 < dates.length; k += 2) counts.push({name, start: dates[k], end: dates[k + 1], count: i.count(dates[k], dates[k + 1])});
  }
  const ranges = [];
  const spans = [[0, 3500], [951782400000, 951782400000 + 86400000 * 3], [Date.UTC(2023, 0, 31), Date.UTC(2023, 7, 31)],
    [Date.UTC(1999, 5, 15, 13), Date.UTC(2003, 2, 1)], [-86400000 * 10.5, 86400000 * 4.25], [Date.UTC(2020, 1, 1), Date.UTC(2020, 1, 1)]];
  for (const [name, i] of Object.entries(intervals)) {
    for (const [start, stop] of spans) {
      for (const step of [1, 2, 0, -1, 2.9]) {
        if (step >= 1 && i.count(start, stop) / Math.floor(step) > 400) continue;
        ranges.push({name, start, stop, step, values: i.range(start, stop, step).map(ms)});
      }
    }
  }
  const ticks = [];
  const tickSpans = [
    [0, 1], [0, 7], [0, 1000], [0, 4321], [0, 60000], [0, 3600000 * 3], [0, 86400000], [0, 86400000 * 9], [0, 86400000 * 40],
    [0, 86400000 * 200], [0, 86400000 * 800], [0, 86400000 * 365 * 30], [0, 86400000 * 365 * 400], [Date.UTC(2011, 0, 1, 12, 0, 27), Date.UTC(2011, 0, 1, 12, 4, 4)],
    [Date.UTC(2011, 0, 1), Date.UTC(2011, 0, 1, 12)], [Date.UTC(2011, 0, 2, 0, 0, 7), Date.UTC(2011, 0, 1, 12, 0)],
    [Date.UTC(1969, 5, 1), Date.UTC(1970, 5, 1)], [-1e15, 1e15], [Date.UTC(2023, 2, 1, 0, 0, 0, 1), Date.UTC(2023, 2, 1, 0, 0, 0, 7)],
  ];
  for (const [start, stop] of tickSpans) {
    for (const count of [1, 5, 10, 13, 50]) {
      const ti = t.utcTickInterval(start, stop, count);
      ticks.push({start, stop, count, ticks: t.utcTicks(start, stop, count).map(ms), hasInterval: ti != null});
    }
  }
  const nice = [];
  for (const [start, stop] of tickSpans) {
    for (const count of [5, 10]) nice.push({start, stop, count, domain: scaleUtc().domain([start, stop]).nice(count).domain().map(ms)});
  }
  const directives = ["%a", "%A", "%b", "%B", "%d", "%e", "%f", "%H", "%I", "%j", "%L", "%m", "%M", "%p", "%q", "%Q", "%s", "%S", "%u", "%w", "%y", "%Y", "%Z", "%%",
    "%-d", "%_d", "%0e", "%-H", "%_m", "%-j", "%-I:%M %p", "%Y-%m-%dT%H:%M:%S.%LZ", "%b %-d, %Y", "x%kx", "%", "plain", "%-", "% ",
    "%U", "%W", "%V", "%g", "%G", "%-U", "%_W", "%-V", "%c", "%x", "%X", "%-c", "%G-W%V-%u", "%Y-%U-%w"];
  const formats = [];
  for (const specifier of directives) formats.push({specifier, outputs: dates.map((d) => utcFormat(specifier)(d))});
  const multi = scaleUtc().tickFormat();
  const multiScale = dates.concat(ticks.flatMap((x) => x.ticks).slice(0, 400)).map((date) => ({date, text: multi(new Date(date))}));
  return {dates: [{dates}], fields, interval, every, counts, ranges, ticks, nice, formats, multiScale};
}
