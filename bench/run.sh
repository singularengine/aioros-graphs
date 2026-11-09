#!/usr/bin/env bash
# Usage: bench/run.sh [micro|demo|all] [BenchmarkDotNet filter]
# Runs the engine's micro-benchmarks (BenchmarkDotNet, default job), the d3 micro-benchmarks (bench/d3/*.mjs) and the
# chart demo, each pinned to BENCH_CPUS (default: the first four CPUs this process may use), after the 1-minute load
# average falls below IDLE_LOAD (default 4) or WAIT_MINUTES (default 20) pass. Raw results go to bench/results.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/.." && pwd)"
profile="${1:-all}"
filter="${2:-*}"
commit="$(git -C "$root" rev-parse --short HEAD)"
day="$(date -u +%Y-%m-%d)"
allowed="$(taskset -pc $$ | sed 's/.*: //')"
first="${allowed%%[-,]*}"
cpus="${BENCH_CPUS:-$first-$((first + 3))}"
idle="${IDLE_LOAD:-4}"

wait_idle() {
  local deadline=$(( $(date +%s) + ${WAIT_MINUTES:-20} * 60 ))
  while awk -v idle="$idle" '{ exit !($1 >= idle) }' /proc/loadavg && [ "$(date +%s)" -lt "$deadline" ]; do
    echo "load $(cut -d' ' -f1-3 /proc/loadavg), waiting for < $idle"
    sleep 30
  done
  echo "load $(cut -d' ' -f1-3 /proc/loadavg) on CPUs $cpus"
}

if [ ! -d "$root/tools/golden/node_modules/d3" ]; then
  (cd "$root/tools/golden" && npm ci --silent)
fi

if [ "$profile" = micro ] || [ "$profile" = all ]; then
  out="$root/bench/results/micro/$day-$commit"
  mkdir -p "$out"
  dotnet build "$root/bench/Aioros.Graphs.Benchmarks" -c Release --nologo -v quiet
  wait_idle
  uptime > "$out/load-before.txt"
  (cd "$out" && taskset -c "$cpus" dotnet "$root/bench/Aioros.Graphs.Benchmarks/bin/Release/net10.0/Aioros.Graphs.Benchmarks.dll" \
    --filter "$filter" --exporters json --artifacts "$out/engine")
  for script in "$root"/bench/d3/*.mjs; do
    TZ=Europe/Berlin taskset -c "$cpus" node "$script" "$out/d3-$(basename "$script" .mjs).json"
  done
  uptime > "$out/load-after.txt"
  echo "micro results in bench/results/micro/$day-$commit"
fi

if [ "$profile" = demo ] || [ "$profile" = all ]; then
  wait_idle
  taskset -c "$cpus" "$root/samples/Demo/run.sh"
fi
