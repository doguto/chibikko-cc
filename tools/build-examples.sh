#!/usr/bin/env bash
set -uo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
out="$root/examples/out/$(date +%Y%m%d-%H%M%S)"
mkdir -p "$out"

if ! dotnet build "$root/src" -v q -nologo > "$out/dotnet-build.log" 2>&1; then
    echo "[ERROR] dotnet build failed: $out/dotnet-build.log"
    exit 1
fi

pass=0
fail=0

for src in "$root"/examples/*.c; do
    name="$(basename "$src" .c)"

    if ! dotnet run --project "$root/src" --no-build -- "$src" > "$out/$name.s" 2> "$out/$name.compile.log"; then
        echo "[FAIL] $name: compile ($out/$name.compile.log)"
        fail=$((fail + 1))
        continue
    fi

    if ! gcc "$out/$name.s" -o "$out/$name" 2> "$out/$name.gcc.log"; then
        echo "[FAIL] $name: assemble ($out/$name.gcc.log)"
        fail=$((fail + 1))
        continue
    fi

    "$out/$name" > "$out/$name.stdout" 2>&1
    actual=$?

    gcc "$src" -o "$out/$name.expected" 2> /dev/null
    "$out/$name.expected" > /dev/null 2>&1
    expected=$?

    if [ "$actual" -eq "$expected" ]; then
        echo "[PASS] $name: exit=$actual"
        pass=$((pass + 1))
    else
        echo "[FAIL] $name: exit=$actual expected=$expected"
        fail=$((fail + 1))
    fi
done

echo "[RESULT] pass=$pass fail=$fail output=$out"
[ "$fail" -eq 0 ]
