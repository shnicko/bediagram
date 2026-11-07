"""Compares what the two builds produced (base = before the change, head = after) and prints one line per suite.
Usage: python3 compare.py <work folder>   (the folder run.sh wrote into)

What counts as "the same":
- diagrams (.json): equal after replacing random ids (n_1a2b3c4d…) with #0, #1, … in order of appearance
- SVG / Mermaid / PDF: equal text after the same id replacement; PDFs ignore their /CreationDate timestamp
- PNG: every pixel equal on all four channels (red, green, blue, alpha)
- step logs (keyboard / panel): every recorded field equal, after replacing each build's storage folder name
Toasts disappear after 3.5 s, so a toast can vanish one step earlier on one build. If that's the only difference and
the step moves when you run again, it's timing, not a change in behaviour.
"""
import json, os, re, sys
from PIL import Image, ImageChops

work = sys.argv[1]


def norm(data: bytes) -> str:
    s = re.sub(r'/CreationDate \(D:\d+Z\)', '', data.decode('latin-1'))
    ids = {}
    return re.sub(r'\b[netg]_[0-9a-f]{8}\b', lambda m: ids.setdefault(m.group(0), f'#{len(ids)}'), s)


def png_same(a: str, b: str) -> bool:
    x, y = Image.open(a).convert('RGBA'), Image.open(b).convert('RGBA')
    # getbbox() on an RGBA image only looks at alpha, so check each channel on its own
    return x.size == y.size and all(band.getbbox() is None for band in ImageChops.difference(x, y).split())


def compare_folder(name: str, skip=lambda f: False) -> int:
    base, head = f'{work}/base-out/{name}', f'{work}/head-out/{name}'
    files = sorted(f for f in os.listdir(head) if not skip(f))
    bad = []
    for f in files:
        a, b = f'{base}/{f}', f'{head}/{f}'
        if not os.path.exists(a):
            bad.append(f + ' (missing in base)'); continue
        if f.endswith('.png'):
            same = png_same(a, b)
        elif f.endswith('.json'):
            x, y = json.loads(norm(open(a, 'rb').read())), json.loads(norm(open(b, 'rb').read()))
            x.pop('modified', None); y.pop('modified', None)
            same = x == y
        else:
            same = norm(open(a, 'rb').read()) == norm(open(b, 'rb').read())
        if not same: bad.append(f)
    print(f'{name:18} {len(files):4} files  {"SAME" if not bad else f"{len(bad)} DIFFERENT"}')
    for f in bad: print(f'                   differs: {f}')
    return len(bad)


def compare_log(name: str, by_label: bool = False) -> int:
    def load(side):
        text = open(f'{work}/{side}-out/{name}.json').read().replace(f'{work}/{side}-store', '<storage>')
        return json.loads(text)
    a, b = load('base'), load('head')

    if by_label:
        # Panel steps are named after their control: match by name, so a control added or removed is one line,
        # not a shift of every step after it.
        bi = {x['label']: i for i, x in enumerate(b)}
        ai = {x['label']: i for i, x in enumerate(a)}
        pairs = [(i, bi[x['label']]) for i, x in enumerate(a) if x['label'] in bi]
        only = [('only in base: ' + x['label'], 'step') for x in a if x['label'] not in bi] + \
               [('only in head: ' + y['label'], 'step') for y in b if y['label'] not in ai]
    else:
        pairs = [(i, i) for i in range(min(len(a), len(b)))]
        only = [('(number of steps)', f'{len(a)} vs {len(b)}')] if len(a) != len(b) else []

    def toast_expired_early(i: int, j: int) -> bool:
        # One build still shows the toast from the step before, the other has already cleared it: timing, not behaviour.
        x, y = a[i].get('toast'), b[j].get('toast')
        if x is not None and y is not None: return False
        shown, side, k = (x, a, i) if x is not None else (y, b, j)
        return k > 0 and side[k - 1].get('toast') == shown

    diffs, timing = [], []
    for i, j in pairs:
        x, y = a[i], b[j]
        for k in x:
            if x.get(k) == y.get(k): continue
            (timing if k == 'toast' and toast_expired_early(i, j) else diffs).append((x['label'], k))
    diffs += only
    print(f'{name:18} {len(a):4} steps  {"SAME" if not diffs else f"{len(diffs)} DIFFERENT"}'
          + (f'  (ignored {len(timing)} toast that expired a step apart — timing)' if timing else ''))
    for label, field in diffs[:10]: print(f'                   differs: {label} → {field}')
    return len(diffs)


total = 0
ran = lambda p: os.path.exists(f'{work}/head-out/{p}')   # suites skipped with SUITES=… have no output
if ran('gestures'): total += compare_folder('gestures', skip=lambda f: f.endswith('.png') and '-gest.' not in f)  # screenshots show the folder
if ran('keyboard.json'): total += compare_log('keyboard')
if ran('all-shapes'): total += compare_folder('all-shapes', skip=lambda f: 'screen' in f)
if ran('panel.json'): total += compare_log('panel', by_label=True)
if not any(ran(p) for p in ('gestures', 'keyboard.json', 'all-shapes', 'panel.json')):
    print('No suite output to compare'); sys.exit(1)
print('ALL THE SAME' if total == 0 else f'{total} difference(s) — look at the lines above')
sys.exit(0 if total == 0 else 1)
