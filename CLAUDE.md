# BeDiagram — notes for Claude

Local, self-hosted diagramming tool (flowcharts / workflows) built with .NET 10 Blazor Server.
Owner: Nick (shnicko). Diagrams are saved as JSON files on his machine — no cloud, no database.
Longer term it becomes a feature of the Data Audit Tools project (visual rule builder, BCF issue workflows),
so the file format must stay meaningful to code, not just pretty.

## Working agreement

- One feature or fix per branch and pull request, into `master` (the default branch). Never push to `master` directly.
- Branch names: `feature/<short-name>`, `fix/<short-name>`, or `style/<area>` for a style cleanup Nick asked for.
- Keep PRs reviewable: say what changed, how it was tested, and anything Nick should check by eye.
- Update `README.md` (usage, file format and project layout sections) whenever a user-visible feature or the
  folder layout changes.
- **Old diagram files must keep opening**, but no file version numbers or migration code unless Nick asks. New
  fields are optional with a sensible default; renamed enum members keep their saved name.
- Nick opens the solution in Visual Studio and pulls `master` after merging — don't commit build output or IDE files.

## Layout

`README.md` → "Project layout" has the fuller map; use `ls` for the class names in a folder.

```
BeDiagram/
  Models/                 the diagram model (Diagram, Node, Edge, TextBox, Group, PageSetup), plus Boxes (hit tests,
                          bounds), GroupTree, StepOrder, NodeFactory, SvgNumbers, Icons (generated Lucide markup)
  Models/Catalogue/       the enums for fixed value sets and their tables (NodeTypes, Outcomes, Sides, …)
  Models/Serialization/   enums saved by name (EnumNames, EnumNameConverter, OptionalEnumNameConverter)
  Models/Kinds/           node kinds — Strategy (classic, circle, outcome, list, IPO)
  Models/Shapes/          shape outlines — Strategy (one *Outline class each, ShapeOutlines holds the instances), Paths
  Models/Panels/          list / IPO layouts — Strategy
  Models/Emphasis/        major / normal / minor looks — Strategy
  Models/Routing/         connector routes (EdgeRouter → ElbowRouting / StraightRouting — Strategy), EdgeLabels
  Models/Text/            TextLayout: labels, descriptions, text boxes, wrapping
  Models/Styles/          looks and colours
  Models/Layout/          geometry results shared by canvas and export (EdgeRoute, LabelLayout, Rect, …)
  Rendering/              SvgShapes: SVG markup shared by canvas and export
  Editing/                DiagramEditor (state, lookups, undo) + DiagramEditor.<Area>.cs partials holding every
                          editing action; IEditorView; AlignOp
  Editing/Lanes/          swim lanes and auto-layout — Strategy (horizontal / vertical)
  Editing/Routing/        Tidy connectors (ConnectorTidier → TidyPlan)
  Editing/Angles/         angle lock — Strategy (free / 90 / 45 / 30)
  Services/               DiagramStore (save / open / list / delete; a name is its `/`-separated path), ExportView
  Services/Export/        export — Strategy (IExportFormat × IExportArea, ExportService), SvgDiagramWriter, Mermaid
  Services/Export/Pdf/    PDF made from the SVG export, no libraries
  Components/Pages/       Designer.razor (markup) + Designer.<Area>.cs partials; IDesignerHost / DesignerPart
                          (Mediator); ICanvasInput
  Components/Pages/Gestures/   pointer drags — State
  Components/Pages/Commands/   editor actions and shortcuts — Command (CommandRegistry, CommandButtons)
  Components/Pages/Views/      view records the markup reads
  Components/Canvas/      canvas layers, in drawing order: PageSheet, PageHeaderLayer, GroupLayer, TextBoxLayer, EdgeLayer, NodeLayer,
                          EdgeLayer RowLayer=true, EdgeLabelLayer
  Components/Panels/      the side panel, one component per kind of selection
  Components/Bars/        toolbar and menus, shape palette, status bar
  Components/Dialogs/     pop-ups (parameters in, EventCallbacks out)
  Components/Shared/      small reusable components
  wwwroot/js/             designer.js (keys, wheel, focus, drag data), export.js (PNG rasterising)
  wwwroot/app.css         all styling
tools/Check-OneTypePerFile.ps1
tools/regression/         side-by-side regression suites — see its README.md and CLAUDE.md
```

