import {path, pathRound} from "d3-path";
import {version} from "../version.mjs";

export const d3Version = version("d3-path");

const programs = {
  lines: [["moveTo", 150, 50], ["lineTo", 200, 100], ["lineTo", 100.123456, 100.5], ["closePath"]],
  curves: [["moveTo", 10.0005, 20], ["quadraticCurveTo", 30, 40, 50.25, 60], ["bezierCurveTo", 1 / 3, 2 / 3, 1e-7, -1e-7, 1e21, 7]],
  arcFromEmpty: [["arc", 100, 100, 50, 0, Math.PI / 2]],
  arcJoined: [["moveTo", 0, 0], ["arc", 100, 100, 50, 0, Math.PI / 2]],
  arcCounterclockwise: [["moveTo", 150, 100], ["arc", 100, 100, 50, 0, Math.PI / 2, true]],
  arcLarge: [["moveTo", 150, 100], ["arc", 100, 100, 50, 0, 1.5 * Math.PI]],
  arcCircle: [["moveTo", 150, 100], ["arc", 100, 100, 50, 0, 2 * Math.PI]],
  arcNegative: [["moveTo", 150, 100], ["arc", 100, 100, 50, 0, -Math.PI / 3]],
  arcEmpty: [["moveTo", 150, 100], ["arc", 100, 100, 50, 1, 1]],
  arcZeroRadius: [["moveTo", 0, 0], ["arc", 100, 100, 0, 0, 1], ["closePath"]],
  arcZeroRadiusFirst: [["arc", 100, 100, 0, 0, 1], ["closePath"], ["lineTo", 5, 5]],
  arcThenClose: [["arc", 0, 0, 10, 0, 1], ["closePath"], ["arc", 0, 0, 10, 1, 2]],
  arcToEmpty: [["arcTo", 270, 39, 163, 100, 53]],
  arcToCoincident: [["moveTo", 270, 182], ["arcTo", 270, 182, 163, 100, 53]],
  arcToCollinear: [["moveTo", 270, 182], ["arcTo", 270, 39, 270, 10, 53]],
  arcToZeroRadius: [["moveTo", 270, 182], ["arcTo", 270, 39, 163, 100, 0]],
  arcToTangent: [["moveTo", 270, 182], ["arcTo", 270, 39, 163, 100, 53]],
  arcToOtherWay: [["moveTo", 270, 182], ["arcTo", 270, 39, 363, 100, 53]],
  rect: [["moveTo", 1, 1], ["rect", 10.5, 20.25, 30.0004, -40.0006], ["lineTo", 3, 3], ["closePath"]],
  closeEmpty: [["closePath"], ["moveTo", 1, 2], ["closePath"], ["closePath"]],
};

export default function() {
  const cases = [];
  for (const [name, commands] of Object.entries(programs)) {
    for (const digits of [null, 3, 0]) {
      const p = digits == null ? path() : pathRound(digits);
      for (const [method, ...args] of commands) p[method](...args);
      cases.push({name, digits, commands, path: p.toString()});
    }
  }
  return {programs: cases};
}
