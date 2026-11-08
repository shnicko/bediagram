"""Writes the "shapes" test diagram (every shape type with and without an icon, all outcomes, link badges, a group,
straight and elbow connectors) into each storage folder given. Usage: python3 make-shapes-diagram.py <folder>..."""
import json, sys
types = ['start','process','decision','check','action','data','status','end','circle']  # list / ipo: further down
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
# List and input–process–output nodes (rows have a fixed height, so h is worked out the way PanelGeometry does).
def stack(c): return 20 if c == 0 else c * 44 + (c - 1) * 6
def item(i, label, desc=None, icon=None, section=None):
    it = {"id": f"i_{i:08x}", "label": label}
    if desc: it["description"] = desc
    if icon: it["icon"] = icon
    if section: it["section"] = section
    return it
lst = [item(1, "Naming", "Prefix and case", "tag"), item(2, "Classification", "Uniclass present", "layers"),
       item(3, "Property sets with a very long name that is cut short"), item(4, "No description", icon="check")]
lid = node(k, 'list', 60, 1100, w=240, h=36 + 10 + stack(len(lst)) + 10, label="Model checks", link="Other diagram"); k += 1
nodes[-1]["items"] = lst
ipo = [item(5, "IFC model", "From the author", "box", "input"), item(6, "Rule template", section="input"),
       item(7, "Custom rules", section="process"), item(8, "IDS check", section="process"),
       item(9, "BCF topics", "One per finding", section="output")]
iid = node(k, 'ipo', 420, 1100, w=520, h=36 + 24 + stack(2) + 10, label="Run audit", desc="Nightly"); k += 1
nodes[-1]["items"] = ipo
sti = [item(10, "Change request", section="input"), item(11, "Approved", section="output"), item(12, "Rejected", section="output")]
sid = node(k, 'ipo', 1020, 1100, w=300, h=36 + (24 + stack(1) + 10) + (24 + stack(0) + 10) + (24 + stack(2) + 10),
           label="Review (stacked)", filled=True); k += 1
nodes[-1].update({"items": sti, "stacked": True, "sectionTitles": ["Needs", "", "Gives"]})
edge(e, lid, iid, "elbow"); edges[-1].update({"fromItem": "i_00000002", "toItem": "i_00000005"}); e += 1
edge(e, iid, hub, "straight", label="row"); edges[-1]["fromItem"] = "i_00000009"; e += 1
edge(e, hub, lid, "elbow"); e += 1
edge(e, iid, sid, "elbow"); edges[-1].update({"fromItem": "i_00000009", "toItem": "i_0000000a"}); e += 1
d = {"schema": "bediagram/1", "name": "shapes", "defaultConnector": "elbow", "groups": [g], "nodes": nodes, "edges": edges, "textBoxes": [],
     "page": {"size": "none", "orientation": "landscape", "customW": 210, "customH": 297, "marginMm": 10, "fitOnExport": True}}
for folder in sys.argv[1:]:
    json.dump(d, open(f"{folder}/shapes.diagram.json", "w"), indent=1)
print(len(nodes), "nodes", len(edges), "edges")
