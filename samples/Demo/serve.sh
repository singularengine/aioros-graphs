#!/usr/bin/env bash
# Usage: samples/Demo/serve.sh [urls]
# Writes d3's reference for the demo's data with Node (out/d3.json), then serves the demo site (default
# http://localhost:5180). The site draws every chart with the engine on each request and compares it with the reference.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
urls="${1:-http://localhost:5180}"

if [ ! -d "$root/tools/golden/node_modules/d3" ]; then
  (cd "$root/tools/golden" && npm ci --silent)
fi

dotnet build "$here/Demo.csproj" -c Release -m:4 --nologo -v quiet
dotnet run --project "$here/Demo.csproj" -c Release --no-build -- data "$here/out"
node "$here/d3.mjs" "$here/out"
cd "$here"
exec dotnet run --project "$here/Demo.csproj" -c Release --no-build -- --urls "$urls"
