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

Each diagram can produce these files, side by side:

| File | What |
|---|---|
| `Name.diagram.json` | The diagram (source of truth) |
| `Name.svg` | Export → SVG image — for reports, Word, PowerPoint (page-sized in mm when a page is set) |
| `Name.png` | Export → PNG image — the page or drawing extents as pixels, at the resolution chosen under *PNG settings* |
| `Name.pdf` | Export → PDF — the whole page at the selected paper size, or (open canvas) the drawing zoomed to its extents |
| `Name.view.svg` / `.view.png` / `.view.pdf` | Export → *… — current view*: exactly what's on screen (fitted inside the page margins when a page size is set) |
| `Name.mmd` | Export → Mermaid — paste into Mermaid-aware tools (Markdown, wikis) |

**Export areas:** SVG, PNG and PDF each have two entries — the *page* (when a page size is set) or *drawing extents* (open canvas), and the *current view*. Mermaid describes the structure, so it has one.

**PNG settings** (at the bottom of the Export menu, kept for the session):
- *Resolution* — on an open canvas 1× (screen size), 2× or 3×; with a page size 96, 150 or 300 dpi (A4 at 300 dpi = 3508 × 2480 px). The dpi is written into the file, so a page PNG inserts into Word/PowerPoint at its paper size. Very large images (e.g. A0 at 300 dpi) are reduced to what the browser can draw, and the message says so.
- *Transparent background* — leaves out the white background, for placing over slides or coloured pages.

The PNG is drawn by your browser from the SVG export, so it matches the SVG and the screen exactly.

## Using it

- **Add shapes:** drag from the sidebar, click a sidebar item, or press `1`–`9`.
- **Auto-join:** with *Auto-join* on, a new shape (sidebar click or drag, number key, `T`) is connected from the currently selected item. Number keys and sidebar clicks place the new step below the selection (side by side for a second branch) — joined when *Auto-join* is on, unjoined when it's off. The panel's *+ Next step* button always joins, and dropping a port drag on empty canvas always creates a joined step.
- **Quick join:** select items (Shift+click or drag a box around them) and press `J` — they are joined in the order you selected them.
- **Connect by dragging:** hover a shape, drag from one of its ● ports onto another shape. Drop on empty canvas to create a new step there, already joined.
- **Text boxes:** drag *Text box* from the sidebar, click it, or press `T` (joined to the selection when *Auto-join* is on). Multi-line — `Enter` adds a new line while editing; click away or `Esc` to finish. In the panel: font (*Sans* / *Serif* / *Mono*), size, bold, left / centre / right and top / middle / bottom alignment, text colour, background (none, white, sticky-note colours or the shape palette), border (none, solid, dashed, dotted, dash-dot, long dash) with weight and colour, rounded corners, and *Fit height to text*. Text boxes have ● ports and connect like shapes (to shapes, groups or other text boxes); number keys add the next step from a selected text box. Resize from the corner handle. A borderless box shows a faint dashed outline on hover so you can find it.
- **Comments:** a comment is a text box with the *Comment* flag. Click *Comment* in the sidebar, press `C` (pins it to the selected item), or drop it on a shape. It gets the standard comment look — yellow sticky note, thin border, folded corner, Consolas — and its lines are dashed pins with no arrowhead (restyle them like any connector). Tick or untick *Comment* in the panel to flag any text box; *Reset to comment style* restores the look. Comments can't be pinned to each other.
- **Groups:** select items and press `Ctrl+G` (or click/drag *Group* from the sidebar for an empty one). Name it by double-clicking the title bar, pick a colour in the panel. Drag a group by its title bar — its contents move with it. Drag a loose item into a group to add it; drag a member into another group to move it there. Dragging a member outside its group keeps it in the group and the group grows to fit (the same as nudging with the arrow keys) — use *remove from group* in the panel or *Ungroup* to take items out. Click a group's body to select it; drag inside it to box-select its items.
  - **Group look:** in the panel — *Tinted background* on/off, border pattern (solid, dashed, dotted, dash-dot, long dash), and title *Top* / *Bottom* as a *Band* (tinted strip inside the group) or *Plain text* (bold title; at the bottom it sits centred under the box). Drag a group by its title wherever it is. Exports (SVG / PDF) match; Mermaid keeps no-fill and dashed outlines.
  - **Align within the group:** left / centre / right, top / middle / bottom, distribute, *Arrange in a row / column*, *Fit to contents*.
