"""Writes the "shapes" test diagram (every shape type with and without an icon, all outcomes, link badges, a group,
straight and elbow connectors, lists and IPO boxes, classes and entities in a package) into each storage folder given. Usage: python3 make-shapes-diagram.py <folder>..."""
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
# The classic shapes drawn as circles ("round": true keeps the type), with their circle icons; one solid, one with no icon.
round_icons = {'start': 'play', 'process': 'settings', 'decision': 'git-branch', 'check': 'shield-check', 'action': 'zap',
               'data': 'database', 'status': 'activity', 'end': 'flag'}
for c, t in enumerate(types[:8]):
    nid = node(k, t, 60 + c * 160, 1450, w=84, h=84, icon=None if c == 7 else round_icons[t], label=f"{t.title()} circle",
               filled=(c == 2)); k += 1
    nodes[-1]["round"] = True
    if c > 0: edge(e, f"n_{k - 2:08x}", nid, "elbow"); e += 1
# Data models: class (abstract), interface, enumeration, name-only class, entities with keys (one solid), in a package
# (a group with the name tag). Compartment heights: name 36 (50 with a stereotype line), then 5 + 20 per row + 5 per
# compartment (12 when empty).
def comp(c): return 12 if c == 0 else 5 + c * 20 + 5
def member(i, label, section, type=None, vis=None, mult=None, pk=False, fk=False):
    it = {"id": f"i_{i:08x}", "label": label, "section": section}
    if type: it["dataType"] = type
    if vis: it["visibility"] = vis
    if mult: it["multiplicity"] = mult
    if pk: it["primaryKey"] = True
    if fk: it["foreignKey"] = True
    return it
pkg = {"id": "g_00000002", "title": "Audit model", "color": "grey", "x": 40, "y": 1620, "w": 1300, "h": 460,
       "filled": False, "border": "solid", "headerPos": "top", "headerStyle": "tag"}
cls = [member(20, "id", "attribute", "int", "private"), member(21, "rules", "attribute", "Rule", "protected", "1..*"),
       member(22, "run(model: IfcModel)", "operation", "Report", "public"), member(23, "a very long operation name that is cut short()", "operation", None, "package")]
cid = node(k, 'class', 80, 1680, w=260, h=50 + comp(2) + comp(2), label="AuditJob", group="g_00000002"); k += 1
nodes[-1].update({"items": cls, "abstract": True})
ifc = node(k, 'interface', 400, 1680, w=220, h=50 + comp(1), label="IRuleSource", group="g_00000002"); k += 1
nodes[-1]["items"] = [member(24, "load()", "operation", "Rule[]", "public")]
enm = node(k, 'enumeration', 660, 1680, w=180, h=50 + comp(3), label="Outcome", group="g_00000002"); k += 1
nodes[-1]["items"] = [member(25, "PASS", "attribute"), member(26, "FAIL", "attribute"), member(27, "HOLD", "attribute")]
nmo = node(k, 'class', 880, 1680, w=180, h=36, label="Finding (name only)", group="g_00000002"); k += 1
nodes[-1].update({"items": [member(28, "id", "attribute", "int")], "nameOnly": True})
e1 = node(k, 'entity', 80, 1880, w=240, h=36 + comp(3), label="rule", group="g_00000002"); k += 1
nodes[-1]["items"] = [member(29, "rule_id", "attribute", "int", pk=True), member(30, "template_id", "attribute", "int", fk=True),
                      member(31, "ifc_entity", "attribute", "varchar(64)")]
e2 = node(k, 'entity', 460, 1880, w=240, h=36 + comp(2), label="template", filled=True, group="g_00000002"); k += 1
nodes[-1]["items"] = [member(32, "template_id", "attribute", "int", pk=True, fk=True), member(33, "name", "attribute", "varchar(100)")]
edge(e, e1, e2, "elbow"); edges[-1].update({"fromItem": "i_0000001e", "toItem": "i_00000020"}); e += 1
edge(e, cid, ifc, "straight"); e += 1
# Connector ends (UML and crow's foot): saved as fromHead / toHead with "arrows": "none".
edges[-2].update({"fromHead": "zeroOrMany", "toHead": "onlyOne", "arrows": "none"})
edges[-1].update({"fromHead": "none", "toHead": "triangle", "arrows": "none", "pattern": "dashed"})
edge(e, cid, enm, "elbow"); edges[-1].update({"fromHead": "filledDiamond", "toHead": "open", "arrows": "none", "weight": "heavy"}); e += 1
edge(e, ifc, nmo, "straight"); edges[-1].update({"fromHead": "diamond", "toHead": "oneOrMany", "arrows": "none", "weight": "light", "color": "red"}); e += 1
edge(e, e2, nmo, "elbow"); edges[-1].update({"fromHead": "one", "toHead": "zeroOrOne", "arrows": "none"}); e += 1
edge(e, nmo, e1, "straight"); edges[-1].update({"fromHead": "many", "toHead": "arrow", "arrows": "none"}); e += 1
d = {"schema": "bediagram/1", "name": "shapes", "defaultConnector": "elbow", "groups": [g, pkg], "nodes": nodes, "edges": edges, "textBoxes": [],
     "page": {"size": "none", "orientation": "landscape", "customW": 210, "customH": 297, "marginMm": 10, "fitOnExport": True}}
for folder in sys.argv[1:]:
    json.dump(d, open(f"{folder}/shapes.diagram.json", "w"), indent=1)
print(len(nodes), "nodes", len(edges), "edges")
