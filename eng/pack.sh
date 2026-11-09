#!/usr/bin/env bash
# Usage: eng/pack.sh [version suffix]
# Packs Aioros.Graphs, Aioros.Graphs.Svg and Aioros.Graphs.Avalonia (with symbols) into artifacts/packages and prints
# the command that copies them into a consumer's local package folder.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
out="$root/artifacts/packages"
suffix="${1:-}"
mkdir -p "$out"
args=(-c Release -o "$out" --nologo -v quiet)
if [ -n "$suffix" ]; then args+=("-p:VersionSuffix=$suffix"); fi
for project in Aioros.Graphs Aioros.Graphs.Svg Aioros.Graphs.Avalonia; do
  dotnet pack "$root/src/$project/$project.csproj" "${args[@]}"
done
ls -1 "$out"
echo
echo "To use this build from a consumer repository (its nuget.config lists ./packages as a source):"
echo "  cp $out/Aioros.Graphs*.nupkg <consumer>/packages/"