- **Align:** select two or more items and use the align buttons in the panel. Alignment lines up the **shape outlines** — a label drawn under a circle or outcome is ignored, so a circle and a box aligned to the bottom share the same bottom edge. *Distribute* and *Column* still leave room for those labels so they don't run into the next item, and aligning inside a group keeps the labels inside it.
  - **Attach groups:** groups have ● ports like shapes, so connect group → shape, shape → group, or group → group (or select both and press `J`).
  - *Ungroup* (`Ctrl+Shift+G`) keeps the items; *Delete* removes the group and its contents.
- **Description:** every shape has an optional *Description* in the panel — smaller grey text under the label, for a short note on who does the step and how. On circles and outcomes it sits under the label; on boxes and other shapes (label inside) it sits under the shape. *Text width* sets how wide that text wraps (blank = automatic, a little wider than the shape) — it also sets the wrap width of a label drawn under a circle. Descriptions are included in SVG / PDF export, and in Mermaid export as a second line of the label.
- **Circle with icon (presentation style):** press `9` or drag *Circle*. Pick an icon from the built-in set (118 icons, searchable), a colour, and *Solid fill* for a bold look; the label sits under the circle. Any other shape can carry an icon too (shown beside its label), and every shape can take a colour.
- **Outcomes (pass / fail / rework / conditional / hold):** the *Outcomes* section of the sidebar adds review results that differ by **shape**, not just colour, so an A3 sheet printed in greyscale or read by a colour-blind reviewer still reads correctly:
  - *Pass* — scalloped seal with a tick (green)
  - *Fail* — octagon (stop sign) with an ✕ (red): rejected, the flow stops
  - *Rework* — circle with an arrow looping back (pink): returned to the author
  - *Conditional* — triangle with `!` (amber, dark mark): pass with comments / approved subject to changes
  - *Hold* — rounded square with a clock (slate): on hold / awaiting review

  Click or drag one in; switch between them with the *Outcome* buttons in the panel (a label still showing the old name follows). Solid fill is on by default; untick *Solid fill* for an outline look, or pick any colour — the outline and symbol stay. Labels sit under the symbol. Straight connectors clip to the real outline (seal, octagon, triangle).
