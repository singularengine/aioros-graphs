import {scaleSequential, scaleSequentialSqrt, scaleSequentialLog, scaleDiverging, scaleDivergingSymlog} from "d3-scale";
import * as c from "d3-scale-chromatic";
import {version} from "../version.mjs";

export const d3Version = version("d3-scale");

const inputs = [-1e3, -50, -1, -0.5, 0, 0.001, 0.5, 1, 2.5, 7, 10, 33.3, 50, 99.99, 100, 250, 1e4, NaN];

export default function() {
  const sequential = [];
  for (const [name, scheme] of [["viridis", c.interpolateViridis], ["plasma", c.interpolatePlasma], ["turbo", c.interpolateTurbo], ["Blues", c.interpolateBlues], ["YlOrRd", c.interpolateYlOrRd]]) {
    for (const [kind, make, domain] of [["linear", scaleSequential, [0, 100]], ["sqrt", scaleSequentialSqrt, [0, 100]], ["log", scaleSequentialLog, [1, 1e4]], ["linear", scaleSequential, [100, 0]]]) {
      const scale = make(domain, scheme);
      sequential.push({scheme: name, kind, domain, inputs, outputs: inputs.map((x) => scale(x) ?? null)});
    }
  }
  const diverging = [];
  for (const [name, scheme] of [["RdBu", c.interpolateRdBu], ["RdYlGn", c.interpolateRdYlGn], ["PuOr", c.interpolatePuOr], ["Spectral", c.interpolateSpectral]]) {
    for (const [kind, make, domain] of [["linear", scaleDiverging, [-50, 0, 100]], ["symlog", scaleDivergingSymlog, [-1000, 0, 1000]], ["linear", scaleDiverging, [100, 10, -1]]]) {
      const scale = make(domain, scheme);
      diverging.push({scheme: name, kind, domain, inputs, outputs: inputs.map((x) => scale(x) ?? null)});
    }
  }
  return {sequential, diverging};
}
