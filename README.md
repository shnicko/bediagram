# BeDiagram

A local, self-hosted diagramming tool built with .NET 10 Blazor (interactive server). Diagrams are saved as JSON files on your own machine — no subscription, no cloud, no impact on corporate software.

## Run it

**Visual Studio:** open `BeDiagram.sln` and press F5 (the `http` profile opens http://localhost:5070).

**Command line:**

```
cd BeDiagram
dotnet run
```

Then open http://localhost:5070. Requires the .NET 10 SDK.

## Where files go

By default: `Documents\BeDiagram\` (Windows) or `~/Documents/BeDiagram/` (macOS/Linux).
Change it in `appsettings.json`:

```json
"Storage": { "Folder": "D:\\Projects\\Diagrams" }
```

Each diagram can produce three files, side by side:

| File | What |
|---|---|
| `Name.diagram.json` | The diagram (source of truth) |
| `Name.svg` | Export → SVG image — for reports, Word, PowerPoint (page-sized in mm when a page is set) |
| `Name.mmd` | Export → Mermaid — paste into Mermaid-aware tools (Markdown, wikis) |

## Using it

- **Add shapes:** drag from the sidebar, click a sidebar item, or press `1`–`9`.
- **Auto-join:** with *Auto-join* on, a new shape is connected from the currently selected shape. Number keys always join to the selection and place the new step below it (side by side for a second branch).
- **Quick join:** select items (Shift+click or drag a box around them) and press `J` — they are joined in the order you selected them.
- **Connect by dragging:** hover a shape, drag from one of its ● ports onto another shape. Drop on empty canvas to create a new step there, already joined.
- **Comments:** drag *Comment* from the sidebar (drop it on a shape to pin it), or select a shape and press `C`. Pinned comments show a dashed leader line. Resize from the corner handle; pick a colour in the panel.
- **Groups:** select items and press `Ctrl+G` (or click/drag *Group* from the sidebar for an empty one). Name it by double-clicking the title bar, pick a colour in the panel. Drag a group by its title bar — its contents move with it. Drag items in or out to change membership; groups grow to fit. Click a group's body to select it; drag inside it to box-select its items.
  - **Align within the group:** left / centre / right, top / middle / bottom, distribute, *Arrange in a row / column*, *Fit to contents*.
  - **Attach groups:** groups have ● ports like shapes, so connect group → shape, shape → group, or group → group (or select both and press `J`).
  - *Ungroup* (`Ctrl+Shift+G`) keeps the items; *Delete* removes the group and its contents.
- **Circle with icon (presentation style):** press `9` or drag *Circle*. Pick an icon from the built-in set (118 icons, searchable), a colour, and *Solid fill* for a bold look; the label sits under the circle. Any other shape can carry an icon too (shown beside its label), and every shape can take a colour.
- **Page size (for documents):** the *Page* box in the toolbar switches between the open canvas (default) and a fixed sheet — A5–A0, US Letter, Legal, Tabloid, 16:9 and 4:3 slides, or a custom size in mm — with portrait/landscape buttons. The sheet shows on the canvas with dashed margin guides; the status bar warns if anything is off the page. In the Diagram panel (click empty canvas): margin, *Centre drawing on page*, and *Scale down to fit the margins on export*. **Export → SVG image** then produces a file exactly that paper size in mm, so every diagram drops into Word/PowerPoint at a consistent size. Switching back to the open canvas leaves the drawing untouched.
- **Connector lines:** *Elbow* (right-angled) or *Straight* (direct, clipped neatly to each shape's outline — circles, diamonds, hexagons included). The *Lines* switch in the toolbar sets the style for new connectors; change any single connector in its panel or press `L` with it selected. With several items selected you can switch all connectors between them at once; *Apply style to all* copies a connector's look to every connector.
- **Connector line types:** in a connector's panel choose *Weight* (light / regular / heavy), *Line type* (solid, dashed, dotted, dash-dot, long dash), *Colour*, and *Arrowheads* (end / both / none). *Use as default* makes new connectors look the same; *Apply to all* restyles every connector. Mermaid export maps broken lines to dotted (`-.->`) and heavy solid lines to thick (`==>`).
- **Connector labels:** double-click a line (or select it and press `Enter`, or type in the panel). Text direction: *Level* (always horizontal), *Along line*, or *90° to line* — text is never upside down. Slide the label anywhere along the connector (or use *Near start / Middle / Near end*). Labels always sit on top of shapes.
- **Edit text:** double-click, or select and press `Enter`. Connector labels have quick chips (Yes / No / Pass / Fail).
- **Properties panel:** type, size, and free-form key/value properties per shape. Rule-check shapes start with `ruleId`, `ifcEntity`, `property`, `operator`, `value` — groundwork for the future rule builder.
- **Navigate:** mouse wheel pans, Ctrl+wheel zooms, right- or middle-drag pans, `F` fits.
- **Other:** `Del` delete, arrows nudge (Shift = 1px), `Ctrl+Z`/`Ctrl+Y` undo/redo, `Ctrl+D` duplicate, `Ctrl+A` select all, `Ctrl+S` save.

## File format (bediagram/1)

```json
{
  "schema": "bediagram/1",
  "name": "IFC audit flow",
  "nodes": [
    { "id": "n_1a2b3c4d", "type": "check", "label": "Fire rating present",
      "x": 375, "y": 120, "w": 170, "h": 64,
      "properties": { "ruleId": "R-001", "ifcEntity": "IfcDoor", "property": "Pset_DoorCommon.FireRating", "operator": "exists", "value": "" } }
  ],
  "edges":    [ { "id": "e_...", "from": "n_...", "to": "n_...", "label": "Pass",
                  "style": "elbow", "labelOrient": "along", "labelPos": 0.5,
                  "weight": "heavy", "pattern": "dashed", "color": "red", "arrows": "end" } ],
  "comments": [ { "id": "c_...", "text": "...", "color": "yellow", "x": 0, "y": 0, "w": 200, "h": 110, "groupId": null } ],
  "groups":   [ { "id": "g_...", "title": "Audit", "color": "teal", "x": 0, "y": 0, "w": 360, "h": 220 } ],
  "page":     { "size": "A4", "orientation": "landscape", "marginMm": 10, "fitOnExport": true, "customW": 210, "customH": 297 }
}
```

Nodes and comments carry an optional `groupId`; nodes also have optional `icon`, `color` and `filled`. Files saved by the first version open unchanged.

Node types: `start`, `process`, `decision`, `check`, `action`, `data`, `status`, `end`, `circle`. Typed nodes + properties mean the audit tool can later read these files and turn a diagram into rules without a format change.

## Project layout

```
BeDiagram.sln                solution (open this in Visual Studio)
BeDiagram/                   the web app project
  Models/Diagram.cs            data model + node type catalogue (shapes, colours, sizes)
  Models/Icons.cs              built-in icon set (Lucide, ISC licence — see LICENSE-lucide.txt)
  Models/Geometry.cs           shape paths, elbow connectors, text wrap (shared by canvas + export)
  Services/DiagramStore.cs     local folder save / open / list / delete
  Services/Exporters.cs        SVG and Mermaid export
  Components/Pages/Designer.razor(.cs)   the editor UI and interaction logic
  wwwroot/js/designer.js       keyboard shortcuts, wheel zoom, canvas size, focus
  wwwroot/app.css              styling
```

To add a shape type: add an entry to `NodeTypes.All` (and optionally default properties in `DefaultProps` in `Designer.razor.cs`).

## Known limits / next steps

- Connectors (elbow or straight) don't route around other shapes yet.
- No auto-layout ("tidy up") yet — candidate: MSAGL.
- Groups can't be nested inside other groups yet.
- No Mermaid import or PNG/PDF export yet (open the SVG in a browser and print to PDF — with a page size set it prints at the right size).
- One page per diagram (no multi-page documents).
- Single user, local only, by design.

## Credits

Icons: [Lucide](https://lucide.dev), ISC licence (`LICENSE-lucide.txt`).