- **Linked diagrams (drill-down):** any shape or group can point to another saved diagram that holds its more detailed flow. Select it and pick the diagram under *Linked diagram* in the panel (save the detailed flow as its own diagram first). A small 🔗 badge then appears at the bottom-right — on the outline for circles, outcomes and diamonds, at the corner for boxes, inside the bottom-right corner for groups. Click the badge (or *Open* in the panel) to open the linked diagram; if there are unsaved changes you're asked to *Save and open*, *Discard and open* or cancel. The toolbar then shows a breadcrumb (`Overview › Detail A › …`) with *← Back*; click any name to jump back up. Opening a diagram from *Open* or starting a *New* one clears the breadcrumb. A link whose diagram has been renamed or deleted shows a faded, dashed badge and a warning in the panel. The badge is also drawn in SVG and PDF exports; Mermaid export keeps links as `%% N1 links to diagram "…"` comments.
- **Page size (for documents):** the *Page* box in the toolbar switches between the open canvas (default) and a fixed sheet — A5–A0, US Letter, Legal, Tabloid, 16:9 and 4:3 slides, or a custom size in mm — with portrait/landscape buttons. The sheet shows on the canvas with dashed margin guides; the status bar warns if anything is off the page. In the Diagram panel (click empty canvas): margin, *Centre drawing on page*, and *Scale down to fit the margins on export*. **Export → SVG image** then produces a file exactly that paper size in mm, so every diagram drops into Word/PowerPoint at a consistent size. **Export → PDF — page** does the same as a PDF. Switching back to the open canvas leaves the drawing untouched.
- **Connector lines:** *Elbow* (right-angled) or *Straight* (direct, clipped neatly to each shape's outline — circles, diamonds, hexagons included). The *Lines* switch in the toolbar sets the style for new connectors; change any single connector in its panel or press `L` with it selected. With several items selected you can switch all connectors between them at once; *Apply style to all* copies a connector's look to every connector.
- **Connector ends and bends:** select a connector to show its handles. Drag a ● end to another side of its shape — it snaps to the nearest side middle (top / right / bottom / left), and the side points light up while you drag. On elbow connectors, drag the ■ on the middle segment sideways to move the bend (snaps to the grid; double-click it to put it back in the middle). Once you've adjusted a connector both ends stay on the sides you chose and the bend keeps its offset when you move shapes; *Reset route to auto* in the panel hands control back. L-shaped connectors (a single corner) have no bend to drag — move an end to another side instead.
- **Connector line types:** in a connector's panel choose *Weight* (light / regular / heavy), *Line type* (solid, dashed, dotted, dash-dot, long dash), *Colour*, and *Arrowheads* (end / both / none). *Use as default* makes new connectors look the same; *Apply to all* restyles every connector. Mermaid export maps broken lines to dotted (`-.->`) and heavy solid lines to thick (`==>`).
- **Connector labels:** double-click a line (or select it and press `Enter`, or type in the panel). Up to two lines — `Shift+Enter` starts the second line (`Enter` finishes); anything longer belongs in a text box. Text direction: *Along line* (the default for new connectors), *Level* (always horizontal), or *90° to line* — text is never upside down. *Label placement*: *Inline* sits on the line (a two-line label straddles it — line one above, line two below, with the connector running between them), *Above* or *Below* sets the text beside the line, as the text reads. Labels beside or straddling the line have no box, just a thin white halo so they stay readable; single-line inline labels keep their white box. Where the text runs across the line (level text on an up-down segment, or 90° labels), *Inline* is boxed on the line and *Above* / *Below* put it to the left / right. Slide the label anywhere along the connector (or use *Near start / Middle / Near end*). Labels always sit on top of shapes; Mermaid export writes the two lines with `<br/>`.
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
      "description": "Every fire door must carry a rating.", "textWidth": 220,
      "x": 375, "y": 120, "w": 170, "h": 64,
      "properties": { "ruleId": "R-001", "ifcEntity": "IfcDoor", "property": "Pset_DoorCommon.FireRating", "operator": "exists", "value": "" } }
  ],
  "edges":    [ { "id": "e_...", "from": "n_...", "to": "n_...", "label": "Pass",
                  "style": "elbow", "labelOrient": "along", "labelPlace": "inline", "labelPos": 0.5,
                  "fromSide": "right", "toSide": "top", "bend": 40,
                  "weight": "heavy", "pattern": "dashed", "color": "red", "arrows": "end" } ],
  "textBoxes": [ { "id": "t_...", "text": "Line one\nLine two", "isComment": true,
                   "x": 0, "y": 0, "w": 200, "h": 110, "groupId": null,
                   "fill": "yellow", "border": "solid", "borderWeight": "light", "borderColor": "yellow", "rounded": true,
                   "font": "mono", "fontSize": 12, "bold": false, "textColor": "ink", "align": "left", "vAlign": "top" } ],
  "groups":   [ { "id": "g_...", "title": "Audit", "color": "teal", "x": 0, "y": 0, "w": 360, "h": 220, "link": "Audit detail",
                  "filled": false, "border": "dashed", "headerPos": "bottom", "headerStyle": "text" } ],
  "page":     { "size": "A4", "orientation": "landscape", "marginMm": 10, "fitOnExport": true, "customW": 210, "customH": 297 }
}
```

Connectors: `label` holds at most two lines separated by `\n`; `labelPlace` is `inline`, `above` or `below`; `labelOrient` is `along`, `horizontal` or `perpendicular`. `fromSide` / `toSide` (`top`, `right`, `bottom`, `left`) pin an end to that side's middle and are left out when automatic; `bend` is how far (px) the middle segment of an elbow connector has been moved from its automatic spot — along x for an up-down segment, along y for a left-right one — and is left out when 0. Nodes and text boxes carry an optional `groupId`; nodes also have optional `icon`, `color` and `filled`, plus `description` (smaller text under the label) and `textWidth` (wrap width in px for text drawn under the shape; left out = automatic). Nodes and groups have an optional `link`: the name of another diagram in the same folder (file name without `.diagram.json`) that holds the more detailed flow — left out when there is none. Groups: `filled` (tinted background or none), `border` (a line pattern: `solid`, `dashed`, `dotted`, `dashdot`, `longdash`), `headerPos` (`top` / `bottom`) and `headerStyle` (`band` = title on a tinted strip inside the group, `text` = plain bold title — inside at the top, or centred under the box when at the bottom). Outcome nodes have `"type": "outcome"` and `"outcome": "pass" | "fail" | "rework" | "conditional" | "hold"` — the meaning lives in that field, the seal/octagon/triangle is only how it is drawn, so the rule compiler never has to guess from a picture (`outcome` is left out on other node types). Text boxes: `fill`, `borderColor` and `textColor` are colour keys (`white`, `grey`, `ink`, `yellow`, `sky`, `mint`, `rose`, or a palette key such as `blue`) or `null` for the default; `border` is `none` or a line pattern; `font` is `sans`, `serif` or `mono`. `isComment` marks annotations, so code reading the file can skip them or treat them as notes. Older files with a `comments` list still open — those become comment text boxes and are saved as `textBoxes`.

Mermaid export writes text boxes (comments included) as nodes with a `style` line for their background, border and text colour. Mermaid has no seal or octagon, so outcomes export as the nearest classic shape with a symbol in the label and their fill colour: pass `((✓ …))`, fail `{{✕ …}}`, rework `((↺ …))`, conditional `[/⚠ …\]`, hold `(⏸ …)`.

Node types: `start`, `process`, `decision`, `check`, `action`, `data`, `status`, `end`, `circle`, `outcome`. Typed nodes + properties mean the audit tool can later read these files and turn a diagram into rules without a format change.

## Project layout

```
BeDiagram.sln                solution (open this in Visual Studio)
BeDiagram/                   the web app project
  Models/                      the diagram model: Diagram, Node, Edge, TextBox, Group, PageSetup, IBox, IGroupable
  Models/Geometry.cs           shape paths, elbow connectors, text wrap (shared by canvas + export)
  Models/Icons.cs              built-in icon set (Lucide, ISC licence — see LICENSE-lucide.txt)
  Models/Catalogue/            type tables: NodeTypes, Outcomes, EdgeStyles, Sides, LabelPlaces, LabelOrients
  Models/Styles/               looks and colours: Palette, Swatch, TextPalette, LineLook, TextLook, GroupLook, NodeStyle, TextStyle
  Models/Layout/               geometry results shared by canvas + export: EdgeRoute, EdgeGeom, LabelLayout, Rect, …
  Services/DiagramStore.cs     local folder save / open / list / delete
  Services/Exporters.cs        SVG and Mermaid export (SVG optionally of the current view)
  Services/PdfExporter.cs      vector PDF made from the exported SVG (no third-party libraries)
  Services/Export/             export strategies (see below)
  Components/Pages/Designer.razor(.cs)   the editor UI and interaction logic
  Components/Pages/Gestures/   mouse gestures (State pattern, see below)
  Components/Pages/Views/      view records the Designer markup reads (NodeView, EdgeView, GroupView, …)
  wwwroot/js/designer.js       keyboard shortcuts, wheel zoom, canvas size, focus
  wwwroot/js/export.js         draws the SVG export onto a canvas for PNG export
  wwwroot/app.css              styling
