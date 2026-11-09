import {axisBottom, axisLeft} from "d3-axis";
import {scaleTime} from "d3-scale";
import {render} from "./dom.mjs";

const spans = [
  [new Date(2024, 0, 1), new Date(2024, 11, 31)], [new Date(2024, 2, 30, 18), new Date(2024, 2, 31, 12)],
  [new Date(2024, 9, 26), new Date(2024, 10, 9)], [new Date(1995, 0, 1), new Date(2025, 0, 1)],
];
const cases = [];
for (const [orient, make] of Object.entries({bottom: axisBottom, left: axisLeft})) {
  for (const [start, stop] of spans) {
    for (const args of [[], [6], [5, "%Y-%m-%d %H:%M"]]) {
      const scale = scaleTime([start, stop], [40, 616]);
      cases.push({orient, domain: [+start, +stop], range: [40, 616], tickArguments: args, options: {}, ...render(make(scale).ticks(...args))});
    }
  }
}
process.stdout.write(JSON.stringify(cases));