## Code style (Nick's preference — keep to it)

Nick reads explicit, step-by-step code more easily than compact modern C#. Prefer clarity and debuggability over
brevity.

**Types and files**
- **No `var`.** Write the actual type (`Node n = ...`, `foreach (Edge e in ...)`, `(double x, double y) = ...`). The only
  exception is an anonymous type, which has no name to write. `.editorconfig` flags `var` (IDE0008). In a `foreach`
  over a non-null collection use the plain type, not `Edge?`.
- **Write `new` with the type:** `List<Node> result = new List<Node>();`, not `new()`.
- **One type per file, named after the type.** Every class, record, interface, enum and struct gets its own `.cs`
  file (`MoveGesture` → `MoveGesture.cs`). Partial classes use `Type.Area.cs` (`Designer.EditorContext.cs`).
  Private nested helpers may stay nested (`Designer.Dialog`, `PdfRenderer`'s local functions); public nested
  types get their own file. Folders match namespaces (`Models/Styles/` = `BeDiagram.Models.Styles`). Run
  `tools/Check-OneTypePerFile.ps1` before opening a PR (in the cloud workspace: download PowerShell from its GitHub
  releases tarball and run `pwsh -File tools/Check-OneTypePerFile.ps1`).
- **`using`s:** write the ones each file needs. The project has `ImplicitUsings` on (the standard `System.*` set), but
  don't add `global using`s of our own. Order: `System` / `Microsoft` first, then `BeDiagram.*`, then `using static`,
  each alphabetical.
- **Numbers in SVG** are written with `SvgNumbers.F` (`using static BeDiagram.Models.SvgNumbers;`, already imported in
  `_Imports.razor`): two decimals, invariant culture. Don't add another local `F`. The PDF writer's `PdfSyntax.F` is
  finer on purpose.

**Members**
- Full bodies with braces and an explicit `return`. No expression-bodied members (`=>`) at all: methods, properties,
  constructors, indexers, operators and local functions all get block bodies, including one-line getters.
- Lambdas passed as arguments are fine (`list.Count(x => x.IsSelected)`, `@onclick="() => Select(n.Id)"`).

**Collections**
- Use `foreach` / `for` loops with `if` statements instead of LINQ.
- Return concrete collections (e.g. `List<T>`) built inside the method, not lazily evaluated `IEnumerable<T>`.
- Exception: **one** simple LINQ call is fine when a loop would be much longer (`list.Count(x => x.IsSelected)`).
  No chains.

**Branching and patterns**
- `switch` **statements** and `if` / `else` are fine. No `switch` expressions.
- Pattern matching: `is Type t`, `is null` and `is not null` only. No property patterns (`is { } x`, `is { Count: 0 }`),
  `and` / `or` patterns or list patterns.

**Example of preferred style**

```csharp
// Preferred
public List<IGroupable> Children(Group g)
{
    List<IGroupable> result = new List<IGroupable>();
    foreach (Node n in d.Nodes)
    {
        if (n.GroupId == g.Id)
        {
            result.Add(n);
        }
    }
    return result;
}

// Avoid
public IEnumerable<IGroupable> Children(Group g) =>
    d.Nodes.Where(n => n.GroupId == g.Id).Cast<IGroupable>();
```

**Comments**
- Add a brief comment above any non-obvious block explaining what it does.

**Razor**
- **No C# in `.razor` files.** No `@code` blocks and no `@{ }` blocks: every `.razor` has a `.razor.cs` partial class.
  The markup only loops, branches and reads values. Anything that needs working out (layouts, paths, transforms,
  filtered lists) is a method, property or view record in the partial class. Event handlers are named methods;
  a one-line lambda that only passes an argument is fine.
  Markup that needs parameters becomes a small component in `Components/Shared`, not a `RenderFragment` in C#.
- Gotchas: wrap SVG `<text>` in a `<g>` when it's the first element in a code block (Razor treats a bare
  `<text>` as its own tag); avoid quoted string literals inside attribute lambdas — call a method instead.

