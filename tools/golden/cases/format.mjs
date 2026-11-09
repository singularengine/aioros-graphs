import {format, formatPrefix, formatSpecifier, precisionFixed, precisionPrefix, precisionRound} from "d3-format";
import {scaleLinear} from "d3-scale";
import {version} from "../version.mjs";

export const d3Version = version("d3-format");

const values = [
  0, -0, 1, -1, 0.5, 1.5, 2.5, -2.5, 0.1, 0.123456789, 1 / 3, 42, 999.5, 1234, -1234.5678, 12345678, 1e21, 1.5e22, 1e-7,
  0.00042, 123456.789, 3.14159, -0.0000001, 255, 1e100, 5e-324, 0.995, 1.005, NaN, Infinity, -Infinity, 1e-30, 1e30,
];

const specifiers = [
  "", "f", ".0f", ".1f", ".2f", ".20f", ",", ",.2f", ",d", "d", "e", ".0e", ".3e", "g", ".1g", ".3g", ".21g", "r", ".2r", ".4r",
  "s", ".3s", ".1s", "~s", ".3~s", "%", ".0%", ".1%", "p", ".2p", "x", "X", "#x", "o", "#o", "b", "#b", "c", "n", ".3n",
  "+", "+.2f", "-.2f", "(.2f", " .2f", "+,.0f", "$,.2f", "($,.2f", "08.2f", "08,.2f", "010,", "=+8.1f", ">8.1f", "<8.1f",
  "^8.1f", "*^9", "_<10,", "0>6", "0=7d", "20,", "020,.3f", ".2~f", "~f", "~e", ".3~g", "~%", "~r", ",.0%", "$.2s", "06.2s",
  ".6~s", ",.3~s", "#.3g", "#", "$", "10", ".2", "~", "+08,d", ",.12~g", ".12~g", "z", ".3Z",
];

const invalid = ["abc", "..2f", ".f2", "2.f.", "%%", ".-1f", "x5"];

export default function() {
  const formats = [];
  for (const specifier of specifiers) {
    const f = format(specifier);
    formats.push({specifier, normalized: formatSpecifier(specifier).toString(), outputs: values.map((value) => f(value))});
  }
  const rejected = invalid.map((specifier) => {
    try { format(specifier); return {specifier, valid: true}; } catch { return {specifier, valid: false}; }
  });
  const prefix = [];
  for (const [specifier, reference] of [[",.0", 1e3], [",.1", 1e6], [".2", 1e-6], ["$,.2", 1e9], [".0", 0.001], [",.3", 1e30], [".1", 1e-30], ["+.1", 0], ["~", 1500]]) {
    const f = formatPrefix(specifier, reference);
    prefix.push({specifier, reference, values, outputs: values.map((v) => f(v))});
  }
  const precision = [];
  for (const step of [1, 0.1, 0.01, 0.001, 0.5, 0.25, 1e-7, 10, 100, 1e5, 0.0025, 1.5e-3]) {
    for (const value of [1, 1.1, 99, 1000, 1e6, 0.001, 1e-9, 1e12]) {
      precision.push({step, value, fixed: precisionFixed(step), prefix: precisionPrefix(step, value), round: precisionRound(step, value)});
    }
  }
  const ticks = [];
  const domains = [[0, 1], [0, 10], [0, 100], [-1, 1], [0, 1e6], [1e6, 0], [0, 0.001], [-0.5, 0.5], [12, 87], [0, 1e-9], [1e9, 1e12], [3, 3], [0.1, 0.9]];
  for (const domain of domains) {
    for (const count of [1, 5, 10, 20]) {
      for (const specifier of [null, ",f", "s", "~s", ".2s", "e", "g", "r", "p", "%", "f", "+f", "$", ".1f", "d", "~g", "$,.2f", ",.0%"]) {
        const scale = scaleLinear().domain(domain);
        const ts = scale.ticks(count);
        const f = scale.tickFormat(count, specifier ?? undefined);
        ticks.push({start: domain[0], stop: domain[1], count, specifier, ticks: ts, outputs: ts.map(f)});
      }
    }
  }
  return {values: [{values}], formats, rejected, prefix, precision, ticks};
}
