"""Writes the "shapes" test diagram (every shape type with and without an icon, all outcomes, link badges, a group,
straight and elbow connectors) into each storage folder given. Usage: python3 make-shapes-diagram.py <folder>..."""
import json, sys
types = ['start','process','decision','check','action','data','status','end','circle']
outcomes = ['pass','fail','rework','conditional','hold']
nodes, edges = [], []
def node(i, t, x, y, w=None, h=None, icon=None, link=None, desc=None, outcome=None, label=None, filled=False, group=None):
    n = {"id": f"n_{i:08x}", "type": t, "label": label or f"{t.title()} step that wraps onto two lines", "x": x, "y": y,
         "groupId": group, "icon": icon, "color": None, "filled": filled, "properties": {}, "link": link, "outcome": outcome}
    if w: n["w"] = w
    if h: n["h"] = h
    if desc: n["description"] = desc
    nodes.append(n); return n["id"]
def edge(i, a, b, style="straight", label=""):
    edges.append({"id": f"e_{i:08x}", "from": a, "to": b, "label": label, "style": style, "labelOrient": "along",
                  "labelPlace": "inline", "labelPos": 0.5, "weight": "regular", "pattern": "solid", "color": None, "arrows": "end", "isManual": False})
hub = node(1, 'process', 900, 520, label="Hub")
k = 2; e = 1
for r, icon in enumerate([None, 'building']):
    for c, t in enumerate(types):
        w = 120 if t == 'circle' and r == 1 else None
        h = 90 if t == 'circle' and r == 1 else None
        nid = node(k, t, 60 + c * 200, 80 + r * 170, w=w, h=h, icon=icon, link="Other diagram",
                   desc="Who does this" if c % 3 == 0 else None, filled=(c % 4 == 1)); k += 1
        edge(e, hub, nid, "straight" if (c + r) % 2 == 0 else "elbow", label="yes" if c == 2 else ""); e += 1
for c, o in enumerate(outcomes):
    nid = node(k, 'outcome', 160 + c * 260, 900, w=90 + c * 12, h=80, outcome=o, link="Other diagram", label=o.title()); k += 1
    edge(e, hub, nid, "straight"); e += 1
    edge(e, nid, hub, "elbow"); e += 1
g = {"id": "g_00000001", "title": "Group with link", "color": "teal", "x": 1880, "y": 60, "w": 380, "h": 260, "link": "Other diagram",
     "filled": True, "border": "dashed", "headerPos": "top", "headerStyle": "band"}
inner = node(k, 'decision', 1960, 140, group="g_00000001", label="Inside"); k += 1
edge(e, hub, "g_00000001", "straight"); e += 1
edge(e, hub, inner, "straight"); e += 1
d = {"schema": "bediagram/1", "name": "shapes", "defaultConnector": "elbow", "groups": [g], "nodes": nodes, "edges": edges, "textBoxes": [],
     "page": {"size": "none", "orientation": "landscape", "customW": 210, "customH": 297, "marginMm": 10, "fitOnExport": True}}
for folder in sys.argv[1:]:
    json.dump(d, open(f"{folder}/shapes.diagram.json", "w"), indent=1)
print(len(nodes), "nodes", len(edges), "edges")