**Bringing old code into line (gradually)**

Much of the existing code predates these rules (`=>` members, LINQ chains, switch expressions). Don't copy its
style: new code always follows the rules above. Old code is converted a piece at a time, only where a feature or fix
is already working:
- **Methods you change:** rewrite the whole method in this style, not just the lines the feature needs.
- **Whole file:** also convert the rest of the file when it is small (roughly under 150 lines) or the change already
  touches most of it. In bigger files, convert only the methods you change.
- **Files you aren't otherwise changing:** leave them alone. No drive-by clean-ups, so each PR stays about one thing.
- **Separate commit:** put the style conversion in its own commit, before the feature commit, named
  `Style: <file or area>`. Nick can then read the feature diff on its own.
- **No behaviour change.** A style conversion is a refactor. Watch the traps when replacing LINQ: a lazy
  `IEnumerable` read later sees later changes, a `List` built up front doesn't; `First()` throws where a loop may
  quietly find nothing; `Any()` / `First()` stop early. Keep the same result, including what happens when nothing is found.
- **Regression:** the conversion counts towards the regression rules below. If it touches shared code, that means a
  full run, and the only differences must be the ones the feature itself makes.
- **PR:** list which files or methods were converted, under their own heading.
- A cleanup PR for a whole area (`style/<area>`, full regression run, must be ALL THE SAME) only when Nick asks.

## Design rules

- **Switch first, Strategy when it grows.** A small `switch` statement (or `if` / `else`) is fine — not every switch
  needs to become a pattern. Move to a Strategy only when the switch gets large: roughly more than 4–5 cases, cases
  with real logic (more than a few lines each), or the same switch on the same value showing up in more than one
  place. When you do convert one, say why in the PR. Where a Strategy **already exists** for that value (node kinds,
  shape outlines, emphasis, panel layouts, connector routing, lanes, angle locks, gestures, commands, export), put the
  new behaviour in it and don't switch on that value elsewhere.