tools/Check-OneTypePerFile.ps1   checks the one-type-per-file rule (see below)
```

**One type per file.** Every class, record, interface and enum is in its own `.cs` file named after it (`MoveGesture` → `MoveGesture.cs`). Partial classes use `Type.Area.cs` (`Designer.EditorContext.cs`). Private nested helpers may stay nested; public nested types get their own file. Folders match namespaces (`Models/Styles/` = `BeDiagram.Models.Styles`). Check it with:

```
powershell -ExecutionPolicy Bypass -File tools\Check-OneTypePerFile.ps1
```

### Export: strategy pattern

Export is split into two families of strategies, put together by one context class:

- **`IExportFormat`** — *what kind of file*: `SvgExportFormat`, `PngExportFormat`, `PdfExportFormat`, `MermaidExportFormat`. Each turns a diagram (plus an optional view window) into bytes.
- **`IExportArea`** — *which part of the drawing*: `WholeDrawingArea` (page or extents) and `CurrentViewArea` (what's on screen, saved with a `.view` suffix).
- **`ExportService`** — the context. It gets every registered format and area from dependency injection, lists the combinations for the Export menu (`Choices()`), and runs one (`ExportAsync`: area → view window, format → bytes, then save next to the diagram).

`PngExportFormat` uses one more small strategy, `IPngRasterizer`, for turning SVG into pixels; `BrowserPngRasterizer` does it in the browser (`wwwroot/js/export.js`). A server-side rasteriser could be swapped in without touching the format.

To add an export format or area: write a class implementing the interface and register it in `Program.cs` — the menu picks it up; the Designer doesn't change. Menu order = registration order.

### Mouse gestures: State pattern

Each drag on the canvas is a state object implementing **`IGesture`** (`Move`, `Up`, `Cancel`). The Designer holds the gesture in progress (`gesture`, null when idle): a pointer-down handler creates the right one, and every pointer move and the pointer-up are handed to it.

| Gesture | Started by |
|---|---|
| `MoveGesture` | dragging a shape, text box, group title or the selection |
| `ResizeGesture` | the bottom-right handle of a text box or group |
| `ConnectGesture` | dragging from a port (drop on an item to join, on empty canvas to add a joined step) |
| `MarqueeGesture` | dragging on empty canvas or a group's body (a click on a group's body selects it) |
| `PanGesture` | right- or middle-button drag |
| `EdgeEndGesture` | a ● end of the selected connector |
| `BendGesture` | the ■ bend handle of the selected connector |

Shared behaviour sits in two base classes. `DragGesture` keeps the start point and the current point. `UndoableDragGesture` takes an undo snapshot at the start and records it on pointer-up only if something changed, so a plain click adds no undo step. Gestures reach the editor only through **`IEditorContext`** (diagram, selection, lookups, snapping, undo), which the Designer implements in `Designer.EditorContext.cs`.

To add a gesture: write a class (usually derived from `UndoableDragGesture`) and create it from a pointer-down handler. The other gestures don't change.

To add a shape type: add an entry to `NodeTypes.All` (and optionally default properties in `DefaultProps` in `Designer.razor.cs`).

## Known limits / next steps

- Connectors (elbow or straight) don't route around other shapes yet.
- No auto-layout ("tidy up") yet — candidate: MSAGL.
- Groups can't be nested inside other groups yet.
- No Mermaid import yet.
- PDF text uses the standard PDF fonts (Helvetica / Times / Courier), so text widths differ slightly from Segoe UI on screen; characters outside Windows-1252 print as `?`.
- One page per diagram (no multi-page documents).
- Linked diagrams open in place of the current one; showing a linked diagram on the same canvas (each sheet as a sized diagram object) is planned for later. Links are by name, so renaming a diagram with *Save as…* doesn't update links that point to it.
- Single user, local only, by design.

## Credits

Icons: [Lucide](https://lucide.dev), ISC licence (`LICENSE-lucide.txt`).
