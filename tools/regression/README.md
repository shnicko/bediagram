# Regression tests

**What they're for:** checking that a change didn't alter how BeDiagram behaves. They were built during the
design-pattern refactoring (PRs #15–#19), where the goal was "move the code around, but change nothing the user can see".

**How they work, in one paragraph:** the app is built twice, once from `master` (*base*, before your change) and once
from your working copy (*head*, after it). A script then drives a real browser on each build. It adds shapes, drags
them, presses every keyboard shortcut and clicks every button in the side panel, and records exactly what the app
shows and saves after each step. Finally the two recordings are compared. If every step matches, the change didn't
alter behaviour. If something differs, the report names the step, e.g. `keyboard: Control+d → svg`, meaning the
drawing looked different after pressing Ctrl+D.

These are *regression* tests: they don't know what the right answer is, only whether it's the same as before. So a
refactor should come out **ALL THE SAME**, while a new feature will (correctly) show differences in the areas it changes.

## The suites

| Suite | What it does | What it compares |
|---|---|---|
| `gestures.js` + `bend.js` | Adds shapes; drags, multi-selects, connects (to a shape and to empty canvas), marquee-selects, groups, resizes, moves connector ends and bends, pans, presses Escape mid-drag, undoes / redoes. Then runs every export on an open canvas and on A4 | The saved diagram after each step (12 files); the SVG, PNG, PDF and Mermaid exports |
| `keyboard.js` | Presses every shortcut, including Shift / Ctrl variants that should do the same thing or nothing; clicks the toolbar and panel buttons that run commands; opens and closes the dialogs | After each of 94 steps: the canvas, zoom, selection, edit state, dialog, toast, panel text, toolbar tooltips and which buttons are greyed out |
| `all-shapes.js` | Opens a test diagram with every shape type (with and without an icon), all 5 outcomes, link badges, descriptions, fills, a group and 30 straight / elbow connectors (made by `make-shapes-diagram.py`) | The canvas drawing and every export, on an open canvas and on A3 |
| `panel.js` | For 19 selections (nothing, 12 shapes, text box, comment, 2 connectors, group, several items), clicks every button, checkbox, dropdown and slider in the side panel | After each of ~830 steps: the canvas, panel text, selection, edit state, toast and Undo state |

Scripts find a shape by its outline path with `.node > path:not(.node-shadow)` (each shape's first direct path that isn't its shadow), so the extra shadow / halo paths drawn since the presentation look don't shift which shape is clicked.

`compare.py` does the comparing. It explains its rules at the top: random ids are ignored, PDFs ignore their timestamp,
PNGs are compared pixel by pixel, and Blazor's component markers (`<!--!-->`) are dropped.

## Waiting for the app (`blazor.js`)

After each click or key the scripts must wait until the app has finished before recording anything. They used to sleep
a fixed time (150–1,500 ms) and copy the whole canvas back and forth to see if it had stopped changing. That was slow,
and still a guess on a busy machine.

Now they wait for the app itself. Blazor Server runs the app on the server and talks to the page over one WebSocket.
`blazor.js` listens to that conversation:

| Message | Means |
|---|---|
| `BeginInvokeDotNetFromJS` (with a call number) | the browser sent an event, e.g. a click |
| `JS.EndInvokeDotNet` (same number) | the server's handler has finished, including awaited work such as saving a file |
| `JS.RenderBatch` / `OnRenderCompleted` | the server sent a screen update / the browser has drawn it |
| `JS.BeginInvokeJS` / `EndInvokeJSFromDotNet` | the server asked the browser to do something (focus a box, draw a PNG) / it's done |

A wait ends when every call is answered, every update drawn, every browser job done, and nothing has moved for 40 ms.
That usually takes 70–200 ms. A script writes `await z.act(() => page.click(…))` to do one thing and wait for it. If
a wait ever gives up (15 s), or a new label box never gets focus, the suite says so and `run.sh` prints it as `check:`.

## Which suites run (`pick-suites.py`)

`run.sh` looks at the files you changed (compared with the base, including files not committed yet) and runs only the
suites that can see them. It prints why:

```
== Changed files (compared with master) and the suites they need
   BeDiagram/Components/Panels/ConnectorPanel.razor: panel (panels: connector)
== Suites: panel   (panels: connector)
```

The table is in `pick-suites.py`. Each part of the page is its own component, so a change to the connector panel only
needs the connector selections of the panel suite (88 steps, not 876). Anything the table doesn't name — models,
editor, exports, canvas layers, gestures, `designer.js`, the command registry — gets the full set, so a gap in the table
costs time, never coverage. Changes the suites can't see (docs) run nothing. Try it without running anything:

```
python3 tools/regression/pick-suites.py . master BeDiagram/Components/Panels/ShapePanel.razor
```

## Running them

From the repo root:

```
tools/regression/run.sh            # compare your working copy with master (suites picked from what changed)
tools/regression/run.sh my-branch  # or with any other branch / commit
FULL=1 tools/regression/run.sh     # every suite, whatever changed
SUITES="keyboard" tools/regression/run.sh   # these suites: gestures, keyboard, all-shapes, panel
SUITES=panel PANELS="shape multi" tools/regression/run.sh   # some panels: shape text connector group multi diagram
```

The full set takes about 15 minutes on Claude's 2-core workspace: building both copies about 4, gestures under 1,
keyboard about 1, all-shapes under 1, and the panel suite about 13 (it was about 75 minutes before the waits changed).
Each suite's own output is kept as `<side>-<suite>.log` in the work folder. It ends with a short report:

```
gestures             26 files  SAME
keyboard             94 steps  SAME
all-shapes           17 files  SAME
panel               876 steps  SAME
ALL THE SAME
```

**Where it runs:** it was built for Claude's cloud workspace, which has the .NET 8 SDK, Node with Playwright and
Python with Pillow installed. NuGet is blocked there, so `prepare-net8.sh` makes a .NET 8 copy of the app: the app
code is unchanged, and only the hosting bits .NET 8 lacks are adjusted. It runs `master` on port 5101 and your copy on
port 5102, and stops if either port is already in use.

Running it on your own PC (Windows) needs Git Bash or WSL, Node + `npx playwright install chromium`, Python +
`pip install pillow`, and the .NET 8 SDK. That's possible, but not needed: asking Claude to "run the regression
suites" is the intended use.

**Toasts:** status-bar messages disappear after 3.5 seconds, which is now several steps, so one can linger a step or
two longer on one build. `compare.py` reports that as timing (`ignored … toast that expired a step apart`), not a
difference.

## When to update them

- **Refactor:** run, expect ALL THE SAME.
- **New feature:** run, expect differences only where the feature changes things. If the feature adds a shortcut, a
  panel control or a shape, `keyboard.js` / `panel.js` pick up new panel controls automatically; add new shapes or
  shortcuts to `make-shapes-diagram.py` / `keyboard.js` so they're covered next time.