- **Fixed sets of values are enums, saved by name.** Node type, outcome, side, connector style, label place and
  orientation, flow direction, IPO section and emphasis level are enums (`Models/Catalogue/`, `EmphasisLevel` in
  `Models/Emphasis/`). Each carries `[JsonConverter(typeof(EnumNameConverter<T>))]`, so the file holds the camelCase
  member name (`"decision"`, `"rework"`) exactly as when these were strings, and an unknown name reads as the enum's
  first member (listed first on purpose). Optional ones (`Edge.FromSide`, `Node.Outcome`, `ListItem.Section`) put
  `OptionalEnumNameConverter<T>` on the property instead: unknown → null. When an enum value must appear as text
  (Mermaid ids, a toast, a dropdown's option values) use `EnumNames.Of(value)`, never `ToString()`. Free-form values
  the user extends (palette colour keys, icon names, property keys) stay strings.
- **Typed nodes.** Every shape has a `Type` (start, process, decision, check, action, data, status, end, circle, outcome,
  list, ipo) and free-form `Properties`. Outcome nodes carry `Outcome` (pass, fail, rework, conditional, hold — see
  `Outcomes`). Rule-check nodes default to `ruleId`, `ifcEntity`, `property`, `operator`, `value`
  (`NodeTypeInfo.PropertyKeys`). This is what lets the audit tool read diagrams later — don't replace types with
  purely visual styling.
- **Node kinds use the Strategy pattern.** What depends on the *kind* of node (classic shape, circle, outcome, list,
  IPO box) belongs in its `INodeKind` class in `Models/Kinds/`, reached as `n.Kind` (or `NodeTypes.Get(type).Kind`):
  whether it's a step (`IsStep`), can be round (`CanBeRound`), is a panel (`IsPanel`), has an outcome (`HasOutcome`),
  its outline (`OutlineOf`), icon (`IconOf`) and icon stroke, label placement (`LabelBelow`, `LayoutLabel`), colours
  (`Colours`, used by `NodeStyle.For`), names (`NameOf`, `PanelTitle`, `NewLabel`, `RetypeLabel`) and what a new or
  retyped node starts with (`NewItems`, `Init`, `Adopt`). Don't test `n.Type == NodeType.Outcome` (or `Ipo`, `Circle`)
  elsewhere. A new per-kind behaviour is a new `INodeKind` member with its usual answer in `NodeKind`. Per-type *data*
  (icons, default properties, size, the default outline) stays on `NodeTypeInfo` in `NodeTypes.All`.
- **Shape outlines use the Strategy pattern.** Outlines are objects (`ShapeOutlines.Diamond`), never names. Get a
  node's drawn outline with `n.Kind.OutlineOf(n)`, or `ShapeOutlines.Of(box)` for any box — not
  `NodeTypes.Get(..).Outline`, which ignores circles and outcomes. Anything that differs per outline belongs in that
  outline's class in `Models/Shapes/`: path, connector clipping (`Boundary`), badge positions, label / icon layout
  hints, side stripe, Mermaid syntax. A new per-shape behaviour is a new `IShapeOutline` member (with a default in
  `ShapeOutline`); don't switch on the outline elsewhere.
- **One geometry, two renderers.** The canvas (the layers in `Components/Canvas`) and the SVG export
  (`SvgDiagramWriter`) must draw the same thing. Put the maths in the shared classes (`EdgeRouter`, `TextLayout`,
  `Boxes`, the outlines and kinds) and the markup in `SvgShapes`, and call them from both. Any new visual feature needs
  both renderers updated, and the Mermaid export updated where Mermaid can express it.
- **PDF and PNG are drawn from the SVG export**, so they have no geometry of their own. If a new visual feature uses
  an SVG element or attribute the PDF writer doesn't read yet (gradients, images, …), extend `PdfRenderer` (`Element`,
  `ShapePath`) as well.
- **Export uses the Strategy pattern** (Nick knows it well). A new format or area is a new class implementing
  `IExportFormat` / `IExportArea`, registered in `Program.cs`; don't add format-specific export methods to the Designer.
- **Connector routing uses the Strategy pattern.** `EdgeRouter.Route` keeps row ends on their left / right side and
  hands the rest to the `IConnectorRouting` for the connector's style (`ElbowRouting`, `StraightRouting`). A new
  connector style is a new routing class (and an `EdgeStyle` member), not a branch in `EdgeRouter`.
- **Partial files by area.** `DiagramEditor` and `Designer` are split into `Type.Area.cs` partial files. Put a new
  member in the file for its area, or start a new area file, rather than growing the main file.
- **Editing lives in `DiagramEditor`, the page is the view.** Anything that changes the diagram, the selection or the
  undo stack is a public method on `DiagramEditor` (no Blazor types in it — take plain values, not `ChangeEventArgs`).
  The Designer keeps only view concerns (pan/zoom, gestures, dialogs, toasts, files, export, render helpers) and passes
  input to `Editor`. The markup calls `Editor.X(...)` directly and reads the diagram as `D`. If the editor needs
  something from the screen, add it to `IEditorView` (implemented in `Designer.EditorView.cs`). New node defaults
  are data on `NodeTypeInfo` (icon, round icon, property keys, size) or behaviour on the kind (`NewLabel`, `Init`);
  `NodeFactory` puts them together.
