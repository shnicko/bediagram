"""Picks the regression suites a change needs, from the files it touches.

Usage: python3 pick-suites.py <repo> <base-ref> [file …]
(with files: what those would need, without asking git — handy for trying the rules)
Prints two shell lines for run.sh to eval, SUITES="…" and PANELS="…" (PANELS empty = every panel), and explains the
choice on stderr.

How it decides: each changed file (compared with base-ref, including uncommitted and new files) is matched against
RULES, top to bottom, and the first match says what it needs. A file that matches no rule needs the FULL set, so a
gap in the table costs time, never coverage. That also keeps the agreed rule: shared code (models, editor, exports,
canvas, keys) always gets the full run.

The child components are what make this possible: each part of the page is its own file, so a change to the connector
panel is visibly just that, and only the connector selections of the panel suite need to run.
"""
import fnmatch, subprocess, sys

ALL = ['gestures', 'keyboard', 'all-shapes', 'panel']
FULL = ('FULL', None)

# (file pattern, suites, panel kinds or None for every panel). Kinds: shape text connector group multi diagram.
RULES = [
    # Not code the suites can see.
    ('*.md', [], None),
    ('.gitignore', [], None),
    ('.editorconfig', [], None),
    ('tools/Check-OneTypePerFile.ps1', [], None),
    # The harness itself: prove it on everything.
    ('tools/regression/*', *FULL),

    # Side panel: one panel per kind of selection.
    ('BeDiagram/Components/Panels/ShapePanel.*', ['panel'], ['shape']),
    ('BeDiagram/Components/Panels/TextBoxPanel.*', ['panel'], ['text']),
    ('BeDiagram/Components/Panels/ConnectorPanel.*', ['panel'], ['connector']),
    ('BeDiagram/Components/Panels/GroupPanel.*', ['panel'], ['group']),
    ('BeDiagram/Components/Panels/SelectionPanel.*', ['panel'], ['multi']),
    ('BeDiagram/Components/Panels/DiagramPanel.*', ['panel'], ['diagram']),
    # Small components used inside the panels.
    ('BeDiagram/Components/Shared/PanelItem*', ['panel'], ['shape']),
    ('BeDiagram/Components/Shared/ShapeStyle.*', ['panel'], ['shape', 'multi']),
    ('BeDiagram/Components/Shared/TextBoxStyle.*', ['panel'], ['text', 'multi']),
    ('BeDiagram/Components/Shared/GroupStyle.*', ['panel'], ['group', 'multi']),
    ('BeDiagram/Components/Shared/AlignBar.*', ['panel'], ['group', 'multi']),
    ('BeDiagram/Components/Shared/LinkPicker.*', ['panel'], ['shape', 'group']),
    ('BeDiagram/Components/Shared/InfoTip.*', ['panel'], None),
    ('BeDiagram/Components/Shared/DropMenu.*', ['keyboard', 'gestures', 'all-shapes'], None),
    ('BeDiagram/Components/Shared/LaneCreator.*', ['keyboard', 'gestures'], None),

    # Bars round the canvas. The File menu holds Open / Save / Export, which every suite but panel uses.
    ('BeDiagram/Components/Bars/FileMenu.*', ['keyboard', 'gestures', 'all-shapes'], None),
    ('BeDiagram/Components/Bars/*', ['keyboard'], None),
    # Dialogs: Save as is opened by the keyboard and gestures suites, step order from the panel.
    ('BeDiagram/Components/Dialogs/StepOrderDialog.*', ['panel', 'keyboard'], None),
    ('BeDiagram/Components/Dialogs/*', ['keyboard', 'gestures', 'all-shapes'], None),

    # Commands: one class per shortcut. The registry (key matching) is shared, so it gets the full run.
    ('BeDiagram/Components/Pages/Commands/CommandRegistry.cs', *FULL),
    ('BeDiagram/Components/Pages/Commands/CommandButtons.cs', ['keyboard', 'panel'], None),
    ('BeDiagram/Components/Pages/Commands/*', ['keyboard'], None),

    # Styling: no suite looks at CSS, but hiding or moving a control stops the click-through suites using it.
    ('BeDiagram/wwwroot/app.css', ['keyboard', 'panel'], None),

    # Everything else (models, editor, exports, canvas layers, the page, gestures, designer.js, …): FULL.
]


def changed_files(repo, base):
    run = lambda *a: subprocess.run(['git', '-C', repo, *a], capture_output=True, text=True, check=True).stdout.split()
    return sorted(set(run('diff', '--name-only', base) + run('ls-files', '--others', '--exclude-standard')))


def main():
    repo, base = sys.argv[1], sys.argv[2]
    files = sys.argv[3:] or changed_files(repo, base)
    suites, panels, why = set(), set(), []
    all_panels = False
    for f in files:
        rule = next((r for r in RULES if fnmatch.fnmatch(f, r[0])), None)
        need, kinds = (rule[1], rule[2]) if rule else FULL
        if need == 'FULL':
            suites.update(ALL); all_panels = True
            why.append(f'{f}: full run' + ('' if rule else ' (shared code, or not in the table)'))
            continue
        suites.update(need)
        if 'panel' in need:
            if kinds is None: all_panels = True
            else: panels.update(kinds)
        why.append(f'{f}: {", ".join(need) or "nothing to run"}' + (f' (panels: {" ".join(kinds)})' if kinds else ''))
    print('\n'.join(why) or '(no changes)', file=sys.stderr)
    ordered = [s for s in ALL if s in suites]
    print(f'SUITES="{" ".join(ordered)}"')
    print(f'PANELS="{"" if all_panels or "panel" not in suites else " ".join(sorted(panels))}"')


main()
