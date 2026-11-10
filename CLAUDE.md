# BeDiagram — notes for Claude

Local, self-hosted diagramming tool (flowcharts / workflows) built with .NET 10 Blazor Server.
Owner: Nick (shnicko). Diagrams are saved as JSON files on the user's machine — no cloud, no database.
Longer term it becomes a feature of the Data Audit Tools project (visual rule builder, BCF issue workflows),
so the file format must stay meaningful to code, not just pretty.

## Working agreement

- One feature or fix per branch and pull request, into `master` (the default branch). Never push to `master` directly.
- Branch names: `feature/<short-name>` or `fix/<short-name>`.
- Keep PRs reviewable: say what changed, how it was tested, and anything the user should check by eye.
- Update `README.md` (usage + file format sections) whenever a user-visible feature changes.
- No backwards-compatibility / file versioning work for now unless asked.
- The user opens the solution in Visual Studio and pulls `master` after merging — don't commit build output or IDE files.

## Layout

```
BeDiagram.sln
BeDiagram/
  Models/                data model, one type per file: Diagram, Node, Edge, TextBox (IsComment flag), Group, PageSetup,
                         IBox, IGroupable, LegacyComment
  Models/Routing/        connectors (BeDiagram.Models.Routing): EdgeRouter (entry point: Route, EdgeGeometry, SidePoint,
                         AutoSides, PointAt, RoundedPathD), IConnectorRouting (Strategy) → ElbowRouting / StraightRouting,
                         EdgeLabels (label lines and layout)
  Models/Text/TextLayout.cs  node labels and descriptions, text boxes, wrapping, room taken under a shape (ExtraBelow)
  Models/Boxes.cs        hit tests (Contains / Intersects), bounds (GroupBounds / ContentBounds), halo reach (HaloOut)
  Models/Shapes/Paths.cs SVG path data from geometry: RoundRect, PathD
  Models/Group.cs        also the group title geometry: InsetTop / InsetBottom, TitleArea, TitlePos, BandPath, FillOpacity
  Rendering/SvgShapes.cs SVG markup shared by canvas and export (BeDiagram.Rendering): ShapePath, halo ring, shadow,
                         step / link badges, text-box paths
  Models/Catalogue/      type tables (BeDiagram.Models.Catalogue): the enums NodeType, OutcomeType, Side, EdgeStyle,
                         LabelOrient, LabelPlace, FlowDirection, IpoSection; the tables NodeTypes / NodeTypeInfo, Outcomes /
                         OutcomeInfo, Sides, LabelPlaces, IpoSections
  Models/Serialization/  enums saved by name (BeDiagram.Models.Serialization): EnumNames (camelCase name ↔ value),
                         EnumNameConverter (unknown name → the enum's first member), OptionalEnumNameConverter (→ null)
  Models/PaletteItem.cs  what a palette tile / number key / gesture adds: NodeType + optional OutcomeType + Round
  Models/Styles/         looks and colours (BeDiagram.Models.Styles): Palette, Swatch, TextPalette, LineLook, TextLook,
                         GroupLook, NodeStyle, TextStyle
  Models/Layout/         geometry results (BeDiagram.Models.Layout): EdgeRoute, EdgeGeom, LabelLayout, TextBoxLayout, TextLine,
                         EdgeLabelLayout, EdgeLabelLine, Rect
  Models/Shapes/         shape outlines (Strategy pattern, BeDiagram.Models.Shapes): IShapeOutline, ShapeOutline base,
                         one class per outline (Rect, Check, RoundedRect, Pill, Diamond, Parallelogram, Hexagon, Circle,
                         Seal, Octagon, Triangle, RoundedSquare), ShapeOutlines (one shared instance of each outline)
  Models/Panels/         list / IPO layouts (Strategy pattern, BeDiagram.Models.Panels): IPanelLayout, PanelLayout base
                         (title band, row layout, SVG body, row ends / ports), ListLayout, IpoLayout → IpoColumnsLayout /
                         IpoStackedLayout; ListItem = one row; Catalogue/IpoSections
  Models/Kinds/          node kinds (Strategy pattern, BeDiagram.Models.Kinds): INodeKind, NodeKind base, ClassicKind,
                         CircleKind, OutcomeKind, PanelKind → ListKind / IpoKind; each NodeTypeInfo holds its Kind, a node
                         reaches it as Node.Kind
  Models/GroupTree.cs    group hierarchy (Composite): parent / children / descendants / ancestors / paint order from GroupId
  Models/Emphasis/       major / normal / minor looks (Strategy pattern, BeDiagram.Models.Emphasis): IEmphasis, NodeEmphasis
                         base, NormalEmphasis, MajorEmphasis, MinorEmphasis, Emphases (lookup by Node.Emphasis, an EmphasisLevel)
  Models/StepOrder.cs    step order (Node.Step): numbering, gap-closing, move / parallel, flow order for unnumbered shapes
  Models/NodeFactory.cs  new nodes from a PaletteItem (a type, an outcome tile, a classic shape as a circle): size, icon and
                         default properties from NodeTypeInfo, label / outcome / rows from the kind
  Models/Icons.cs        generated Lucide icon markup (ISC) — picker categories + a few UI icons
  Editing/DiagramEditor.cs   the editor model, no UI (BeDiagram.Editing): open Diagram, Selection / SelectedEdgeId /
                             EditingId, Dirty, AutoConnect / Snap, undo / redo (snapshots), and every editing action
                             (create, join, group, align, duplicate, delete, nudge, panel setters)
  Editing/DiagramEditor.*.cs partial files by area (find a method by what it's about): .Creating (add shapes / text /
                             groups, quick-add, quick branch, join), .Groups (grouping, nesting, fit / centre, title look),
                             .Alignment (align / distribute / swap), .Selection (selection, connector ends / resets),
                             .Actions (join, delete, duplicate, nudge), .Page (page, flow, lane lock), .Connectors (style,
                             look, labels), .Styling (type, outcome, round, icon, emphasis, links, bulk restyle),
                             .StepOrder, .Layout (auto-layout, tidy, swim lanes), .Text (text boxes, properties, inline
                             editing), .Panels (list / IPO rows: Add / Remove / Move / sections / SetStacked, FitPanel,
                             RetypeRows, Ends(edge)). DiagramEditor.cs keeps state, lookups and undo.
  Editing/IEditorView.cs     what the editor needs from the screen: ShowToast, ViewCentre, CentreOn, EditStarted
  Editing/AlignOp.cs         align / distribute / row / column operations
  Editing/Lanes/             swim lanes (Strategy pattern, BeDiagram.Editing.Lanes): ILaneLayout, HorizontalLanes,
                             VerticalLanes, LaneLayouts, LaneArranger (the one-off arrange). Editor.ArrangeLanes(layout);
                             auto-layout = LaneArranger.ArrangeAll (whole diagram, one lane, no box) + Tidy: Editor.AutoLayout(layout)
  Editing/Routing/           Tidy connectors (BeDiagram.Editing.Routing): ConnectorTidier (Plan → TidyPlan, plan.Apply()). Editor.TidyConnectors()
  Editing/Angles/            angle lock (Strategy pattern, BeDiagram.Editing.Angles): IAngleLock, LockedOffset, FreeAngle,
                             StepAngle (90 / 45 / 30), AngleLocks. Editor.LockOffset (moves) / LockPoint (placed steps)
  Services/DiagramStore.cs   save/open/list/delete *.diagram.json in the storage folder
  Services/ExportView.cs     the window onto the drawing that a current-view export uses
  Services/Export/           export strategies: IExportFormat (Svg/Png/Pdf/Mermaid), IExportArea (WholeDrawing/CurrentView),
                             ExportService (context: lists menu choices, renders + saves), ExportChoice (menu entry: Label /
                             Tip), IPngRasterizer (BrowserPngRasterizer), SvgDiagramWriter (the SVG renderer behind SVG / PNG /
                             PDF, page-sized in mm when a page is set, or an ExportView window; injected); the Mermaid writer
                             lives in MermaidExportFormat
  Services/Export/Pdf/       PDF = SvgDiagramWriter output converted to PDF operators, no libraries (BeDiagram.Services.Export.Pdf):
                             PdfWriter (SVG → file; injected into PdfExportFormat), PdfRenderer (SVG elements → content
                             stream), PdfStyle, SvgPath / SvgPathTokens (path data → operators), PdfMatrix (transforms),
                             PdfFonts (core fonts + metrics), Win1252Encoding; SvgValues / PdfSyntax (pure parse / format helpers)
  Components/Pages/Designer.razor      the editor page — markup only: the SVG canvas (defs, background, the layers in
                                       order, gesture previews and connector handles) and the child components around it
                                       (Components/Bars, Components/Panels, Components/Dialogs)
  Components/Pages/IDesignerHost.cs    what the page's child components may ask of it (Mediator): Editor, Buttons, Redraw,
                                       dialogs, files, export, links / breadcrumb, page size, zoom, grid, open menu, toast,
                                       palette drop. The Designer implements it in Designer.Host.cs
  Components/Pages/DesignerPart.cs     base class of those child components: Host, Editor / D / Cmd / Run / CanRun / Tip, and
                                       every event it handles redraws the whole page
  Components/Panels/                   the side panel (BeDiagram.Components.Panels): ShapePanel, TextBoxPanel, ConnectorPanel,
                                       GroupPanel, SelectionPanel (several items), DiagramPanel (nothing selected)
  Components/Bars/                     the bars round the canvas (BeDiagram.Components.Bars): Toolbar with FileMenu / EditMenu /
                                       ArrangeMenu / ViewMenu (each a DropMenu), ShapePalette (tiles; a drag tells the page what
                                       to add on drop), StatusBar
  Components/Canvas/                   the canvas layers (BeDiagram.Components.Canvas), drawn by the page in this order:
                                       PageSheet, GroupLayer, TextBoxLayer, EdgeLayer, NodeLayer, EdgeLayer RowLayer=true,
                                       EdgeLabelLayer. Each derives from CanvasLayer (Ports, EditKey), builds its own view records
                                       and hands pointer presses to the page through ICanvasInput (Host.Canvas)
  Components/Pages/ICanvasInput.cs     pointer presses on items (ItemDown, GroupBodyDown, PortDown, ResizeDown, EdgeDown): the
                                       page starts the gesture. Moves and releases stay on the page's SVG
  Components/Dialogs/                  pop-ups (BeDiagram.Components.Dialogs): OpenDialog, SaveAsDialog, ConfirmNewDialog,
                                       ConfirmLeaveDialog, ShortcutsDialog, StepOrderDialog. Plain components: parameters
                                       in, EventCallbacks out to the page (no Host), each keeping its own field state
  Components/Pages/Designer.razor.cs   the page: fields, lifecycle, canvas coordinates. The rest is in partial
                                       files by area: Designer.Pointer (pointer handlers → gestures), .Palette (tiles, drag
                                       and drop), .View (pan / zoom), .Page (paper size, orientation, the sheet), .Files,
                                       .Navigation (linked diagrams), .Render (canvas background, markers, gesture previews),
                                       .Host (IDesignerHost / ICanvasInput)
  Components/Pages/Designer.EditorView.cs      Designer's IEditorView implementation (what the editor needs from the screen)
  Components/Pages/Designer.EditorContext.cs   Designer's IEditorContext implementation (what gestures may use)
  Components/Pages/Gestures/           mouse gestures (State pattern): IGesture, DragGesture / UndoableDragGesture bases,
                                       Move, Resize, Connect, Marquee, Pan, EdgeEnd, Bend
  Components/Pages/Views/              view records the Designer markup reads (NodeView, EdgeView, GroupView, …)
  Components/Pages/Commands/           editor commands (Command pattern): IEditorCommand, EditorCommand base, KeyGesture,
                                       ICommandContext (receiver), CommandRegistry (invoker), one class per action,
                                       CommandButtons (Run / CanRun / Tip and the commands with buttons: Cmd.Save, Cmd.Delete, …)
  Components/Pages/Designer.Commands.cs        OnKey (keyboard → CommandRegistry) and the page's CommandButtons (Cmd)
  Components/Pages/Designer.CommandContext.cs  Designer's ICommandContext implementation
  Components/Shared/*.razor(.cs)       small reusable components: PanelItems / PanelItemRow (list / IPO rows in the panel),
                                       ShapeStyle / TextBoxStyle / GroupStyle, AlignBar, LinkPicker, LinkBadge (SVG, rendered
                                       inside the canvas), DropMenu, InfoTip, LaneCreator
  Components/UiSvg.cs                  IconSvg / OutcomeSwatch helpers (imported with `@using static` in _Imports.razor)
  wwwroot/js/designer.js     keyboard shortcuts, wheel zoom/pan, canvas size, focus, drag data
  wwwroot/js/export.js       PNG export: draws the SVG export onto a canvas, returns the bytes (streamed to .NET)
  wwwroot/app.css            all styling
tools/Check-OneTypePerFile.ps1   one-type-per-file check (Windows PowerShell 5.1 or PowerShell 7)
tools/regression/                side-by-side regression suites (master vs working copy) — see its README.md
```