- **The page is split into child components (Mediator).** A part of the page (a side panel, the toolbar and its menus,
  the palette, the status bar, a canvas layer) is a component that inherits `DesignerPart` and takes `Host="this"`. It
  reaches the editor as `Editor` / `D`, runs commands as `Run(Cmd.Delete)` (`CanRun`, `Tip`), and asks the page for
  anything else through `IDesignerHost` (add a member there and in `Designer.Host.cs` when a part needs more). Parts
  don't talk to each other. Blazor only re-renders the component that handled an event, so `DesignerPart` redraws the
  whole page after every event it handles: handlers don't call `StateHasChanged`, and a child's `OnChanged` is
  `Redraw`. Parts keep no state the page needs to outlive them (the icon search is bound back to the page with
  `@bind-IconFilter`). Dialogs are simpler: parameters in, `EventCallback`s out (an EventCallback made from a page
  method redraws the page by itself). Canvas layers derive from `CanvasLayer` and hand pointer presses to the page
  (`Input.ItemDown(e, id)`, …); the drag itself (pointer move / up) stays on the page's SVG, so `ShouldRender` can
  still skip moves that change nothing. A new kind of item is a new layer, placed in `Designer.razor` where it should
  stack. Gesture previews (live connector, marquee, connector handles) read the gesture, so they stay in the page.
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
  disabled, `Tip` for the tooltip; `CommandButtons` lists the commands that have buttons). Commands reach the editor
  only through `ICommandContext`. A new shortcut key must also be added to the key list in `designer.js`. Commands
  don't do their own undo: the editor actions they call already use `Editor.Change()`.
- **Emphasis uses the Strategy pattern.** Anything that differs between major / normal / minor (colours via
  `Adjust`, outline width, shadow, halo ring, label weight and colour) belongs in that emphasis class, reached through
  `Emphases.Of(n)` — via `NodeStyle.For`, `SvgShapes.HaloSvg` and directly (`Emphases.Of(n).StrokeWidth`,
  `.ShadowOpacity`, `.LabelWeight(n)`, `.BelowLabelColour`). Don't test `n.Emphasis == EmphasisLevel.Major` elsewhere.
  Emphasis is a look only: it never changes size or step.
- **Text boxes** replace the old comments (`LegacyComment` only reads old files): one `TextBox` class, `IsComment`
  flags annotations (comment preset = `ApplyCommentStyle`, pin lines = `ApplyPinLook`). Layout in
  `TextLayout.Layout(TextBox)`, drawn with SVG text in both renderers.
- **Groups nest (Composite pattern).** Membership is `GroupId` on nodes, text boxes and groups (`Group` is an
  `IGroupable`), so the file stays flat. Walk the hierarchy with `GroupTree` (`Editor.Tree`, or `new GroupTree(d)` in
  exports): `Children`, `Descendants`, `Ancestors`, `IsInside`, `CommonParent`, `PaintOrder`. Don't compare `GroupId`
  by hand or loop over `Diagram.Groups` to draw — use `PaintOrder` so inner groups sit over their parents. Moving a
  group moves all its descendants (`MoveBox`); `GrowGroup` also grows every group around it. A group can't go inside
  itself or one of its own groups (`CanHold`).
- **Swim lanes use the Strategy pattern.** A lane is a top-level group; `LaneArranger` works in lane coordinates
  (`flow` along a lane, `cross` across) and an `ILaneLayout` maps them to x / y and gives the title insets, so one
  algorithm does both directions. Columns come from `Node.Step`. It's a one-off arrange (one `Change()`), not a
  stored layout: no file-format change. `Diagram.LockLanes` stops lanes (top-level groups) growing or shrinking
  automatically (`Editor.SizeLocked`, checked in `GrowOne` / `FitGroup`); explicit sizing still works. Run from
  `ArrangeLanesHorizontalCommand` / `ArrangeLanesVerticalCommand`. Auto-layout reuses the same core (`ArrangeAll`:
  top-level items as one lane with no box, groups as blocks) and then `ConnectorTidier`, in one `Change()`
  (`AutoLayoutHorizontalCommand` / `AutoLayoutVerticalCommand`).
