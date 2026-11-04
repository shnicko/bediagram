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
  Models/Geometry.cs     shape outlines, connector routing (elbow/straight), label + icon layout, bounds
  Models/Catalogue/      type tables (BeDiagram.Models.Catalogue): NodeTypes, NodeTypeInfo, Outcomes, OutcomeInfo, EdgeStyles,
                         Sides, LabelPlaces, LabelOrients
  Models/Styles/         looks and colours (BeDiagram.Models.Styles): Palette, Swatch, TextPalette, LineLook, TextLook,
                         GroupLook, NodeStyle, TextStyle
  Models/Layout/         geometry results (BeDiagram.Models.Layout): EdgeRoute, EdgeGeom, LabelLayout, TextBoxLayout, TextLine,
                         EdgeLabelLayout, EdgeLabelLine, Rect
  Models/Shapes/         shape outlines (Strategy pattern, BeDiagram.Models.Shapes): IShapeOutline, ShapeOutline base,
                         one class per outline (Rect, Check, RoundedRect, Pill, Diamond, Parallelogram, Hexagon, Circle,
                         Seal, Octagon, Triangle, RoundedSquare), ShapeOutlines (lookup by shape key)
  Models/Icons.cs        generated Lucide icon markup (ISC) — picker categories + a few UI icons
  Services/DiagramStore.cs   save/open/list/delete *.diagram.json in the storage folder
  Services/Exporters.cs      SVG export (page-sized in mm when a page is set, or an ExportView window) and Mermaid export
  Services/ExportView.cs     the window onto the drawing that a current-view export uses
  Services/PdfExporter.cs    PDF = Exporters.ToSvg output converted to PDF operators (own SVG-subset reader, core fonts)
  Services/Export/           export strategies: IExportFormat (Svg/Png/Pdf/Mermaid), IExportArea (WholeDrawing/CurrentView),
                             ExportService (context: lists menu choices, renders + saves), IPngRasterizer (BrowserPngRasterizer)
  Components/Pages/Designer.razor      the whole editor UI (toolbar, palette, SVG canvas, panel, dialogs) — markup only
  Components/Pages/Designer.razor.cs   editor state + interaction logic + render helpers (view records the markup reads)
  Components/Pages/Designer.EditorContext.cs   Designer's IEditorContext implementation (what gestures may use)
  Components/Pages/Gestures/           mouse gestures (State pattern): IGesture, DragGesture / UndoableDragGesture bases,
                                       Move, Resize, Connect, Marquee, Pan, EdgeEnd, Bend
  Components/Pages/Views/              view records the Designer markup reads (NodeView, EdgeView, GroupView, …)
  Components/Pages/Commands/           editor commands (Command pattern): IEditorCommand, EditorCommand base, KeyGesture,
                                       ICommandContext (receiver), CommandRegistry (invoker), one class per action
  Components/Pages/Designer.Commands.cs        OnKey (keyboard → CommandRegistry) and Run / CanRun / Tip for buttons
  Components/Pages/Designer.CommandContext.cs  Designer's ICommandContext implementation
  Components/Shared/*.razor(.cs)       small child components: AlignBar, LinkPicker, LinkBadge (SVG, rendered inside the canvas)
  Components/UiSvg.cs                  IconSvg / OutcomeSwatch helpers (imported with `@using static` in _Imports.razor)
  Components/AlignOp.cs                align / distribute / row / column operations
  wwwroot/js/designer.js     keyboard shortcuts, wheel zoom/pan, canvas size, focus, drag data
  wwwroot/js/export.js       PNG export: draws the SVG export onto a canvas, returns the bytes (streamed to .NET)
  wwwroot/app.css            all styling
tools/Check-OneTypePerFile.ps1   one-type-per-file check (Windows PowerShell 5.1 or PowerShell 7)
```

## Code style (owner's preference — keep to it)

- **No `var`.** Write the actual type (`Node n = ...`, `foreach (Edge e in ...)`, `(double x, double y) = ...`), so code
  reads clearly when browsing. `.editorconfig` flags `var` as a warning (IDE0008). In a `foreach` over a non-null
  collection use the plain type, not `Edge?`.
- **One type per file, named after the type.** Every class, record, interface, enum and struct gets its own `.cs`
  file (`MoveGesture` → `MoveGesture.cs`). Partial classes use `Type.Area.cs` (`Designer.EditorContext.cs`).
  Private nested helpers may stay nested (`Designer.Dialog`, the parser classes inside `PdfExporter`); public nested
  types get their own file. Folders match namespaces (`Models/Styles/` = `BeDiagram.Models.Styles`), with explicit
  `using`s, no global usings. Run `tools/Check-OneTypePerFile.ps1` before opening a PR (in the cloud workspace:
  download PowerShell from its GitHub releases tarball and run `pwsh -File tools/Check-OneTypePerFile.ps1`).
- **No C# in `.razor` files.** No `@code` blocks and no `@{ }` blocks: every `.razor` has a `.razor.cs` partial class.
  The markup only loops, branches and reads values. Anything that needs working out (layouts, paths, transforms,
  filtered lists) is a method, property or view record in the partial class. Event handlers are named methods;
  a one-line lambda that only passes an argument (`@onclick="() => Select(n.Id)"`) is fine.
  Markup that needs parameters becomes a small component in `Components/Shared`, not a `RenderFragment` in C#.

## Design rules

- **Typed nodes.** Every shape has a `Type` (start, process, decision, check, action, data, status, end, circle, outcome)
  and free-form `Properties`. Outcome nodes carry `Outcome` (pass, fail, rework, conditional, hold — see `Outcomes`);
  `Geometry.ShapeOf`/`OutlineOf`/`IconOf` turn that into the drawn outline and glyph, so use those rather than `NodeTypes.Get(..).Shape`. Rule-check nodes default to `ruleId`, `ifcEntity`, `property`, `operator`, `value`.
  This is what lets the audit tool read diagrams later — don't replace types with purely visual styling.
- **One geometry, two renderers.** The canvas (Designer.razor) and the SVG export (Exporters.cs) must draw the
  same thing. Put shape/connector/label maths in `Geometry.cs` and call it from both. Any new visual feature
  needs both renderers updated, and the Mermaid export updated where Mermaid can express it.
- **Shape outlines use the Strategy pattern.** Anything that differs per outline belongs in that outline's class in
  `Models/Shapes/`, reached through `Geometry.OutlineOf(n)`: path, connector clipping (`Boundary`), badge position,
  label / icon layout hints, side stripe, Mermaid syntax. Don't add `switch`es or `== "diamond"` tests on shape
  keys elsewhere. A new per-shape behaviour is a new `IShapeOutline` member (with a default in `ShapeOutline`).
- **Undo.** Every model change goes through `Change()` (snapshot + dirty) or, for drags, a snapshot taken at
  gesture start and pushed on pointer-up (`UndoableDragGesture` does this). Panel inputs call `Checkpoint` on focus.
- **Gestures use the State pattern.** A drag is an `IGesture` object held in `gesture` (null when idle). New drags are
  new gesture classes created from a pointer-down handler. Don't add mode enums or `switch` statements on gesture type
  in the Designer. Gestures reach the editor only through `IEditorContext`: if a gesture needs more, add it to that
  interface and `Designer.EditorContext.cs`. Canvas previews read the gesture's public properties (e.g. `Marquee`,
  `LiveConnector()`).
- **Editor actions use the Command pattern.** Every keyboard shortcut is an `IEditorCommand` in
  `Components/Pages/Commands/`, registered in `CommandRegistry.CreateDefault()`. Don't add `switch` cases on keys
  in the Designer. Toolbar and panel buttons for the same action run the command (`Run(XxxCmd)`, `CanRun` for
  disabled, `Tip` for the tooltip). Commands reach the editor only through `ICommandContext`. A new shortcut key
  must also be added to the key list in `designer.js`. Commands don't do their own undo: the editor actions they
  call already use `Change()`.
- **Text boxes** replace the old comments: one `TextBox` class, `IsComment` flags annotations (comment preset = `ApplyCommentStyle`, pin lines = `ApplyPinLook`). Layout in `Geometry.Layout(TextBox)`, drawn with SVG text in both renderers.
- **Groups** don't nest. Membership is `GroupId` on nodes/text boxes; moving a group moves its members (`MoveBox`).
- **PDF is drawn from the SVG export**, so it has no geometry of its own. If a new visual feature uses an SVG
  element or attribute `PdfExporter` doesn't read yet (gradients, images, …), extend `PdfExporter` as well.
- **Export uses the strategy pattern** (owner's preference — he knows it well). A new format or area is a new class
  implementing `IExportFormat` / `IExportArea`, registered in `Program.cs`; don't add format-specific export methods to
  the Designer. PNG is rasterised from the SVG export in the browser, so like PDF it has no geometry of its own.
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
- Tell the user plainly that the .NET 10 build was compile-checked but click-tested on .NET 8.

## Backlog (rough priority)

1. Connector routing around shapes (lines currently pass behind obstacles).
2. Auto-layout ("tidy") — candidate: MSAGL (Microsoft Automatic Graph Layout, MIT).
3. Nested groups.
4. Mermaid import.
5. Later: visual rule builder for the Data Audit Tools rules engine (Excel template stays a valid source).
