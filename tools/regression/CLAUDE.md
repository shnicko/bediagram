# Regression suites — notes for Claude

`README.md` in this folder explains the suites, `blazor.js`, `pick-suites.py` and `compare.py`. These are the
points that matter when running or changing them in Claude's cloud workspace.

## Running

- `run.sh [base-ref]` builds master (or `base-ref`) and the working copy as .NET 8 copies (`prepare-net8.sh`), runs
  the suites on both and compares them. `FULL=1` forces all four suites; `SUITES=` / `PANELS=` choose.
  `pick-suites.py` prints why it picked what it did; a file not in its table gets the full set.
- A full run takes ~15 min. Run it in the background and poll: long runs can be cut off when the workspace restarts.
- Commit harness changes before a picked run, or the harness files themselves make it a full run.

## Reading the result

- A `check:` line after a suite means a wait gave up or a label box never got focus. Look into it before trusting
  the result.
- A toast that vanishes one step apart is timing, not behaviour. Rerun to confirm.
- Refactors must come out ALL THE SAME. Features should differ only where they change things; say which differences
  are expected in the PR.

## Changing the suites

- Waits follow Blazor's WebSocket messages (`blazor.js`), never fixed pauses: new suite code does
  `await z.act(() => page.click(…))`.
- When a feature adds a shape, extend `make-shapes-diagram.py`; when it adds a shortcut, extend `keyboard.js`.
  `keyboard.js` / `panel.js` pick up new panel controls by themselves.
- The .NET 8 copy (`prepare-net8.sh`) retargets to `net8.0`, swaps `App.razor` for a plain .NET 8 host page (no
  `@Assets`, `ImportMap`, `ResourcePreloader`, `ReconnectModal`), uses `UseStaticFiles()` instead of
  `MapStaticAssets()` and drops `NotFoundPage` from Routes. App code is copied unchanged. If the app starts using
  another .NET 10-only feature, extend the script.