- **Tidy connectors is on request, never automatic** (Nick's call, 2026-11-06: auto re-routing would get frustrating).
  `ConnectorTidier` only picks values for the existing route fields (`FromSide` / `ToSide` / `Bend`) by trying candidates
  through `EdgeRouter.Route` and scoring them, so the renderers and file format don't change. It plans first and the
  editor calls `Change()` only when a route changes, and keeps a new route only when it scores better. Elbow connectors
  only; obstacles are shapes, text boxes and the text under shapes (`TextLayout.OutsideTextBox`).
- **Step order is data, not decoration.** Every shape except outcomes has `Node.Step` (1, 2, 3, …, no gaps; parallel
  steps share a number), whether or not `Diagram.ShowSteps` draws the badges. Change numbers only through `StepOrder`
  (`Editor.Steps`): `Normalise` / `Compact` after adding, deleting or retyping shapes, `MoveTo` / `SetParallel` /
  `MakeParallel` for edits; the step order pop-up uses `MoveBefore` (a gap between slots) and `Join` (onto a slot).
  The badge sits at `IShapeOutline.StepBadgeCentre` (default: the link badge's spot mirrored through the centre).
- **Flow direction** (`Diagram.Flow`, a `FlowDirection`) maps to an `ILaneLayout` via `LaneLayouts.ForFlow`, so quick-add
  (`Editor.NextSpot`) places the next step in (flow, cross) coordinates like the lane arranger. New shapes open their
  label for editing (`AddNodeAt` → `BeginEdit`); quick-adds also pan to centre them (`IEditorView.CentreOn`).
  Quick branch (`Editor.QuickBranch`, `QuickBranchCommand`, key B) places Pass / Fail outcomes at 45° and their next
  steps in the same (flow, cross) coordinates; Pass is on the −cross side (above / left).
- **Circle versions of the classic shapes.** `Node.Round` draws start / process / decision / check / action / data /
  status / end as a circle (`ClassicKind.CanBeRound`, size `NodeTypes.RoundSize`) without changing `Type`, so the
  meaning stays data. `ClassicKind.OutlineOf` / `DrawnAsCircle` pick the circle outline, so everything else (label
  below, badges, Mermaid `(( ))`, semibold label) follows from the outline. Circle icons per type:
  `NodeTypeInfo.RoundIcon`. Palette keys are `PaletteItem`s with `Round` set (`NodeFactory.SizeFor`); panel toggle
  `Editor.SetRound`; `ChangeType` keeps a circle a circle.
- **Lists and IPO boxes ("panels").** `list` / `ipo` nodes keep their rows in `Node.Items` (`ListItem`: id, label,
  description, icon, IPO `Section`); IPO adds `SectionTitles` and `Stacked`. Rows have a fixed height and the node is
  always as tall as its rows (`Editor.FitPanel` after every row change, `ResizeGesture` won't go shorter via
  `n.Kind.MinSize`). Layout is a Strategy: `PanelKind.LayoutOf(n)` gives the node's `IPanelLayout` (`ListLayout`, or
  `IpoColumnsLayout` / `IpoStackedLayout` from `Node.Stacked`), which owns sizes, sections, rows, the title band and the
  SVG body; renderers draw `n.Kind.Body(n, style)`. Row editing that differs between a list and an IPO box is on the
  kind (`PanelKind.AddRow` / `Peers` / `TidyRows`); IPO section rows and titles are `Node.RowsIn` / `Node.SectionTitle`.
  The title is the node's label (laid out in the band via `TextLayout.Layout`). A connector end on a row is
  `Edge.FromItem` / `ToItem`: resolve ends with `Edge.Ends(find)` / `Editor.Ends` / `IEditorContext.EdgeEnds` (never
  `FindBox(e.From)` alone), which gives an `ItemBox` for a row (`n.Kind.EndAt`, `RowAt`, `RowPorts`); a row end only
  connects left / right (`IBox.ConnectSides`, `ItemBox.SideFacing` in `EdgeRouter.Route`). Row ids are only unique
  inside their node and survive a copy. Connectors ending on a row draw above the shapes (`Edge.EndsOnRow`). Mermaid: a
  subgraph with a node per row.
- **Connector labels** render in their own layer above shapes (canvas and export).
- **Page mode** puts the sheet at world (0,0); sizes in mm convert at 96 px/inch (`PageSetup.PxPerMm`).
- **Page title and description** (`Diagram.Title` / `Description` / `HideHeader`) are data on the diagram, not items:
  `PageHeader` lays them out as text boxes made from `PageHeader.TitleLook(d)` / `DescriptionLook(d)` (looks only;
  null on the diagram = the starting look, `Editor.HeaderLook` creates one for the style controls), so they are
  never in `Diagram.TextBoxes` and selection, layout and export scaling leave them alone. Anything that places the
  drawing on a page uses `PageHeader.DrawingArea(d)` (inside the margins, below the header), not the raw margins.

## Building and testing in Claude's cloud workspace

- `api.nuget.org` is blocked there, and .NET 10 Blazor needs the `Microsoft.AspNetCore.App.Internal.Assets` package
  from it. So:
  - **Compile check (.NET 10):** copy the repo to a temp folder, add
    `<ItemGroup><KnownAspNetCorePack Remove="Microsoft.AspNetCore.App.Internal.Assets" /></ItemGroup>` to the
    copy's csproj, and `dotnet build --source <empty folder>`. Never commit that change.
  - **Run and click-test on .NET 8:** `tools/regression/prepare-net8.sh <BeDiagram folder> <dest>` makes the .NET 8
    copy (details in `tools/regression/CLAUDE.md`). Run with `Storage__Folder=/tmp/diagrams` and drive it with
    Playwright (pre-installed: `NODE_PATH=$(npm root -g) node test.js`). Take screenshots and look at them; check the
    browser console and the exported SVG / Mermaid files (`mmdc` validates Mermaid).
  - Tell Nick plainly that the .NET 10 build was compile-checked but click-tested on .NET 8.
- **Regression suites:** `tools/regression/run.sh [base-ref]` builds master (or `base-ref`) and the working copy, runs
  the suites on both, and prints `ALL THE SAME` or the steps that differ. A full run takes ~15 min: run it in the
  background and poll. Refactors must come out ALL THE SAME; features should differ only where they change things.
  How to read and extend the suites: `tools/regression/CLAUDE.md`.
- **How much regression to run** (agreed with Nick 2026-11-05 — he tests every merge himself and reports problems;
  `run.sh` picks the suites from the changed files):
  - **Full run** (all four suites) for refactors (no behaviour change allowed), and for any change to shared code:
    `Models/Routing`, `Models/Text`, `Boxes`, `Rendering`, the exporters (`Services/Export/`, including `Pdf/`),
    `DiagramEditor` beyond a new method or two, the canvas markup in `Designer.razor` or `Components/Canvas`, or
    `CommandRegistry` / `designer.js` keys.
  - **Small features and fixes** that touch only a few classes: a hands-on Playwright test of the change itself, plus
    only the suites that cover the area touched, e.g. `SUITES="panel" tools/regression/run.sh` —
    `gestures` for dragging, moving, resizing, connecting; `keyboard` for shortcuts, commands, toolbar buttons;
    `all-shapes` for how things are drawn and exported; `panel` for the side panel. If unsure, run the full set.
  - **Docs only** (`*.md`): nothing to run.
  - Say in the PR which suites ran and why the others were skipped.
  - Problems Nick finds after a merge come back as their own `fix/` PRs; note them in the PR as found after merge.

Roadmap and backlog live in the Data Audit Tools project docs, not here.