## Code style (owner's preference — keep to it)

- **No `var`.** Write the actual type (`Node n = ...`, `foreach (Edge e in ...)`, `(double x, double y) = ...`), so code
  reads clearly when browsing. `.editorconfig` flags `var` as a warning (IDE0008). In a `foreach` over a non-null
  collection use the plain type, not `Edge?`.
- **One type per file, named after the type.** Every class, record, interface, enum and struct gets its own `.cs`
  file (`MoveGesture` → `MoveGesture.cs`). Partial classes use `Type.Area.cs` (`Designer.EditorContext.cs`).
  Private nested helpers may stay nested (`Designer.Dialog`, `PdfRenderer`'s local functions); public nested
  types get their own file. Folders match namespaces (`Models/Styles/` = `BeDiagram.Models.Styles`), with explicit
  `using`s, no global usings. Run `tools/Check-OneTypePerFile.ps1` before opening a PR (in the cloud workspace:
  download PowerShell from its GitHub releases tarball and run `pwsh -File tools/Check-OneTypePerFile.ps1`).
- **Numbers in SVG** are written with `SvgNumbers.F` (`using static BeDiagram.Models.SvgNumbers;`, already imported in
  `_Imports.razor`): two decimals, invariant culture. Don't add another local `F`. The PDF writer's `PdfSyntax.F` is
  finer on purpose.
- **`using` order:** `System` / `Microsoft` first, then `BeDiagram.*`, then `using static`, each alphabetical.
- **No C# in `.razor` files.** No `@code` blocks and no `@{ }` blocks: every `.razor` has a `.razor.cs` partial class.
  The markup only loops, branches and reads values. Anything that needs working out (layouts, paths, transforms,
  filtered lists) is a method, property or view record in the partial class. Event handlers are named methods;
  a one-line lambda that only passes an argument (`@onclick="() => Select(n.Id)"`) is fine.
  Markup that needs parameters becomes a small component in `Components/Shared`, not a `RenderFragment` in C#.

## Design rules

- **Fixed sets of values are enums, saved by name.** Node type, outcome, side, connector style, label place and
  orientation, flow direction, IPO section and emphasis level are enums (`Models/Catalogue/`, `EmphasisLevel` in
  `Models/Emphasis/`). Each carries `[JsonConverter(typeof(EnumNameConverter<T>))]`, so the file holds the camelCase
  member name (`"decision"`, `"rework"`) exactly as when these were strings, and an unknown name reads as the enum's
  first member (listed first on purpose). Optional ones (`Edge.FromSide`, `Node.Outcome`, `ListItem.Section`) put
  `OptionalEnumNameConverter<T>` on the property instead: unknown → null. When an enum value must appear as text
  (Mermaid ids, a toast, a dropdown's option values) use `EnumNames.Of(value)`, never `ToString()`. Outlines are referenced
  as objects (`NodeTypeInfo.Outline`, `OutcomeInfo.Outline` → `ShapeOutlines.Diamond`), not by name. Free-form values
  the user extends (palette colour keys, icon names, property keys) stay strings.
- **Typed nodes.** Every shape has a `Type` (start, process, decision, check, action, data, status, end, circle, outcome, list, ipo)
  and free-form `Properties`. Outcome nodes carry `Outcome` (pass, fail, rework, conditional, hold — see `Outcomes`);
  `n.Kind.OutlineOf(n)` (or `ShapeOutlines.Of(box)` for any box) / `n.Kind.IconOf(n)` turn that into the drawn outline and glyph, so use
  those rather than `NodeTypes.Get(..).Outline`. Rule-check nodes default to `ruleId`, `ifcEntity`, `property`, `operator`,
  `value` (`NodeTypeInfo.PropertyKeys`).
  This is what lets the audit tool read diagrams later — don't replace types with purely visual styling.
- **Node kinds use the Strategy pattern.** What depends on the *kind* of node (classic shape, circle, outcome, list,
  IPO box) belongs in its `INodeKind` class in `Models/Kinds/`, reached as `n.Kind` (or `NodeTypes.Get(type).Kind`):
  whether it's a step (`IsStep`), can be round (`CanBeRound`), is a panel (`IsPanel`), has an outcome (`HasOutcome`),
  its outline key, icon and icon stroke, label placement (`LabelBelow`, `LayoutLabel`), colours (`Colours`, used by
  `NodeStyle.For`), names (`NameOf`, `PanelTitle`, `NewLabel`, `RetypeLabel`) and what a new or retyped node starts with
  (`NewItems`, `Init`, `Adopt`). Don't test `n.Type == NodeTypes.Outcome` (or `Ipo`, `Circle`) elsewhere. A new per-kind
  behaviour is a new `INodeKind` member with its usual answer in `NodeKind`. Per-type *data* (icons, default
  properties, size) stays on `NodeTypeInfo` in `NodeTypes.All`.
- **One geometry, two renderers.** The canvas (the layers in Components/Canvas) and the SVG export (SvgDiagramWriter) must draw the
  same thing. Put the maths in the shared classes (`EdgeRouter`, `TextLayout`, `Boxes`, the outlines and kinds) and the markup in
  `SvgShapes`, and call them from both. Any new visual feature
  needs both renderers updated, and the Mermaid export updated where Mermaid can express it.
- **Connector routing uses the Strategy pattern.** `EdgeRouter.Route` keeps row ends on their left / right side and
  hands the rest to the `IConnectorRouting` for the connector's style (`ElbowRouting`, `StraightRouting`). A new
  connector style is a new routing class (and an `EdgeStyle` member), not a branch in `EdgeRouter`.
- **Shape outlines use the Strategy pattern.** Anything that differs per outline belongs in that outline's class in
  `Models/Shapes/`, reached through `n.Kind.OutlineOf(n)` / `ShapeOutlines.Of(box)`: path, connector clipping (`Boundary`), badge position,
  label / icon layout hints, side stripe, Mermaid syntax. Don't add `switch`es or `== "diamond"` tests on shape
  keys elsewhere. A new per-shape behaviour is a new `IShapeOutline` member (with a default in `ShapeOutline`).
- **Partial files by area.** `DiagramEditor` and `Designer` are split into `Type.Area.cs` partial files (see Layout). Put a
  new member in the file for its area, or start a new area file, rather than growing the main file.
- **Editing lives in `DiagramEditor`, the page is the view.** Anything that changes the diagram, the selection or the
  undo stack is a public method on `DiagramEditor` (no Blazor types in it — take plain values, not `ChangeEventArgs`).
  The Designer keeps only view concerns (pan/zoom, gestures, dialogs, toasts, files, export, render helpers) and passes
  input to `Editor`. The markup calls `Editor.X(...)` directly and reads the diagram as `D`. If the editor needs
  something from the screen, add it to `IEditorView` (implemented in `Designer.EditorView.cs`). New node defaults
  are data on `NodeTypeInfo` (icon, round icon, property keys, size) or behaviour on the kind (`NewLabel`, `Init`);
  `NodeFactory` puts them together.
- **The page is split into child components (Mediator).** A part of the page (a side panel, the toolbar and its menus,
  the palette, the status bar, a canvas layer) is a component that inherits `DesignerPart` and takes `Host="this"`. It reaches the
  editor as `Editor` / `D`, runs commands as `Run(Cmd.Delete)` (`CanRun`, `Tip`), and asks the page for anything else
  through `IDesignerHost` (add a member there and in `Designer.Host.cs` when a part needs more). Parts don't talk to each
  other. Blazor only re-renders the component that handled an event, so `DesignerPart` redraws the whole page after every
  event it handles: handlers don't call `StateHasChanged`, and a child's `OnChanged` is `Redraw`. Parts keep no state
  the page needs to outlive them (the icon search is bound back to the page with `@bind-IconFilter`). Dialogs are
  simpler: parameters in, `EventCallback`s out (an EventCallback made from a page method redraws the page by itself).
  Canvas layers derive from `CanvasLayer` and hand pointer presses to the page (`Input.ItemDown(e, id)`, …); the drag
  itself (pointer move / up) stays on the page's SVG, so `ShouldRender` can still skip moves that change nothing. A new
  kind of item is a new layer, placed in `Designer.razor` where it should stack. Gesture previews (live connector,
  marquee, connector handles) read the gesture, so they stay in the page.
- **Undo.** Every model change goes through `Editor.Change()` (snapshot + dirty) or, for drags, a snapshot taken at
  gesture start and pushed on pointer-up (`UndoableDragGesture` does this). Panel inputs call `Editor.Checkpoint` on focus.
- **Gestures use the State pattern.** A drag is an `IGesture` object held in `gesture` (null when idle). New drags are
  new gesture classes created from a pointer-down handler. Don't add mode enums or `switch` statements on gesture type
  in the Designer. Gestures reach the editor only through `IEditorContext`: if a gesture needs more, add it to that
  interface and `Designer.EditorContext.cs`. Canvas previews read the gesture's public properties (e.g. `Marquee`,
  `LiveConnector()`).
- **Editor actions use the Command pattern.** Every keyboard shortcut is an `IEditorCommand` in
  `Components/Pages/Commands/`, registered in `CommandRegistry.CreateDefault()`. Don't add `switch` cases on keys
  in the Designer. Toolbar and panel buttons for the same action run the command (`Run(Cmd.Xxx)`, `CanRun` for
  disabled, `Tip` for the tooltip; `CommandButtons` lists the commands that have buttons). Commands reach the editor only through `ICommandContext`. A new shortcut key
  must also be added to the key list in `designer.js`. Commands don't do their own undo: the editor actions they
  call already use `Editor.Change()`.
- **Text boxes** replace the old comments: one `TextBox` class, `IsComment` flags annotations (comment preset = `ApplyCommentStyle`, pin lines = `ApplyPinLook`). Layout in `TextLayout.Layout(TextBox)`, drawn with SVG text in both renderers.
- **Groups nest (Composite pattern).** Membership is `GroupId` on nodes, text boxes and groups (`Group` is an
  `IGroupable`), so the file stays flat. Walk the hierarchy with `GroupTree` (`Editor.Tree`, or `new GroupTree(d)` in
  exports): `Children`, `Descendants`, `Ancestors`, `IsInside`, `CommonParent`, `PaintOrder`. Don't compare `GroupId`
  by hand or loop over `Diagram.Groups` to draw — use `PaintOrder` so inner groups sit over their parents. Moving a
  group moves all its descendants (`MoveBox`); `GrowGroup` also grows every group around it. A group can't go inside
  itself or one of its own groups (`CanHold`).
- **Emphasis uses the Strategy pattern.** Anything that differs between major / normal / minor (colours via
  `Adjust`, outline width, shadow, halo ring, label weight and colour) belongs in that emphasis class, reached through
  `Emphases.Of(n)` — via `NodeStyle.For`, `SvgShapes.HaloSvg` and directly (`Emphases.Of(n).StrokeWidth`,
  `.ShadowOpacity`, `.LabelWeight(n)`, `.BelowLabelColour`). Don't test `n.Emphasis == "major"` elsewhere. Emphasis is a look only: it
  never changes size or step.
- **Swim lanes use the Strategy pattern.** A lane is a top-level group; `LaneArranger` works in lane coordinates
  (`flow` along a lane, `cross` across) and an `ILaneLayout` maps them to x / y and gives the title insets, so one
  algorithm does both directions. Columns come from `Node.Step`. It's a one-off arrange (one `Change()`), not a
  stored layout: no file-format change. `Diagram.LockLanes` stops lanes (top-level groups) growing or shrinking
  automatically (`Editor.SizeLocked`, checked in `GrowOne` / `FitGroup`); explicit sizing still works. Run from `ArrangeLanesHorizontalCommand` / `ArrangeLanesVerticalCommand`.
  Auto-layout reuses the same core (`ArrangeAll`: top-level items as one lane with no box, groups as blocks) and then
  `ConnectorTidier`, in one `Change()` (`AutoLayoutHorizontalCommand` / `AutoLayoutVerticalCommand`).
- **Tidy connectors is on request, never automatic** (Nick's call, 2026-11-06: auto re-routing would get frustrating).
  `ConnectorTidier` only picks values for the existing route fields (`FromSide` / `ToSide` / `Bend`) by trying candidates
  through `EdgeRouter.Route` and scoring them, so the renderers and file format don't change. It plans first and the
  editor calls `Change()` only when a route changes, and keeps a new route only when it scores better. Elbow connectors
  only; obstacles are shapes, text boxes and the text under shapes (`TextLayout.OutsideTextBox`).
- **Step order is data, not decoration.** Every shape except outcomes has `Node.Step` (1, 2, 3, …, no gaps; parallel
  steps share a number), whether or not `Diagram.ShowSteps` draws the badges. Change numbers only through `StepOrder`
  (`Editor.Steps`): `Normalise` / `Compact` after adding, deleting or retyping shapes, `MoveTo` / `SetParallel` /
  `MakeParallel` for edits; the step order pop-up uses `MoveBefore` (a gap between slots) and `Join` (onto a slot). The badge sits at
  `IShapeOutline.StepBadgeCentre` (default: the link badge's spot mirrored through the centre).
- **PDF is drawn from the SVG export**, so it has no geometry of its own. If a new visual feature uses an SVG
  element or attribute the PDF writer doesn't read yet (gradients, images, …), extend `PdfRenderer` (`Element`,
  `ShapePath`) as well.
- **Export uses the strategy pattern** (owner's preference — he knows it well). A new format or area is a new class
  implementing `IExportFormat` / `IExportArea`, registered in `Program.cs`; don't add format-specific export methods to
  the Designer. PNG is rasterised from the SVG export in the browser, so like PDF it has no geometry of its own.
- **Flow direction** (`Diagram.Flow`, a `FlowDirection`) maps to an `ILaneLayout` via `LaneLayouts.ForFlow`, so quick-add
  (`Editor.NextSpot`) places the next step in (flow, cross) coordinates like the lane arranger. New shapes open their
  label for editing (`AddNodeAt` → `BeginEdit`); quick-adds also pan to centre them (`IEditorView.CentreOn`).
  Quick branch (`Editor.QuickBranch`, `QuickBranchCommand`, key B) places Pass / Fail outcomes at 45° and their next
  steps in the same (flow, cross) coordinates; Pass is on the −cross side (above / left).
- **Circle versions of the classic shapes.** `Node.Round` draws start / process / decision / check / action / data /
  status / end as a circle (`ClassicKind.CanBeRound`, size `NodeTypes.RoundSize`) without changing `Type`, so the meaning
  stays data. `ClassicKind.OutlineOf` / `DrawnAsCircle` pick the circle outline, so everything else (label below, badges,
  Mermaid `(( ))`, semibold label) follows from the outline. Circle icons per type: `NodeTypeInfo.RoundIcon`. Palette keys
  are `PaletteItem`s with `Round` set (`NodeFactory.SizeFor`); panel toggle `Editor.SetRound`; `ChangeType` keeps a circle a circle.
- **Lists and IPO boxes ("panels").** `list` / `ipo` nodes keep their rows in `Node.Items` (`ListItem`: id, label,
  description, icon, IPO `Section`); IPO adds `SectionTitles` and `Stacked`. Rows have a fixed height and the node is
  always as tall as its rows (`Editor.FitPanel` after every row change, `ResizeGesture` won't go shorter via
  `n.Kind.MinSize`). Layout is a Strategy: `PanelKind.LayoutOf(n)` gives the node's `IPanelLayout` (`ListLayout`, or
  `IpoColumnsLayout` / `IpoStackedLayout` from `Node.Stacked`), which owns sizes, sections, rows, the title band and the
  SVG body; renderers draw `n.Kind.Body(n, style)`. Row editing that differs between a list and an IPO box is on the kind
  (`PanelKind.AddRow` / `Peers` / `TidyRows`); IPO section rows and titles are `Node.RowsIn` / `Node.SectionTitle`.
  The title is the node's label (laid out in the band via `TextLayout.Layout`). A connector end on a row is
  `Edge.FromItem` / `ToItem`: resolve ends with `Edge.Ends(find)` / `Editor.Ends` / `IEditorContext.EdgeEnds` (never
  `FindBox(e.From)` alone), which gives an `ItemBox` for a row (`n.Kind.EndAt`, `RowAt`, `RowPorts`); a row end only
  connects left / right (`IBox.ConnectSides`, `ItemBox.SideFacing` in `EdgeRouter.Route`). Row ids are only unique inside their
  node and survive a copy. Connectors ending on a row draw above the shapes (`Edge.EndsOnRow`). Mermaid: a subgraph
  with a node per row.
- **Connector labels** render in their own layer above shapes (canvas and export).
- **Page mode** puts the sheet at world (0,0); sizes in mm convert at 96 px/inch (`PageSetup.PxPerMm`).
- Razor gotchas: wrap SVG `<text>` in a `<g>` when it's the first element in a code block (Razor treats a bare
  `<text>` as its own tag); avoid quoted string literals inside attribute lambdas — call a method instead.

## Building and testing in Claude's cloud workspace

- `api.nuget.org` is blocked there, and .NET 10 Blazor needs the `Microsoft.AspNetCore.App.Internal.Assets` package
  from it. So:
  - **Compile check (.NET 10):** copy the repo to a temp folder, add
    `<ItemGroup><KnownAspNetCorePack Remove="Microsoft.AspNetCore.App.Internal.Assets" /></ItemGroup>` to the
    copy's csproj, and `dotnet build --source <empty folder>`. Never commit that change.
  - **Run and click-test:** build a .NET 8 copy (`apt-get install dotnet-sdk-8.0`), retarget to `net8.0`, replace
    `App.razor` with a plain .NET 8 version (no `@Assets`, `ImportMap`, `ResourcePreloader`, `ReconnectModal`),
    use `app.UseStaticFiles()` instead of `MapStaticAssets()`, drop `NotFoundPage` from Routes. App code
    (Models, Services, Designer, js, css) is copied in unchanged. Run with `Storage__Folder=/tmp/diagrams`.
  - Drive it with Playwright (pre-installed: `NODE_PATH=$(npm root -g) node test.js`), take screenshots and look
    at them. Check the browser console for errors and the exported SVG/Mermaid files (`mmdc` validates Mermaid).
  - `tools/regression/prepare-net8.sh <BeDiagram folder> <dest>` does the .NET 8 copy above in one step.
- **Regression suites:** `tools/regression/run.sh [base-ref]` builds master (or `base-ref`) and the working copy, runs
  the gesture / keyboard / all-shapes / panel suites on both, and prints `ALL THE SAME` or the steps that differ
  (full set ~15 min; run it in the background and poll, as long runs can be cut off when the workspace restarts).
  It picks the suites from the changed files (`pick-suites.py`, which prints why; anything not in its table gets the
  full set); `FULL=1` forces all, `SUITES=` / `PANELS=` choose. Commit harness changes before a picked run, or the
  harness files themselves make it a full run. Waits follow Blazor's WebSocket messages (`blazor.js`), never fixed
  pauses: new suite code does `await z.act(() => page.click(…))`. A `check:` line after a suite means a wait gave up or
  a label box never got focus — look before trusting the result. Refactors must come out ALL THE SAME. Features should differ only where
  they change things. A toast that vanishes one step apart is timing, not behaviour (rerun to confirm). When a
  feature adds a shape or a shortcut, extend `make-shapes-diagram.py` / `keyboard.js`.
- **How much regression to run** (agreed with Nick 2026-11-05 — he tests every merge himself and reports problems;
  since 2026-11-10 `run.sh` applies this itself from the changed files):
  - **Full run** (all four suites) for refactors (no behaviour change allowed), and for any change to shared code:
    `Models/Routing`, `Models/Text`, `Boxes`, `Rendering`, the exporters (`Services/Export/`, including `Pdf/`), `DiagramEditor` beyond a new method or
    two, the canvas markup in `Designer.razor` or `Components/Canvas`, or `CommandRegistry` / `designer.js` keys.
  - **Small features and fixes** that touch only a few classes: a hands-on Playwright test of the change itself, plus
    only the suites that cover the area touched, e.g. `SUITES="panel" tools/regression/run.sh` —
    `gestures` for dragging, moving, resizing, connecting; `keyboard` for shortcuts, commands, toolbar buttons;
    `all-shapes` for how things are drawn and exported; `panel` for the side panel. If unsure, run the full set.
  - Say in the PR which suites ran and why the others were skipped.
  - Problems Nick finds after a merge come back as their own `fix/` PRs; note them in the PR as found after merge.
- Tell the user plainly that the .NET 10 build was compile-checked but click-tested on .NET 8.

## Backlog (rough priority)

1. Connector routing: *Tidy* (on request) is in; live routing deliberately not wanted.
2. Auto-layout: by step number is in (Diagram panel). MSAGL only if a general graph layout is ever needed.
3. Mermaid import.
4. Later: visual rule builder for the Data Audit Tools rules engine (Excel template stays a valid source).
