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
  Models/Diagram.cs      data model: Diagram, Node, Edge, Comment, Group, PageSetup, NodeTypes, Palette, LineLook
  Models/Geometry.cs     shape outlines, connector routing (elbow/straight), label + icon layout, bounds
  Models/Icons.cs        generated Lucide icon markup (ISC) — picker categories + a few UI icons
  Services/DiagramStore.cs   save/open/list/delete *.diagram.json in the storage folder
  Services/Exporters.cs      SVG export (page-sized in mm when a page is set) and Mermaid export
  Components/Pages/Designer.razor      the whole editor UI (toolbar, palette, SVG canvas, panel, dialogs)
  Components/Pages/Designer.razor.cs   editor state + interaction logic
  wwwroot/js/designer.js     keyboard shortcuts, wheel zoom/pan, canvas size, focus, drag data
  wwwroot/app.css            all styling
```

## Design rules

- **Typed nodes.** Every shape has a `Type` (start, process, decision, check, action, data, status, end, circle)
  and free-form `Properties`. Rule-check nodes default to `ruleId`, `ifcEntity`, `property`, `operator`, `value`.
  This is what lets the audit tool read diagrams later — don't replace types with purely visual styling.
- **One geometry, two renderers.** The canvas (Designer.razor) and the SVG export (Exporters.cs) must draw the
  same thing. Put shape/connector/label maths in `Geometry.cs` and call it from both. Any new visual feature
  needs both renderers updated, and the Mermaid export updated where Mermaid can express it.
- **Undo.** Every model change goes through `Change()` (snapshot + dirty) or, for drags, a snapshot taken at
  gesture start and pushed on pointer-up. Panel inputs call `Checkpoint` on focus.
- **Groups** don't nest. Membership is `GroupId` on nodes/comments; moving a group moves its members (`MoveBox`).
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
3. PNG / PDF export (PDF at page size).
4. Nested groups.
5. Mermaid import.
6. Later: visual rule builder for the Data Audit Tools rules engine (Excel template stays a valid source).
