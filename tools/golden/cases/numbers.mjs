export const d3Version = null;

const values = [
  0, -0, 1, -1, 0.1, 0.2 + 0.1, 1 / 3, -2 / 3, 123.456, 1e21, 1.5e21, 9.999999999999999e20, 1e-6, 1.5e-6, 1e-7, 1.234e-7,
  123456789012345680000, 5e-324, 1.7976931348623157e308, Math.PI, -Math.E, 0.5, 1.5, 2.5, -2.5, 1.005, 1.0005, 0.000001234,
  1e15, 1e16, 12345678.9, 0.045, 0.0045, 99.995, 0.15, 1.45, 8.345, 1e-10, 255, 1000000, NaN, Infinity, -Infinity,
];

export default function() {
  const shortest = values.map((value) => ({value, text: String(value)}));
  const fixed = [], exponential = [], precision = [];
  for (const value of values) {
    for (const digits of [0, 1, 2, 3, 6, 12, 20]) {
      fixed.push({value, digits, text: value.toFixed(digits)});
      exponential.push({value, digits, text: value.toExponential(digits)});
    }
    exponential.push({value, digits: null, text: value.toExponential()});
    for (const digits of [1, 2, 3, 6, 12, 21]) precision.push({value, digits, text: value.toPrecision(digits)});
  }
  const round = values.concat([0.49999999999999994, -0.5, -0.4, -1.5, 2.4999, 4503599627370495.5]).map((value) => ({value, rounded: Math.round(value)}));
  const rounded = [];
  for (const value of values.concat([0.0005, 0.0015, -0.0005, 1.2345, 1e-4 + 1e-12, 1234567.8915, 1e12 + 0.0005])) {
    for (const digits of [0, 1, 3, 6, 9]) {
      const k = 10 ** digits;
      rounded.push({value, digits, text: String(Math.round(value * k) / k)});
    }
  }
  return {shortest, fixed, exponential, precision, round, rounded};
}
