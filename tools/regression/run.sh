#!/bin/bash
# Side-by-side regression run: builds the app twice — "base" (a git ref, default master) and "head" (your working
# copy) — runs both in a browser with the same scripted clicks and key presses, and compares everything they produce.
# A refactor should give "ALL THE SAME".
#
# Usage (from the repo root):  tools/regression/run.sh [base-ref]
#                              SUITES="keyboard panel" tools/regression/run.sh   (only some suites)
# Needs: .NET 8 SDK, Node with Playwright (+ Chromium), Python 3 with Pillow. Built for Claude's cloud workspace;
# see tools/regression/README.md.
set -uo pipefail
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../.." && pwd)
base_ref=${1:-master}
work=${WORK:-/tmp/bediagram-regression}
export NODE_PATH=${NODE_PATH:-$(npm root -g)}

for port in 5101 5102; do
    if curl -s -o /dev/null "http://localhost:$port/"; then
        echo "Port $port is already in use (a server left over from an earlier run?). Stop it and try again."; exit 1
    fi
done
echo "== Base: $base_ref   Head: working copy   Work folder: $work"
rm -rf "$work"; mkdir -p "$work"/{base-store,head-store,base-out,head-out}
git -C "$repo" worktree remove --force "$work/base-src" >/dev/null 2>&1 || true
git -C "$repo" worktree add -q --detach "$work/base-src" "$base_ref" || exit 1

echo "== Building .NET 8 copies"
"$here/prepare-net8.sh" "$work/base-src/BeDiagram" "$work/base-app" || exit 1
"$here/prepare-net8.sh" "$repo/BeDiagram" "$work/head-app" || exit 1

start() { # side port — starts that build's app server in its own process group and remembers the group id
    # (the dotnet launcher runs the server as a child process, so the whole group is stopped, not just one pid)
    (cd "$work/$1-app" && Storage__Folder="$work/$1-store" ASPNETCORE_URLS="http://localhost:$2" \
        setsid bash -c 'echo $$ > "$0"; exec dotnet bin/Debug/net8.0/BeDiagram.dll' "$work/$1.pid" > "$work/$1.log" 2>&1 &)
}
stop() { for side in base head; do [ -f "$work/$side.pid" ] && kill -- -"$(cat "$work/$side.pid")" 2>/dev/null; done; }
trap stop EXIT
start base 5101; start head 5102
up=no
for i in $(seq 1 30); do
    curl -s -o /dev/null http://localhost:5101/ && curl -s -o /dev/null http://localhost:5102/ && { up=yes; break; }; sleep 1
done
[ $up = yes ] || { echo "The app servers didn't start — see $work/base.log and $work/head.log"; exit 1; }

suite() { # name — runs the named suite on base, then head (one after the other, never in parallel)
    for side in base head; do
        port=$([ $side = base ] && echo 5101 || echo 5102)
        store="$work/$side-store"; out="$work/$side-out"
        rm -rf "$store"/*
        case $1 in
            gestures)   mkdir -p "$out/gestures"
                        node "$here/gestures.js" "http://localhost:$port/" "$store" "$out/gestures" > /dev/null
                        node "$here/bend.js" "http://localhost:$port/" "$store" "$out/gestures" > /dev/null ;;
            keyboard)   node "$here/keyboard.js" "http://localhost:$port/" "$out/keyboard.json" > /dev/null ;;
            all-shapes) mkdir -p "$out/all-shapes"; python3 "$here/make-shapes-diagram.py" "$store" > /dev/null
                        node "$here/all-shapes.js" "http://localhost:$port/" "$store" "$out/all-shapes" > /dev/null ;;
            panel)      python3 "$here/make-shapes-diagram.py" "$store" > /dev/null
                        node "$here/panel.js" "http://localhost:$port/" "$out/panel.json" > /dev/null ;;
        esac || echo "   $1 failed on $side (see $work/$side.log)"
    done
    echo "   $1 done"
}
echo "== Running suites (the panel suite takes about 10 minutes per build)"
for s in ${SUITES:-gestures keyboard all-shapes panel}; do suite $s; done

echo "== Results"
python3 "$here/compare.py" "$work"
status=$?
git -C "$repo" worktree remove --force "$work/base-src" >/dev/null 2>&1
exit $status
