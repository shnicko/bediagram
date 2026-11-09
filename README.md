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

- **Menu bar:** *File* (New, Open, Save, Save as, every export with the PNG settings, the flow direction and the page size), *Edit* (Undo / Redo, Select all, Duplicate, Delete, Join, Group / Ungroup, plus *Auto-join*, *Snap to grid* and the angle lock), *Arrange* (Step order, Tidy connectors, Auto-layout, Swim lanes with *Lock lane size*, and the line style for new connectors), *View* (step numbers, zoom) and *Help → Keyboard shortcuts* (built from the editor's commands, so it always matches what the keys do). Undo / redo, the grid, zoom / fit, the flow direction (→ / ↓) and *Snap to grid* (magnet) stay on the bar for quick access. `Esc` or a click elsewhere closes a menu.
- **Add shapes:** drag from the sidebar, click a sidebar item, or press `1`–`9`. A new shape's label opens for editing straight away with the default text selected, so just type its name and press `Enter`. Shapes added by a sidebar click, a number key or *+ Next step* also pan the view (zoom unchanged) so the new shape sits in the middle of the screen; dragged or double-click-placed shapes are already under the pointer, so the view stays put.
- **Flow direction:** the → / ↓ buttons on the toolbar, or *File → Flow direction* — *Left → right* or *Top → down* (default) — decides where the next step goes when you add one from the sidebar, a number key or *+ Next step*. It's saved with the diagram, and *Start with swim lanes* sets it to match the lanes (rows → left to right, columns → top to down). One undo step to change. The sidebar shows icons only — shapes in a 3 × 3 grid (each with its number key) followed by the List and IPO tiles (no number key), the eight circle versions in two rows of four, the five outcomes in one row, then text box, comment and group; hover a tile to see its name.
- **Help notes:** the panels keep their explanations behind small *i* buttons. Click one to show the note under that line; click again to hide it.
- **Auto-join:** with *Auto-join* on, a new shape (sidebar click or drag, number key, `T`) is connected from the currently selected item. Number keys and sidebar clicks place the new step after the selection in the **flow direction** — to the right or below (side by side across the flow for a second branch) — joined when *Auto-join* is on, unjoined when it's off. The panel's *+ Next step* button always joins, and dropping a port drag on empty canvas always creates a joined step.
- **Circles:** the *Circles* row in the sidebar has the eight classic shapes drawn as circles — icon in the middle, label underneath, in the type's colour: Start ▶ play, Process gear, Decision branch, Rule check shield, Action zap, Data / IFC database, Status activity, End flag. They keep their type, so a Decision circle is still a decision (properties, step, export meaning). Any classic shape can be switched with *Draw as circle* in its panel: a shape without an icon gets its type's circle icon, one with an icon keeps it; switching back puts the shape's own outline back and drops the circle icon. Changing the type of a circle keeps it a circle and swaps in the new type's icon.
- **Lists:** the *List* tile (sidebar, under the shapes) adds a titled box of rows. Type the title on the canvas (double-click the title band) or in the panel's *Label*. The panel's *Items* section edits the rows: label, an optional description (second line, smaller) and an optional icon (the + / icon button opens a picker), ↑ / ↓ to reorder, ✕ to remove, *+ Add item*. Rows all have the same height and the box always fits them, so it grows and shrinks as you add or remove rows (you can make it wider, not shorter). The list as a whole takes one step number.
- **Input / process / output boxes:** the *IPO* tile adds a titled box with three sections, side by side. Rows work like list rows, plus a section picker to move a row to another section and *+ Add to …* under each section. Rename a section by typing in its title box (blank = Input / Process / Output). *Stack sections top to bottom* puts the sections one under another — narrower, for slides and portrait pages.
- **Connecting to a row:** hover a list or IPO box and each row shows a ● port on its left and right. Drag from a row's port to connect from that row; drop any connector on a row to connect to it; drop on the title or the box's edge to connect to the whole box. A row connector leaves and arrives on the row's left or right side (whichever faces the other end, or the side you pin by dragging its end). Connectors that end on a row are drawn above the shapes so their arrowhead shows on the row. Removing a row removes the connectors on it; changing a list or IPO box to another type moves them to the shape itself.
- **Quick join:** select items (Shift+click or drag a box around them) and press `J` — they are joined in the order you selected them.
- **Swap places:** select exactly two shapes and click *Swap places* in the panel. Each shape (type, label, icon, colour, size, look, properties, link) moves to the other's spot, centred on it. The spots keep their connectors, their group and their step numbers, so if you built a flow and two steps are the wrong way round, swapping fixes it without moving anything by hand. Swapping a step with an outcome takes the step number with the step (outcomes have none). One undo puts them back.
- **Connect by dragging:** hover a shape, drag from one of its ● ports onto another shape. Drop on empty canvas to create a new step there, already joined.
- **Text boxes:** drag *Text box* from the sidebar, click it, or press `T` (joined to the selection when *Auto-join* is on). Multi-line — `Enter` adds a new line while editing; click away or `Esc` to finish. In the panel: font (*Sans* / *Serif* / *Mono*), size, bold, left / centre / right and top / middle / bottom alignment, text colour, background (none, white, sticky-note colours or the shape palette), border (none, solid, dashed, dotted, dash-dot, long dash) with weight and colour, rounded corners, and *Fit height to text*. When you finish typing (on the canvas or in the panel) the box grows taller if the text no longer fits — it never shrinks, so a box you made tall on purpose stays that way; use *Fit height to text* to shrink it. Groups around it grow to match. Text boxes have ● ports and connect like shapes (to shapes, groups or other text boxes); number keys add the next step from a selected text box. Resize from the corner handle. A borderless box shows a faint dashed outline on hover so you can find it.
- **Comments:** a comment is a text box with the *Comment* flag. Click *Comment* in the sidebar, press `C` (pins it to the selected item), or drop it on a shape. It gets the standard comment look — yellow sticky note, thin border, folded corner, Consolas — and its lines are dashed pins with no arrowhead (restyle them like any connector). Tick or untick *Comment* in the panel to flag any text box; *Reset to comment style* restores the look. Comments can't be pinned to each other.
- **Groups:** select items and press `Ctrl+G` (or click/drag *Group* from the sidebar for an empty one). Name it by double-clicking the title bar, pick a colour in the panel. Drag a group by its title bar — its contents move with it. Drag a loose item into a group to add it; drag a member into another group to move it there. Dragging a member outside its group keeps it in the group and the group grows to fit (the same as nudging with the arrow keys) — use *remove from group* in the panel or *Ungroup* to take items out. Click a group's body to select it; drag inside it to box-select its items.
  - **Group look:** in the panel — *Tinted background* on/off, border pattern (solid, dashed, dotted, dash-dot, long dash), border weight (*Light* / *Regular* / *Heavy*, like connectors), and title *Top* / *Bottom* as a *Band* (tinted strip inside the group) or *Plain text* (bold title; at the bottom it sits centred under the box). Drag a group by its title wherever it is. Exports (SVG / PDF) match; Mermaid keeps no-fill, dashed outlines and light / heavy weights.
  - **Align within the group:** left / centre / right, top / middle / bottom, distribute, *Arrange in a row / column*, *Fit to contents*.
  - **Centre in the group:** two buttons that keep the box size — *Centre box on items* moves the box so the items sit in its middle (items stay put), *Centre items in box* moves the items together, keeping their layout, to the middle of the box (box stays put). *Fit to contents* is different: it shrink-wraps the box. Both allow for the title band and labels under circles.
  - **Groups within groups:** select items inside a group and press `Ctrl+G` to make a group inside it; select whole groups (with or without other items) and press `Ctrl+G` to wrap them in a new group. Drop *Group* from the sidebar inside a group, or drag a group by its title until it sits wholly inside another group, to nest it. An inner group is drawn over its parent and moves, duplicates and deletes with it; every group grows to keep the groups inside it. Drag an item out of an inner group into its parent to move it up a level. *Remove from group* (panel) and *Ungroup* move items up one level, not straight to the top. Resizing a shape or group grows the groups around it. Mermaid export writes nested `subgraph`s.
- **Align:** select two or more items and use the align buttons in the panel. Alignment lines up the **shape outlines** — a label drawn under a circle or outcome is ignored, so a circle and a box aligned to the bottom share the same bottom edge. *Distribute* and *Column* still leave room for those labels so they don't run into the next item, and aligning inside a group keeps the labels inside it. Items already lined up on the same centre line stay together when you distribute: *Distribute vertically* moves items side by side on one row as a block, and *Distribute horizontally* does the same for items stacked in one column, so the gaps are spaced between rows (or columns) rather than between single items.
  - **Attach groups:** groups have ● ports like shapes, so connect group → shape, shape → group, or group → group (or select both and press `J`).
  - *Ungroup* (`Ctrl+Shift+G`) keeps the items; *Delete* removes the group and its contents.
- **Description:** every shape has an optional *Description* in the panel — smaller grey text under the label, for a short note on who does the step and how. On circles and outcomes it sits under the label; on boxes and other shapes (label inside) it sits under the shape. *Text width* sets how wide that text wraps (blank = automatic, a little wider than the shape) — it also sets the wrap width of a label drawn under a circle. *Transparent text background* drops the white backing behind text under the shape, so it sits cleanly on a tinted group (the backing is there by default to keep text readable where connectors pass behind it). Descriptions are included in SVG / PDF export, and in Mermaid export as a second line of the label.
- **Restyle several items at once:** select shapes, text boxes and/or groups together (Shift+click, drag a box or `Ctrl+A`). Below the align buttons the panel shows a style section for each kind selected — *Shapes* (colour, solid fill, emphasis, transparent text background), *Text boxes* (font, size, bold, alignment, text colour, background, border pattern / weight / colour, rounded corners) and *Groups* (colour, tint, border pattern / weight, title position and style). A click changes every selected item of that kind in one undo step. A button shows as on only when every selected item already has that setting; mixed font sizes show `—`. The single-item panels use the same controls.
- **Circle with icon (presentation style):** press `9` or drag *Circle*. Pick an icon from the built-in set (118 icons, searchable), a colour, and *Solid fill* for a bold look; the label sits under the circle. Circle labels are semibold (600), inside or under the circle, so they read well on slides. Any other shape can carry an icon too (shown beside its label), and every shape can take a colour.
- **Outcomes (pass / fail / rework / conditional / hold):** the *Outcomes* section of the sidebar adds review results that differ by **shape**, not just colour, so an A3 sheet printed in greyscale or read by a colour-blind reviewer still reads correctly:
  - *Pass* — scalloped seal with a tick (green)
  - *Fail* — octagon (stop sign) with an ✕ (red): rejected, the flow stops
  - *Rework* — circle with an arrow looping back (pink): returned to the author
  - *Conditional* — triangle with `!` (amber, dark mark): pass with comments / approved subject to changes
  - *Hold* — rounded square with a clock (slate): on hold / awaiting review

  Click or drag one in; switch between them with the *Outcome* buttons in the panel (a label still showing the old name follows). Solid fill is on by default; untick *Solid fill* for an outline look, or pick any colour — the outline and symbol stay. Labels sit under the symbol. Straight connectors clip to the real outline (seal, octagon, triangle).
- **Linked diagrams (drill-down):** any shape or group can point to another saved diagram that holds its more detailed flow. Select it and pick the diagram under *Linked diagram* in the panel (save the detailed flow as its own diagram first). A small 🔗 badge then appears at the bottom-right — on the outline for circles, outcomes and diamonds, at the corner for boxes, inside the bottom-right corner for groups. Click the badge (or *Open* in the panel) to open the linked diagram; if there are unsaved changes you're asked to *Save and open*, *Discard and open* or cancel. The toolbar then shows a breadcrumb (`Overview › Detail A › …`) with *← Back*; click any name to jump back up. Opening a diagram from *Open* or starting a *New* one clears the breadcrumb. A link whose diagram has been renamed or deleted shows a faded, dashed badge and a warning in the panel. The badge is also drawn in SVG and PDF exports; Mermaid export keeps links as `%% N1 links to diagram "…"` comments.
- **Page size (for documents):** the *Page* box in the toolbar switches between the open canvas (default) and a fixed sheet — A5–A0, US Letter, Legal, Tabloid, 16:9 and 4:3 slides, or a custom size in mm — with portrait/landscape buttons. The sheet shows on the canvas with dashed margin guides; the status bar warns if anything is off the page. In the Diagram panel (click empty canvas): margin, *Centre drawing on page*, and *Scale down to fit the margins on export*. **File → Export → SVG image** then produces a file exactly that paper size in mm, so every diagram drops into Word/PowerPoint at a consistent size. **File → Export → PDF — page** does the same as a PDF. Switching back to the open canvas leaves the drawing untouched.
- **Step order:** every shape except outcomes has a step number — its place in the flow — whether or not the numbers are shown. New shapes go on the end (one after the last step); deleting a shape closes the gap, so the steps always run 1, 2, 3, … Steps that happen at the same time share a number. In a shape's panel: type a number in *Step* to move it there (the steps from that number on move down one), or tick *Runs in parallel* to share the number of the step before it (untick to make it its own step again — the panel lists which steps it runs alongside). Select several shapes for *Run in parallel* (all take the earliest of their numbers) or *One after another* (numbered in turn, top to bottom). *View → Show step numbers* (or *Show step numbers* in the Diagram panel) draws each number in a small badge at the top-left of the shape, in the shape's colour; SVG / PDF / PNG exports match, and Mermaid export puts the number in front of the label (`3. Run clash tests`). Diagrams saved before step numbers existed are numbered when opened, following the connectors from where the flow starts (a connector that loops back, e.g. *Model updates → Exchange models*, doesn't count) and then reading order — check the order and adjust if needed. Outcomes (pass / fail / …) have no number: they're results of a step, not steps.
- **Step order pop-up:** *Arrange → Step order…* (or *Step order…* in the Diagram panel or a shape's panel) lists every step in order as numbered slots; parallel steps share a slot. Drag a step **between** two slots to make it a step of its own there, or **onto** a slot to run it in parallel with that step — the numbers close up and the badges on the canvas change as you go. Each row also has ↑ / ↓ (one place earlier / later; a parallel step comes out just before / after its slot) and a parallel button (join the step above, or split back out to its own step straight after). Click a name to select that shape on the diagram; each row shows the shape's type and the group it's in. Every change is one undo step (*Undo* in the pop-up, or Ctrl+Z after closing it). *Done*, Escape or a click outside closes it.
- **Swim lanes:** every top-level group is a lane. *Left → right* or *Top → down* (Diagram panel under *Swim lanes*, a top-level group's panel, or the panel for two or more selected groups) arranges them once — like *Align*, not a lasting layout, and one Undo puts everything back. The lanes line up edge to edge in their current order (rows stay in order when they become columns), all the same length along the flow and each as deep as it needs. Inside each lane the shapes go in columns by **step number**, so step 3 sits in the same column in every lane and the connectors between them run straight; parallel steps stack across the lane. An inner group moves as one block (its own layout is kept) in the column of the earliest step inside it. An outcome or text box connected to a step goes in that step's column, next to it — a decision's outcomes go one each side of it, with room for the Yes / No labels; steps sharing a column follow the steps that lead into them, so branches don't cross; an unconnected note goes in a column after the last step. Shapes outside every group stay where they are. Connectors between moved items lose hand-placed bends so they re-route. With two or more groups selected, only those are arranged.
- **Start with swim lanes:** on a blank diagram, *Start with swim lanes ▾* under the start hint opens a small form: which way the lanes run (*left → right* = lanes as rows, *top → down* = lanes as columns — one direction per diagram), the canvas size (*Open canvas* or a paper size, with portrait / landscape — the lanes then fill the page inside its margins), how many, the gap between them (0 = edge to edge), and *Lock lane size* (on by default). The diagram's flow direction is set to match the lanes. *Create lanes* adds them as top-level groups named *Lane 1*, *Lane 2*, … in different colours; rename and restyle them in the group panel as usual. With a page size set they fill the printable area; on an open canvas each lane is 1200 × 240 px. One undo removes them (and puts the page size back).
- **Quick branch (pass / fail):** select one shape (not an outcome) and press `B`, or *Branch pass / fail* in its panel or the *Edit* menu. It adds a **Pass** and a **Fail** outcome at 45° either side of the flow — left → right: Pass above, Fail below; top → down: Pass on the left, Fail on the right — each followed by a new process step straight on along the flow. The two new steps run in parallel (same step number), all four go in the source's group (lane), and the connectors leave the source on the side facing each branch and come into the outcome from the flow side. The Pass branch's step is selected with its label open for typing, and the view centres on the branch. One undo step.
- **Lock lane size:** *Arrange → Lock lane size* (also in the lane form). While it's on, the lanes (top-level groups) keep their size: adding, moving or resizing shapes no longer grows them, and a step placed past the end of a lane stays in that lane even when it hangs over the edge. Arranging lanes, resizing a lane by its handle and *Fit to contents* still change the size. Groups inside a lane still grow as usual. Saved with the diagram.
- **Tidy connectors:** *Arrange → Tidy connectors* re-routes elbow connectors that need it — never automatically, only when you click it. With a connector selected it tidies that one; with items selected, the connectors on them; with nothing selected, all of them. A connector needs tidying when it passes through a shape, a text box or the label / description under a shape, when its arrow arrives through its target's own label or description (hiding the arrowhead), or when it arrives on a side another connector leaves from (or the other way round). It tries every pair of sides and a range of positions for the middle segment and keeps the best route: nothing crossed first; then not squeezing past shapes, not hiding the arrowhead, not clashing at a shape's side, not running along or crossing other connectors; then fewest corners; then shortest. Connectors meeting at the same shape may share their last stretch. The result is stored like a hand-placed route (pinned sides and bend), so you can still drag it afterwards, and it stays put when you move shapes (run *Tidy* again, or reset the route in the connector's panel). Connectors that are fine — including ones you placed by hand — and straight connectors are left alone. Groups aren't obstacles (lines cross lane borders). One Undo puts the routes back; if nothing needed tidying, nothing changes and no undo step is added.
- **Auto-layout:** *Left → right* or *Top → down* under *Auto-layout* in the Diagram panel lays the whole diagram out once by **step number** — step 1, then 2, then 3, with parallel steps side by side across the flow — then runs *Tidy* on the connectors. One Undo puts everything back. It uses the same rules as swim lanes: an outcome or connected note sits next to the step it comes from, an unconnected note goes after the last step, and each shape's centre sits on its step's centre line so straight runs stay straight. Groups move as whole blocks (their insides aren't touched — use *Swim lanes* for that), placed at the earliest step inside them. The drawing keeps its top-left corner.
- **Start and End:** both are green pills, so the end of a flow doesn't read as a failure; new ones get an icon that tells them apart — ▶ (`play`) on *Start*, a flag on *End*. Change or remove the icon in the panel like any other.
- **Emphasis (major / normal / minor):** a per-shape option in the panel, under *Colour*. *Major* marks a key step: solid fill with a white icon and label, a semibold label and a thin ring drawn just outside the outline (any shape — the ring follows the outline). Connectors stop on the outside of the ring, so their arrowheads stay visible, and a label or description under a major shape starts below the ring. *Solid fill* is ticked and greyed out while a shape is major. *Normal* is the standard look. *Minor* marks a supporting step: white fill, a lighter and thinner outline, a lighter icon, muted text and no shadow. Emphasis never changes a shape's size or step number. SVG / PDF / PNG exports match; Mermaid export gives major steps a solid fill with a 4px outline and minor steps the light look. Duplicates keep the emphasis.
- **Presentation look (default):** every shape has a firm 2.4px outline and a soft shadow underneath (the same outline moved down 4px in the shape's colour, faint), so steps lift off the grid. The shadow is drawn as a plain shape rather than a blur, so SVG, PDF and PNG exports and greyscale prints match the canvas. Connectors default to 2px in a darker grey with slightly larger arrowheads, and elbow connectors have rounded bends.
- **Connector lines:** *Elbow* (right-angled) or *Straight* (direct, clipped neatly to each shape's outline — circles, diamonds, hexagons included). *Arrange → New connectors* sets the style for new connectors; change any single connector in its panel or press `L` with it selected. With several items selected you can switch all connectors between them at once; *Apply style to all* copies a connector's look to every connector.
- **Connector ends and bends:** select a connector to show its handles. Drag a ● end to another side of its shape — it snaps to the nearest side middle (top / right / bottom / left), and the side points light up while you drag. On elbow connectors, drag the ■ on the middle segment sideways to move the bend (snaps to the grid; double-click it to put it back in the middle). Once you've adjusted a connector both ends stay on the sides you chose and the bend keeps its offset when you move shapes; *Reset route to auto* in the panel hands control back. L-shaped connectors (a single corner) have no bend to drag — move an end to another side instead.
- **Connector line types:** in a connector's panel choose *Weight* (light / regular / heavy), *Line type* (solid, dashed, dotted, dash-dot, long dash), *Colour*, and *Arrowheads* (end / both / none). *Use as default* makes new connectors look the same; *Apply to all* restyles every connector. Mermaid export maps broken lines to dotted (`-.->`) and heavy solid lines to thick (`==>`).
- **Connector labels:** double-click a line (or select it and press `Enter`, or type in the panel). Up to two lines — `Shift+Enter` starts the second line (`Enter` finishes); anything longer belongs in a text box. Text direction: *Along line* (the default for new connectors), *Level* (always horizontal), or *90° to line* — text is never upside down. *Label placement*: *Inline* sits on the line (a two-line label straddles it — line one above, line two below, with the connector running between them), *Above* or *Below* sets the text beside the line, as the text reads. Labels beside or straddling the line have no box, just a thin white halo so they stay readable; single-line inline labels keep their white box. Where the text runs across the line (level text on an up-down segment, or 90° labels), *Inline* is boxed on the line and *Above* / *Below* put it to the left / right. Slide the label anywhere along the connector (or use *Near start / Middle / Near end*). Labels always sit on top of shapes; Mermaid export writes the two lines with `<br/>`.
- **Angle lock:** the *Angle* buttons in the toolbar lock drags to *90°* (horizontal / vertical only), *45°* or *30°* steps, or leave them *Free* (default). It applies when moving items (the selection follows the pointer along the nearest allowed direction) and when placing a joined step with the pointer — dropping a port drag on empty canvas, or dragging a shape from the sidebar with *Auto-join* on — where the new shape is put on an allowed angle from the shape it joins, measured centre to centre (the live connector shows where it will land). On a straight horizontal or vertical move the other axis stays exactly where it was, so items stay in line; on a diagonal the distance snaps to 10 px steps when *Snap* is on, and the grid isn't used per axis so the angle stays exact. Steps placed by number keys, sidebar clicks and *+ Next step* still go straight after the selection in the flow direction. A screen setting: kept while the page is open, not saved in the file.
- **Grid:** the *Grid* buttons in the toolbar switch the background between no grid, dots (default) and lines — on the open canvas and on a page sheet. It is a screen setting only: not saved in the file and never exported. *Snap* works the same whichever grid is shown.
- **Edit text:** double-click, or select and press `Enter`. Connector labels have quick chips (Yes / No / Pass / Fail).
- **Properties panel:** type, size, and free-form key/value properties per shape. Rule-check shapes start with `ruleId`, `ifcEntity`, `property`, `operator`, `value` — groundwork for the future rule builder.
- **Navigate:** mouse wheel pans, Ctrl+wheel zooms, right- or middle-drag pans, `F` fits the whole drawing, `Shift+F` (or *Selection* next to *Fit* in the toolbar, or *View → Zoom to selection*) zooms and centres on the selected items — a selected connector counts as its two ends.
- **Other:** `Del` delete, arrows nudge (Shift = 1px), `Ctrl+Z`/`Ctrl+Y` undo/redo, `Ctrl+D` duplicate (the copy keeps everything but the id: look, link, description, properties; groups keep their fill, border and title style), `Ctrl+A` select all, `Ctrl+S` save.

## File format (bediagram/1)

```json
{
  "schema": "bediagram/1",
  "name": "IFC audit flow",
  "showSteps": true,
  "flow": "right",
  "nodes": [
    { "id": "n_1a2b3c4d", "type": "check", "label": "Fire rating present",
      "description": "Every fire door must carry a rating.", "textWidth": 220,
      "x": 375, "y": 120, "w": 170, "h": 64, "step": 1,
      "properties": { "ruleId": "R-001", "ifcEntity": "IfcDoor", "property": "Pset_DoorCommon.FireRating", "operator": "exists", "value": "" } },
    { "id": "n_list", "type": "list", "label": "Model checks", "x": 60, "y": 300, "w": 240, "h": 200, "step": 2,
      "items": [ { "id": "i_2b3c4d5e", "label": "Naming", "description": "Prefix and case", "icon": "tag" },
                 { "id": "i_4d5e6f70", "label": "Classification" } ] },
    { "id": "n_ipo", "type": "ipo", "label": "Run audit", "x": 400, "y": 300, "w": 480, "h": 160, "step": 3,
      "sectionTitles": [ "Needs", "", "" ], "stacked": false,
      "items": [ { "id": "i_3c4d5e6f", "label": "IFC model", "section": "input" },
                 { "id": "i_5e6f7081", "label": "IDS check", "section": "process" },
                 { "id": "i_6f708192", "label": "BCF topics", "section": "output" } ] }
  ],
  "edges":    [ { "id": "e_...", "from": "n_...", "to": "n_...", "label": "Pass",
                  "style": "elbow", "labelOrient": "along", "labelPlace": "inline", "labelPos": 0.5,
                  "fromSide": "right", "toSide": "top", "bend": 40,
                  "weight": "heavy", "pattern": "dashed", "color": "red", "arrows": "end" },
                { "id": "e_...", "from": "n_list", "fromItem": "i_2b3c4d5e", "to": "n_ipo", "toItem": "i_3c4d5e6f", "label": "" } ],
  "textBoxes": [ { "id": "t_...", "text": "Line one\nLine two", "isComment": true,
                   "x": 0, "y": 0, "w": 200, "h": 110, "groupId": null,
                   "fill": "yellow", "border": "solid", "borderWeight": "light", "borderColor": "yellow", "rounded": true,
                   "font": "mono", "fontSize": 12, "bold": false, "textColor": "ink", "align": "left", "vAlign": "top" } ],
  "groups":   [ { "id": "g_...", "title": "Audit", "color": "teal", "x": 0, "y": 0, "w": 360, "h": 220, "link": "Audit detail",
                  "filled": false, "border": "dashed", "headerPos": "bottom", "headerStyle": "text" },
                { "id": "g_...", "title": "Checks", "color": "blue", "x": 20, "y": 52, "w": 200, "h": 120, "groupId": "g_..." } ],
  "page":     { "size": "A4", "orientation": "landscape", "marginMm": 10, "fitOnExport": true, "customW": 210, "customH": 297 }
}
```

Connectors: `label` holds at most two lines separated by `\n`; `labelPlace` is `inline`, `above` or `below`; `labelOrient` is `along`, `horizontal` or `perpendicular`. `fromSide` / `toSide` (`top`, `right`, `bottom`, `left`) pin an end to that side's middle and are left out when automatic; `bend` is how far (px) the middle segment of an elbow connector has been moved from its automatic spot — along x for an up-down segment, along y for a left-right one — and is left out when 0. Nodes, text boxes and groups carry an optional `groupId`: the id of the group directly holding them (a group inside a group points at its parent; a top-level group leaves `groupId` out). The file stays flat — the hierarchy is only these parent ids; nodes also have optional `icon`, `color` and `filled`, plus `description` (smaller text under the label) `textWidth` (wrap width in px for text drawn under the shape; left out = automatic) and `clearTextBack` (`true` = no white backing behind that text, so a tinted group shows through; left out = white backing). Nodes have `step` (place in the flow, 1, 2, 3, …; shapes running in parallel share a number; left out only on outcomes) and `emphasis` (`"major"` or `"minor"`; left out for normal), and the diagram has `showSteps` (`true` = draw the numbers; left out when off), `flow` (`"down"` or `"right"`: which way new steps go) and `lockLanes` (`true` = top-level groups keep their size; left out when off). Nodes and groups have an optional `link`: the name of another diagram in the same folder (file name without `.diagram.json`) that holds the more detailed flow — left out when there is none. Groups: `filled` (tinted background or none), `border` (a line pattern: `solid`, `dashed`, `dotted`, `dashdot`, `longdash`), `borderWeight` (`light` / `regular` / `heavy`; files without it are `regular`), `headerPos` (`top` / `bottom`) and `headerStyle` (`band` = title on a tinted strip inside the group, `text` = plain bold title — inside at the top, or centred under the box when at the bottom). Outcome nodes have `"type": "outcome"` and `"outcome": "pass" | "fail" | "rework" | "conditional" | "hold"` — the meaning lives in that field, the seal/octagon/triangle is only how it is drawn, so the rule compiler never has to guess from a picture (`outcome` is left out on other node types). Text boxes: `fill`, `borderColor` and `textColor` are colour keys (`white`, `grey`, `ink`, `yellow`, `sky`, `mint`, `rose`, or a palette key such as `blue`) or `null` for the default; `border` is `none` or a line pattern; `font` is `sans`, `serif` or `mono`. `isComment` marks annotations, so code reading the file can skip them or treat them as notes. Older files with a `comments` list still open — those become comment text boxes and are saved as `textBoxes`.

Mermaid export writes text boxes (comments included) as nodes with a `style` line for their background, border and text colour. Mermaid has no seal or octagon, so outcomes export as the nearest classic shape with a symbol in the label and their fill colour: pass `((✓ …))`, fail `{{✕ …}}`, rework `((↺ …))`, conditional `[/⚠ …\]`, hold `(⏸ …)`.

Nodes of the eight classic types may have `"round": true`: drawn as a circle (icon in the middle, label underneath) while keeping their `type`; left out when false.

List and IPO nodes (`"type": "list"` / `"ipo"`) hold their rows in `items`, in order: each row has an `id` (unique inside its node, kept when the node is duplicated), `label`, and optional `description` and `icon`; IPO rows also have `section` (`input`, `process` or `output`). IPO nodes have optional `sectionTitles` (renamed section titles in input / process / output order; a blank entry or a missing list = the default title) and `stacked` (`true` = sections top to bottom; left out when side by side). A connector's optional `fromItem` / `toItem` name the row it leaves from / arrives at (left out = the whole node). Rows have a fixed height, so `h` always fits the rows. In the Mermaid export a list or IPO box is a `subgraph` with a node per row (IPO sections are nested subgraphs), so connectors to rows keep pointing at the row.

Node types: `start`, `process`, `decision`, `check`, `action`, `data`, `status`, `end`, `circle`, `outcome`, `list`, `ipo`. Typed nodes + properties mean the audit tool can later read these files and turn a diagram into rules without a format change.

## Project layout

```
BeDiagram.sln                solution (open this in Visual Studio)
BeDiagram/                   the web app project
  Models/                      the diagram model: Diagram, Node, Edge, TextBox, Group, PageSetup, IBox, IGroupable
  Models/Routing/               connector routes (Strategy: ElbowRouting / StraightRouting) and connector labels
  Models/Text/TextLayout.cs    node labels, descriptions, text boxes, wrapping
  Models/Boxes.cs              hit tests and bounds
  Rendering/SvgShapes.cs       SVG markup shared by canvas + export (halo, shadow, badges)
  Models/Icons.cs              built-in icon set (Lucide, ISC licence — see LICENSE-lucide.txt)
  Models/Catalogue/            the fixed value sets as enums (NodeType, OutcomeType, Side, EdgeStyle, …) and the type tables (NodeTypes, Outcomes, …)
  Models/Serialization/        enums saved by name, so the file reads "decision", not 3
  Models/Styles/               looks and colours: Palette, Swatch, TextPalette, LineLook, TextLook, GroupLook, NodeStyle, TextStyle
  Models/Layout/               geometry results shared by canvas + export: EdgeRoute, EdgeGeom, LabelLayout, Rect, …
  Models/Shapes/               one class per shape outline (Strategy pattern, see below)
  Models/Kinds/                one class per kind of node: classic, circle, outcome, list, IPO (Strategy pattern, see below)
  Models/Panels/               layout of list / IPO nodes (Strategy pattern): list, IPO side by side, IPO stacked
  Models/ListItem.cs           one row of a list / IPO node
  Services/DiagramStore.cs     local folder save / open / list / delete
  Services/Export/Pdf/         vector PDF made from the exported SVG (no third-party libraries): PdfWriter, PdfRenderer, …
  Services/Export/             export strategies (see below)
  Editing/DiagramEditor.cs     the diagram being edited: state, lookups and undo (no UI code); the editing actions are
                               in partial files by area: .Creating, .Groups, .Alignment, .Selection, .Actions, .Page,
                               .Connectors, .Styling, .StepOrder, .Layout, .Text, .Panels (list / IPO rows)
  Editing/IEditorView.cs       what the editor needs from the screen (toast, view centre, start text editing)
  Editing/Angles/              angle lock for moves and placed steps (Strategy pattern): IAngleLock, FreeAngle, StepAngle, AngleLocks
  Models/NodeFactory.cs        new shapes from a PaletteItem (defaults from NodeTypes, label / outcome / rows from the kind)
  Models/GroupTree.cs          the group hierarchy (Composite pattern) worked out from the GroupId fields
  Models/StepOrder.cs          the step order: numbering, closing gaps, moving and parallel steps
  Models/Emphasis/             major / normal / minor looks (Strategy pattern, see below)
  Components/Pages/Designer.razor(.cs)   the editor page: toolbar, palette, canvas, dialogs, files, export; shows the side
                               panel for the selection; passes input to the editor.
                               The code is in partial files by area: .Pointer, .Palette, .View, .Page, .Files,
                               .Navigation, .Render, .Commands, .CommandContext, .EditorContext, .EditorView, .Host
  Components/Pages/IDesignerHost.cs, DesignerPart.cs   how the page's child components reach it (Mediator, see below)
  Components/Panels/           the side panel, one component per kind of selection (shape, text box, connector, group,
                               several items, nothing selected)
  Components/Pages/Gestures/   mouse gestures (State pattern, see below)
  Components/Pages/Commands/   editor commands and keyboard shortcuts (Command pattern, see below)
  Components/Pages/Views/      view records the Designer markup reads (NodeView, EdgeView, GroupView, …)
  wwwroot/js/designer.js       keyboard shortcuts, wheel zoom, canvas size, focus
  wwwroot/js/export.js         draws the SVG export onto a canvas for PNG export
  wwwroot/app.css              styling
tools/Check-OneTypePerFile.ps1   checks the one-type-per-file rule (see below)
tools/regression/                side-by-side regression tests: master vs your changes (see its README)
```

**One type per file.** Every class, record, interface and enum is in its own `.cs` file named after it (`MoveGesture` → `MoveGesture.cs`). Partial classes use `Type.Area.cs` (`Designer.EditorContext.cs`). Private nested helpers may stay nested; public nested types get their own file. Folders match namespaces (`Models/Styles/` = `BeDiagram.Models.Styles`). Check it with:

```
powershell -ExecutionPolicy Bypass -File tools\Check-OneTypePerFile.ps1
```

### Export: strategy pattern

Export is split into two families of strategies, put together by one context class:

- **`IExportFormat`** — *what kind of file*: `SvgExportFormat`, `PngExportFormat`, `PdfExportFormat`, `MermaidExportFormat`. Each turns a diagram (plus an optional view window) into bytes. SVG, PNG and PDF all start from the same SVG, written by **`SvgDiagramWriter`** (registered once and given to those three formats); the Mermaid writer lives in `MermaidExportFormat` itself.
- **`IExportArea`** — *which part of the drawing*: `WholeDrawingArea` (page or extents) and `CurrentViewArea` (what's on screen, saved with a `.view` suffix).
- **`ExportService`** — the context. It gets every registered format and area from dependency injection, lists the combinations for the Export menu (`Choices()`), and runs one (`ExportAsync`: area → view window, format → bytes, then save next to the diagram).

`PngExportFormat` uses one more small strategy, `IPngRasterizer`, for turning SVG into pixels; `BrowserPngRasterizer` does it in the browser (`wwwroot/js/export.js`). A server-side rasteriser could be swapped in without touching the format.

To add an export format or area: write a class implementing the interface and register it in `Program.cs` — the menu picks it up; the Designer doesn't change. Menu order = registration order.

### Editor and page: DiagramEditor

The diagram and everything you can do to it live in **`DiagramEditor`** (`Editing/`): the open `Diagram`, the selection, inline-edit state, undo / redo (whole-diagram snapshots, i.e. the Memento pattern), and every editing action. That covers adding, joining, grouping, aligning, duplicating, deleting, nudging, restyling and the panel setters. It has no Blazor, browser or file code.

The **Designer** page (`Designer.razor` + partial classes) is the view. It owns pan / zoom, the gesture in progress, dialogs, toasts, files, linked-diagram navigation and export. It turns mouse, keyboard and panel input into editor calls:
- the markup calls the editor directly (`Editor.SetNodeColour(...)`, `Editor.Checkpoint`), and reads the diagram as `D`;
- gestures (`IEditorContext`) and commands (`ICommandContext`) reach the editor through the Designer, which also adds the view-only parts (panning, zoom, save);
- the editor talks back through **`IEditorView`** (show a toast, the view centre, focus the inline text box), which the Designer implements in `Designer.EditorView.cs`.

New shapes come from **`NodeFactory`** (`Models/`). It takes the type's data from `NodeTypes.All` (size, icon, circle icon, default property keys) and asks the type's kind for the rest (label, outcome, starter rows).

### Node kinds: Strategy pattern

Everything that depends on what *kind* of node a shape is sits behind **`INodeKind`**, with one class per kind in `Models/Kinds/`. Each entry in `NodeTypes.All` holds its kind, and a node reaches it as `n.Kind`.

| Kind | Types | What's different |
|---|---|---|
| `ClassicKind` | Start, Process, Decision, Rule check, Action, Data / IFC, Status, End | can be drawn as a circle (`Round`) |
| `CircleKind` | Circle | always a circle with an icon; a new one is labelled "Label" |
| `OutcomeKind` | Outcome | not a step; outline, glyph and colour come from the outcome; label always below |
| `ListKind`, `IpoKind` (both `PanelKind`) | List, Input / process / output | rows; title in a band at the top |

- `NodeKind` is the base class with classic-shape behaviour; each kind overrides only what differs.
- The kind answers: is it a step, can it be round, is it a panel, does it have an outcome; which outline and icon it's drawn with; where the label goes; its colours before emphasis; its names; and what a new or retyped node starts with.
- Code elsewhere asks the kind (`n.Kind.IsStep`, `n.Kind.LayoutLabel(n, label)`) rather than testing `n.Type == "outcome"`.

The inside of a list or IPO box is a second strategy, **`IPanelLayout`** (`Models/Panels/`): `ListLayout`, `IpoColumnsLayout` (sections side by side) and `IpoStackedLayout` (one under another), all derived from `PanelLayout`, which holds the title band, how one row is drawn, the SVG markup and finding a row for a connector. The kind picks the layout (`PanelKind.LayoutOf`), so the canvas, export, connectors and resize all ask the same object.

### Nested groups: Composite pattern

Groups can hold shapes, text boxes and other groups. In Composite terms, **`IGroupable`** is the component, `Node` and `TextBox` are leaves and **`Group`** is the composite. The file keeps flat lists with a parent id (`GroupId`) on each item, so **`GroupTree`** (`Models/`) works the hierarchy out from the diagram whenever it's asked: `Parent`, `Children`, `Descendants`, `Ancestors`, `IsInside`, `CommonParent` and `PaintOrder` (each top-level group followed by the groups inside it, so inner groups draw over their parents). Every walk keeps a visited set, so a hand-edited file with a parent loop can't hang the editor.

The editor's group operations go through the tree: moving, duplicating, deleting and box-selection carry every descendant; growing a group grows the groups around it; a group can't be put inside itself or inside one of its own groups; a group can't connect to anything inside it. The canvas, SVG / PDF / PNG exports and hit testing all use `PaintOrder`; Mermaid export writes nested `subgraph`s from `Children`.

### Shape outlines: Strategy pattern

Everything that depends on a shape's outline sits behind **`IShapeOutline`**, with one class per outline in `Models/Shapes/`. That covers the SVG path, where a straight connector meets the outline, where the 🔗 badge sits, how the label and icon are laid out, the rule-check stripe, and the Mermaid syntax.

| Outline | Used by |
|---|---|
| `RectOutline`, `CheckOutline` (adds the left stripe) | Process, Rule check |
| `RoundedRectOutline`, `PillOutline` | Action; Start / End |
| `DiamondOutline`, `ParallelogramOutline`, `HexagonOutline` | Decision, Data / IFC, Status |
| `CircleOutline` | Circle, Rework outcome |
| `SealOutline`, `OctagonOutline`, `TriangleOutline`, `RoundedSquareOutline` | Pass, Fail, Conditional, Hold outcomes |
| `PanelOutline` | List, Input / process / output (the inside comes from the panel layout, see below) |

- `ShapeOutline` is the base class with plain-rectangle behaviour; each outline overrides only what differs.
- `ShapeOutlines` holds one shared instance of each outline; `NodeTypes` and `Outcomes` point at them directly (`ShapeOutlines.Diamond`), so there is no lookup by name.
- `node.Kind.OutlineOf(node)` (or `ShapeOutlines.Of(box)`) gives the outline a node is drawn with. The canvas and the SVG export both go through it, so they always match.

To add a shape: write a class (usually derived from `ShapeOutline`), add an instance to `ShapeOutlines`, and point a `NodeTypes` (or `Outcomes`) entry at it. Nothing else changes.

### Shape emphasis: Strategy pattern

How strongly a shape is drawn sits behind **`IEmphasis`**, with one class per level in `Models/Emphasis/`: `MajorEmphasis`, `NormalEmphasis` (the default) and `MinorEmphasis`, all derived from the `NodeEmphasis` base (the normal look). Each says what differs: it adjusts the colours (`Adjust`, used by `NodeStyle.For`), and gives the outline width, the shadow opacity, whether a halo ring is drawn, and the label's weight and colour. `Emphases.Of(node)` finds the strategy from the node's `emphasis` field; the canvas and the SVG export ask it directly (`Emphases.Of(n).StrokeWidth`, `.ShadowOpacity`, `.LabelWeight(n)`, `.BelowLabelColour`) and through `SvgShapes.HaloSvg`, so they always match. To add a level: write a class derived from `NodeEmphasis` and add it to `Emphases.All` — the panel buttons come from that list.

### Editor commands and shortcuts: Command pattern

Every keyboard shortcut is an **`IEditorCommand`** object: a `Name`, its `Keys` (shortcuts), `CanExecute` and `ExecuteAsync`. Toolbar and panel buttons run the same objects, so an action, its shortcut, its tooltip ("Undo (Ctrl+Z)") and its disabled state are defined in one place.

| Role (GoF) | In BeDiagram |
|---|---|
| Command | `IEditorCommand`, with the `EditorCommand` base class for the usual synchronous, always-available case |
| Concrete commands | `SaveCommand`, `UndoCommand`, `DuplicateCommand`, `DeleteCommand`, `GroupCommand`, `NudgeCommand` (one per direction and step), `AddShapeCommand` (one per number key), `ZoomInCommand`, … — one class per file in `Components/Pages/Commands/` |
| Receiver | The Designer, seen through **`ICommandContext`** (implemented in `Designer.CommandContext.cs`) |
| Invoker | **`CommandRegistry`** (a singleton registered in `Program.cs`): finds the command for a key press. The Designer's `OnKey` lives in `Designer.Commands.cs`; buttons go through **`CommandButtons`** (`Run`, `CanRun`, `Tip`, and the commands that have buttons: `Cmd.Save`, `Cmd.Delete`, …) |

Shortcut matching: a key press first looks for an exact match. If there is none and Shift was held, it tries the same key without Shift. So Shift only matters where a command asks for it (Ctrl+Shift+S, Ctrl+Shift+G, Shift+arrows), and Shift+J still joins. The registry refuses two commands with the same shortcut.

To add a command: write a class (usually derived from `EditorCommand`), add it to `CommandRegistry.CreateDefault()`, and add any new editor action it needs to `ICommandContext` and `Designer.CommandContext.cs`. If it's a new key, add it to the key list in `wwwroot/js/designer.js` so the browser forwards it. Undo stays snapshot-based (Memento): commands call editor actions that already record undo.

### Page parts: Mediator

The editor page is split into child components (Blazor components with their own markup and code-behind), so no one
file holds the whole UI. So far the side panel is split: `ShapePanel`, `TextBoxPanel`, `ConnectorPanel`, `GroupPanel`,
`SelectionPanel` and `DiagramPanel` in `Components/Panels/`, one of which the page shows for the current selection.

| Role (GoF) | In BeDiagram |
|---|---|
| Mediator | **`IDesignerHost`**, implemented by the Designer in `Designer.Host.cs`: the editor, the command buttons, redraw, and what only the page can do (dialogs, following links, page size) |
| Colleagues | The parts, each derived from **`DesignerPart`** and given `Host="this"`. They don't know about each other, only the host |

Blazor redraws only the component that handled an event, so a click in a panel would leave the canvas out of date.
`DesignerPart` therefore redraws the whole page after every event a part handles, just as when the markup was all in
`Designer.razor`. To give a part something new from the page, add it to `IDesignerHost` and `Designer.Host.cs`.

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

Shared behaviour sits in two base classes. `DragGesture` keeps the start point and the current point. `UndoableDragGesture` takes an undo snapshot at the start and records it on pointer-up only if something changed, so a plain click adds no undo step. Gestures reach the editor only through **`IEditorContext`** (diagram, selection, lookups, snapping, angle lock, undo), which the Designer implements in `Designer.EditorContext.cs`.

The angle lock is a strategy too: `IAngleLock.Apply(dx, dy, snap)` turns a drag offset into a `LockedOffset` (the allowed offset, plus which axes may still snap to the grid). `FreeAngle` passes it through; `StepAngle(90 / 45 / 30)` turns it to the nearest allowed direction. `MoveGesture` uses `LockOffset`, and `ConnectGesture` / palette drops use `LockPoint` from the joined shape's centre. A new kind of lock (e.g. 15°) is one more entry in `AngleLocks.All`.

To add a gesture: write a class (usually derived from `UndoableDragGesture`) and create it from a pointer-down handler. The other gestures don't change.

To add a shape type: add a member to the `NodeType` enum (its camelCase name is what the file saves) and an entry to `NodeTypes.All` with its kind (usually the shared classic kind) and, optionally, its icons and default property keys. A type that behaves differently gets its own `INodeKind` class.

## Known limits / next steps

- Connectors don't route around other shapes as you draw; *Tidy* re-routes elbow connectors on request. Straight connectors are never re-routed.
- Auto-layout is by step number, not a general graph layout: shapes with the same step stack side by side, and branches that rejoin later aren't straightened. (MSAGL stays the candidate if that's ever needed.)
- No Mermaid import yet.
- PDF text uses the standard PDF fonts (Helvetica / Times / Courier), so text widths differ slightly from Segoe UI on screen; characters outside Windows-1252 print as `?`.
- One page per diagram (no multi-page documents).
- Linked diagrams open in place of the current one; showing a linked diagram on the same canvas (each sheet as a sized diagram object) is planned for later. Links are by name, so renaming a diagram with *Save as…* doesn't update links that point to it.
- Single user, local only, by design.

## Credits

Icons: [Lucide](https://lucide.dev), ISC licence (`LICENSE-lucide.txt`).
