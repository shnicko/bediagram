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

`compare.py` does the comparing. It explains its rules at the top: random ids are ignored, PDFs ignore their timestamp,
and PNGs are compared pixel by pixel.

## Running them

From the repo root:

```
tools/regression/run.sh            # compare your working copy with master
tools/regression/run.sh my-branch  # or with any other branch / commit
SUITES="keyboard" tools/regression/run.sh   # only some suites: gestures, keyboard, all-shapes, panel
```

It takes about 25 minutes (the panel suite is the slow one) and ends with a short report:

```
gestures             26 files  SAME
keyboard             94 steps  SAME
all-shapes           17 files  SAME
panel               830 steps  SAME
ALL THE SAME
```

**Where it runs:** it was built for Claude's cloud workspace, which has the .NET 8 SDK, Node with Playwright and
Python with Pillow installed. NuGet is blocked there, so `prepare-net8.sh` makes a .NET 8 copy of the app: the app
code is unchanged, and only the hosting bits .NET 8 lacks are adjusted. It runs `master` on port 5101 and your copy on
port 5102, and stops if either port is already in use.

Running it on your own PC (Windows) needs Git Bash or WSL, Node + `npx playwright install chromium`, Python +
`pip install pillow`, and the .NET 8 SDK. That's possible, but not needed: asking Claude to "run the regression
suites" is the intended use.

**One known false alarm:** status-bar messages (toasts) disappear after 3.5 seconds, so occasionally one vanishes a
step earlier on one build than the other. If the *only* difference is a toast, and it lands on a different step when
you run again, it's timing, not a change.

## When to update them

- **Refactor:** run, expect ALL THE SAME.
- **New feature:** run, expect differences only where the feature changes things. If the feature adds a shortcut, a
  panel control or a shape, `keyboard.js` / `panel.js` pick up new panel controls automatically; add new shapes or
  shortcuts to `make-shapes-diagram.py` / `keyboard.js` so they're covered next time.
