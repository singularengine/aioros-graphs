import {readFileSync} from "node:fs";
import {fileURLToPath} from "node:url";
import {dirname, join} from "node:path";

const here = dirname(fileURLToPath(import.meta.url));

export function version(module) {
  return JSON.parse(readFileSync(join(here, "node_modules", module, "package.json"), "utf8")).version;
}
