#!/usr/bin/env bash
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
out="${1:-$here/out}"
commit="$(git -C "$root" rev-parse --short HEAD)"

if [ ! -d "$root/tools/golden/node_modules/d3" ]; then
  (cd "$root/tools/golden" && npm ci --silent)
fi

dotnet build "$here/Demo.csproj" -c Release -m:4 --nologo -v quiet
dotnet run --project "$here/Demo.csproj" -c Release --no-build -- data "$out"
node "$here/d3.mjs" "$out"
dotnet run --project "$here/Demo.csproj" -c Release --no-build -- page "$out" "$commit"

mkdir -p "$root/bench/results/demo"
cp "$out/results.json" "$root/bench/results/demo/$(date -u +%Y-%m-%d)-$commit.json"
echo "raw results in bench/results/demo/$(date -u +%Y-%m-%d)-$commit.json"
