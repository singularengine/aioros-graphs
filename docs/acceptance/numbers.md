# Numbers

JavaScript's number formatting, which d3 relies on everywhere it writes a number (`JsNumber`). Golden fixture:
`tests/Aioros.Graphs.Tests/Golden/numbers.json`, from `tools/golden/cases/numbers.mjs` (Node's own
`Number.prototype`, no d3 module).

| ID | Criterion |
|---|---|
| NUMBER-01 | Given each value of the `shortest` cases (integers, fractions, ±0, 1e21 and beyond, 1e-7 and below, the extremes, NaN and ±Infinity), when `JsNumber.ToShortestString` formats it, then the text equals JavaScript's `String(value)` and `WriteShortest` writes the same text as UTF-8. |
| NUMBER-02 | Given each value and digit count of the `fixed` cases, when `JsNumber.ToFixed` formats it, then the text equals `value.toFixed(digits)`, exact ties rounded up and values from 1e21 written as `String(value)`. |
| NUMBER-03 | Given each value and digit count of the `exponential` cases, including no digit count, when `JsNumber.ToExponential` formats it, then the text equals `value.toExponential(digits)`. |
| NUMBER-04 | Given each value and precision of the `precision` cases, when `JsNumber.ToPrecision` formats it, then the text equals `value.toPrecision(precision)`, in exponential form when the exponent is below -6 or not below the precision. |
| NUMBER-05 | Given each value of the `round` cases, when `JsNumber.Round` rounds it, then the result equals `Math.round(value)`: halves go toward positive infinity, 0.49999999999999994 gives 0 and non-finite values are returned unchanged. |
| NUMBER-06 | Given each value and digit count of the `rounded` cases, when `JsNumber.WriteRounded` writes it, then the UTF-8 text equals `String(Math.round(value * 10 ** digits) / 10 ** digits)`, as d3-path writes rounded coordinates. |
| NUMBER-07 | Given each value of the `trig` cases of `Golden/trig.json` (the first 2,000 phyllotaxis angles, uniform values in ±10, magnitudes from 1e-10 to 1e300 that take fdlibm's large argument reduction, multiples of π/2 and their neighbours, ±0, ±Infinity and NaN), when `JsTrig.Sin` and `JsTrig.Cos` compute them, then the results equal V8's `Math.sin` and `Math.cos` bit for bit (fdlibm, as V8's `ieee754.cc` ports it; .NET's `Math.Sin` and `Math.Cos` differ in the last bit on about 3% of these values). |
