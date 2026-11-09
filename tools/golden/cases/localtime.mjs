import {spawnSync} from "node:child_process";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";
import {version} from "../version.mjs";

export const d3Version = version("d3-time");

const zones = ["UTC", "Europe/Berlin", "America/Mexico_City", "America/Sao_Paulo", "Australia/Lord_Howe", "Asia/Kolkata", "America/New_York"];
const helper = join(dirname(fileURLToPath(import.meta.url)), "local", "zone.mjs");

export default function() {
  const groups = {};
  for (const zone of zones) {
    const result = spawnSync(process.execPath, [helper], {env: {...process.env, TZ: zone}, maxBuffer: 1 << 28});
    if (result.status !== 0) throw new Error(result.stderr.toString());
    for (const [group, list] of Object.entries(JSON.parse(result.stdout.toString()))) {
      (groups[group] ??= []).push(...list.map((c) => ({zone, ...c})));
    }
  }
  return groups;
}
