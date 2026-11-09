using System.Globalization;
using BeDiagram.Models;
using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Services;
using BeDiagram.Services.Export;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Editing;
using BeDiagram.Editing.Angles;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;

namespace BeDiagram.Components.Pages;

public partial class Designer
{
    public Designer()
    {
        Editor = new DiagramEditor(this);
    }

    [Inject] DiagramStore Store { get; set; } = default!;
    [Inject] ExportService Exports { get; set; } = default!;
    [Inject] IJSRuntime JS { get; set; } = default!;

    enum Dialog { None, Open, SaveAs, ConfirmNew, ConfirmLeave, StepOrder, Shortcuts }

    /// <summary>
    /// The diagram and everything done to it (selection, undo, editing actions) live in the editor; this page shows
    /// it, turns mouse / keyboard / panel input into editor calls, and handles files, dialogs and export.
    /// </summary>
    readonly DiagramEditor Editor;

    /// <summary>Short name for the open diagram — the markup reads it a lot.</summary>
    Diagram D => Editor.Diagram;

    bool hasFile;

    // View state
    double PanX = 40, PanY = 40, Zoom = 1;
    /// <summary>Background grid (<see cref="GridLook"/>): a view setting, kept while the page is open, not saved in the file.</summary>
    string grid = GridLook.Dots;
    double canvasLeft, canvasTop, canvasW = 1000, canvasH = 700;

    // Gesture state (State pattern — see Gestures/IGesture.cs): the drag in progress, null when the pointer is idle.
    IGesture? gesture;
    string? paletteDrag;
    bool skipRender;

    // UI
    Dialog dialog;
    string saveAsName = "";
    string? confirmDelete;
    string? toast;
    string iconFilter = "";
    /// <summary>Saved diagrams listed in the Open dialog (read when it opens).</summary>
    IReadOnlyList<(string Name, DateTime Modified)> openFiles = Array.Empty<(string Name, DateTime Modified)>();
    List<Node> SelectedNodes => D.Nodes.Where(n => Editor.Selection.Contains(n.Id)).ToList();
    List<TextBox> SelectedTextBoxes => D.TextBoxes.Where(t => Editor.Selection.Contains(t.Id)).ToList();
    List<Group> SelectedGroups => D.Groups.Where(g => Editor.Selection.Contains(g.Id)).ToList();

    /// <summary>The menu bar heading whose drop-down is open ("File", "Edit", …), or null.</summary>
    string? openMenu;
    readonly ExportOptions exportOptions = new();
    CancellationTokenSource? toastCts;

    ElementReference canvasEl;
    IJSObjectReference? module;
    DotNetObjectReference<Designer>? selfRef;

    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    // ───────────── Lifecycle ─────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/designer.js");
            selfRef = DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("init", selfRef, canvasEl);
        }
    }

    protected override bool ShouldRender()
    {
        if (skipRender) { skipRender = false; return false; }
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (module is not null)
            {
                await module.InvokeVoidAsync("dispose");
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        selfRef?.Dispose();
    }

    // ───────────── Canvas coordinates ─────────────

    static IEnumerable<(double, double)> Ports(IBox n)
    {
        yield return (n.X + n.W / 2, n.Y);
        yield return (n.X + n.W, n.Y + n.H / 2);
        yield return (n.X + n.W / 2, n.Y + n.H);
        yield return (n.X, n.Y + n.H / 2);
    }

    (double X, double Y) ToWorld(double clientX, double clientY) =>
        ((clientX - canvasLeft - PanX) / Zoom, (clientY - canvasTop - PanY) / Zoom);

    (double X, double Y) ViewCentre() => ((canvasW / 2 - PanX) / Zoom, (canvasH / 2 - PanY) / Zoom);

    // ───────────── Palette drag and drop ─────────────

    void OnDrop(DragEventArgs e)
    {
        if (paletteDrag is null) return;
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        switch (paletteDrag)
        {
            case "comment":
                Editor.AddTextAt(x - 100, y - 55, (Editor.HitTest(x, y) as Node)?.Id, comment: true);
                break;
            case "text":
                Editor.AddTextAt(x - 100, y - 30, null, comment: false);
                break;
            case "group":
                Editor.AddGroupAt(x, y);
                break;
            default:
                Editor.AddNodeAt(paletteDrag, x, y, join: Editor.AutoConnect, lockAngle: true);
                break;
        }
        paletteDrag = null;
    }

    // ───────────── Pointer handling ─────────────

    /// <summary>Where the pointer is, in screen px and in world coordinates.</summary>
    GesturePoint PointAt(PointerEventArgs e)
    {
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        return new GesturePoint(e.ClientX, e.ClientY, x, y);
    }

    void ItemDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        if (Editor.EditingId is not null && Editor.EditingId != id) Editor.EndEdit();
        Editor.SelectedEdgeId = null;
        if (e.ShiftKey || e.CtrlKey)
        {
            if (Editor.Selection.Contains(id)) { Editor.Selection.Remove(id); return; }
            Editor.Selection.Add(id);
        }
        else if (!Editor.Selection.Contains(id))
        {
            Editor.SelectOnly(id);
        }
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new MoveGesture(this, x, y, Editor.WithMembers(Editor.Selection));
    }

    /// <summary>Pressing on a group's body: click selects the group, drag draws a selection box inside it.</summary>
    void GroupBodyDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        if (e.ShiftKey || e.CtrlKey) { ItemDown(e, id); return; }
        Editor.EndEdit();
        Editor.ClearSelection();
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new MarqueeGesture(this, x, y, clickSelects: id);
    }

    void PortDown(PointerEventArgs e, string id) => PortDown(e, id, null);

    /// <summary>Pressing a ● port: a shape's side, or (<paramref name="itemId"/>) a row of a list / IPO node.</summary>
    void PortDown(PointerEventArgs e, string id, string? itemId)
    {
        if (e.Button != 0) { StartPan(e); return; }
        Editor.EndEdit();
        Editor.SelectOnly(id);
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new ConnectGesture(this, x, y, id, itemId);
    }

    void ResizeDown(PointerEventArgs e, string id)
    {
        if (Editor.FindBox(id) is not { } box) return;
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new ResizeGesture(this, x, y, box);
    }

    void EdgeDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        if (Editor.EditingId is not null && Editor.EditingId != id) Editor.EndEdit();
        Editor.Selection.Clear();
        Editor.SelectedEdgeId = id;
    }

    /// <summary>Pressing a ● end of the selected connector: drag it to another side of its shape.</summary>
    void EdgeEndDown(PointerEventArgs e, string id, bool fromEnd)
    {
        if (e.Button != 0) { StartPan(e); return; }
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new EdgeEndGesture(this, x, y, id, fromEnd);
    }

    /// <summary>Pressing the ■ on an elbow connector's middle segment: slide that segment sideways.</summary>
    void BendDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        if (BendGesture.TryStart(this, x, y, id) is { } bend) gesture = bend;
    }

    void StartPan(PointerEventArgs e) => gesture = new PanGesture(this, e.ClientX, e.ClientY);

    void BgDown(PointerEventArgs e)
    {
        Editor.EndEdit();
        if (e.Button != 0) { StartPan(e); return; }
        if (!e.ShiftKey) Editor.ClearSelection();
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new MarqueeGesture(this, x, y);
    }

    void BgDoubleClick(MouseEventArgs e)
    {
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        Editor.AddNodeAt(NodeTypes.Process, x, y, join: false);
    }

    /// <summary>Hands the move to the current gesture; skips the render when nothing changed.</summary>
    void PointerMove(PointerEventArgs e)
    {
        if (gesture is null || !gesture.Move(PointAt(e))) skipRender = true;
    }

    void PointerUp(PointerEventArgs e)
    {
        if (gesture is null) { skipRender = true; return; }
        IGesture finished = gesture;
        gesture = null;
        finished.Up();
    }

    /// <summary>Stops the current gesture without pointer-up (Escape, inline editing).</summary>
    void CancelGesture()
    {
        gesture?.Cancel();
        gesture = null;
    }

    // What the canvas reads about the gesture in progress.
    MarqueeGesture? Marquee => gesture as MarqueeGesture;
    string SurfaceClass => gesture switch
    {
        PanGesture => "surface panning",
        ConnectGesture => "surface connecting",
        _ => "surface",
    };

    // ───────────── Panel helpers ─────────────

    /// <summary>Small inline sample of a line look for the panel buttons.</summary>
    static string LabelPlaceHint(Edge e) => e.LabelOrient == LabelOrients.Along
        ? "Inline sits on the line; above and below set the text beside it, as the text reads."
        : "Inline sits on the line; above and below set the text beside it. On up-down lines above = left, below = right.";

    // ───────────── Page (paper size) ─────────────

    void SetPageSize(string? size)
    {
        if (size is null || size == D.Page.Size) return;
        Editor.Change();
        bool wasOpen = !D.Page.Enabled;
        D.Page.Size = size;
        if (D.Page.Enabled && wasOpen && Editor.OutsidePage().Count > 0) Editor.CentreOnPage(checkpoint: false);
        Fit();
        if (D.Page.Enabled && Editor.ContentTooBigForPage())
            ShowToast(D.Page.FitOnExport ? "Drawing is bigger than the page — it will be scaled down to fit on export" : "Drawing is bigger than the page");
    }

    void SetOrientation(string orientation)
    {
        if (D.Page.Size == PageSetup.Custom)
        {
            // Custom sizes: orientation just swaps the numbers.
            bool isLandscape = D.Page.CustomW > D.Page.CustomH;
            if ((orientation == "landscape") == isLandscape) return;
            Editor.Change();
            (D.Page.CustomW, D.Page.CustomH) = (D.Page.CustomH, D.Page.CustomW);
        }
        else
        {
            if (D.Page.Orientation == orientation) return;
            Editor.Change();
        }
        D.Page.Orientation = orientation;
        Fit();
    }

    // ───────────── Flow direction and lane lock (menu toggles) ─────────────

    string FlowClass(string flow) => D.Flow == flow ? "on" : "";

    void SetLockLanes(ChangeEventArgs e) => Editor.SetLockLanes(e.Value is true);

    string SnapClass => Editor.Snap ? "on" : "";

    string SnapTip => Editor.Snap ? "Snap to grid is on (10 px) — click to turn off" : "Snap to grid is off — click to turn on";

    void ToggleSnap() => Editor.Snap = !Editor.Snap;

    bool PageIsLandscape => D.Page.Size == PageSetup.Custom ? D.Page.CustomW > D.Page.CustomH : D.Page.Landscape;

        /// <summary>What above/below means for the selected label's text direction.</summary>

    // ───────────── Inline text editing (the editor tracks what is being edited) ─────────────

    void EditKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey) Editor.EndEdit();
    }

    // ───────────── View ─────────────

    void ZoomAt(double factor, double px, double py)
    {
        double z = Math.Clamp(Zoom * factor, 0.05, 3);
        PanX = px - (px - PanX) * z / Zoom;
        PanY = py - (py - PanY) * z / Zoom;
        Zoom = z;
    }

    void ZoomBy(double factor) => ZoomAt(factor, canvasW / 2, canvasH / 2);

    void Fit()
    {
        double minX, minY, maxX, maxY;
        (double X, double Y, double W, double H)? cb = Geometry.ContentBounds(D);
        if (D.Page.Enabled)
        {
            // Show the whole page (plus anything hanging off it).
            minX = 0; minY = 0; maxX = D.Page.WidthPx; maxY = D.Page.HeightPx;
            if (cb is { } c) { minX = Math.Min(minX, c.X); minY = Math.Min(minY, c.Y); maxX = Math.Max(maxX, c.X + c.W); maxY = Math.Max(maxY, c.Y + c.H); }
            minY -= 24; // room for the page caption
        }
        else
        {
            if (cb is not { } c) { Zoom = 1; PanX = 40; PanY = 40; return; }
            (minX, minY, maxX, maxY) = (c.X, c.Y, c.X + c.W, c.Y + c.H);
        }
        ZoomToBounds(minX, minY, maxX, maxY);
    }

    /// <summary>Zooms and centres on the selected items, labels under shapes included. A selected connector counts as its two ends.</summary>
    void FitSelection()
    {
        List<IBox> boxes = Editor.Selection.Select(Editor.FindBox).OfType<IBox>().ToList();
        if (Editor.SelectedEdge is { } e) boxes.AddRange(new[] { Editor.FindBox(e.From), Editor.FindBox(e.To) }.OfType<IBox>());
        if (boxes.Count == 0) { ShowToast("Select something to zoom to"); return; }
        ZoomToBounds(boxes.Min(b => b.X), boxes.Min(b => b.Y),
                     boxes.Max(b => b.X + b.W), boxes.Max(b => b.Y + b.H + Geometry.ExtraBelow(b)));
    }

    /// <summary>Zooms so the area fits the canvas with a 40px margin (at most 150%), centred.</summary>
    void ZoomToBounds(double minX, double minY, double maxX, double maxY)
    {
        Zoom = Math.Clamp(Math.Min((canvasW - 80) / Math.Max(1, maxX - minX), (canvasH - 80) / Math.Max(1, maxY - minY)), 0.05, 1.5);
        PanX = canvasW / 2 - (minX + maxX) / 2 * Zoom;
        PanY = canvasH / 2 - (minY + maxY) / 2 * Zoom;
    }

    [JSInvokable]
    public void OnCanvasRect(double left, double top, double width, double height)
    {
        canvasLeft = left; canvasTop = top; canvasW = width; canvasH = height;
    }

    [JSInvokable]
    public void OnWheel(double dx, double dy, bool ctrl, bool shift, double px, double py)
    {
        if (ctrl) ZoomAt(Math.Exp(-dy * 0.0015), px, py);
        else if (shift) PanX -= dy;
        else { PanX -= dx; PanY -= dy; }
        StateHasChanged();
    }

    // ───────────── Files ─────────────

    void Reset(bool keepTrail = false)
    {
        if (!keepTrail) trail.Clear();
        Editor.Load(new Diagram());
        hasFile = false;
        Zoom = 1; PanX = 40; PanY = 40;
    }

    void NewDiagram()
    {
        if (Editor.Dirty) dialog = Dialog.ConfirmNew;
        else Reset();
    }

    void ShowOpen()
    {
        confirmDelete = null;
        openFiles = Store.List();
        dialog = Dialog.Open;
    }

    void ShowSaveAs()
    {
        saveAsName = D.Name;
        dialog = Dialog.SaveAs;
        _ = module?.InvokeVoidAsync("focus", "saveas-name");
    }

    /// <summary>True for a group not inside another group (one that can be a swim lane).</summary>
    bool IsTopLevel(Group g) => Editor.Tree.Parent(g) is null;

    /// <summary>Selected groups that are lanes (top-level); with two or more, Arrange works on just those.</summary>
    int SelectedLaneCount => Editor.TopLevelGroups().Count(g => Editor.Selection.Contains(g.Id));

    /// <summary>Opens the step order pop-up (Components/Shared/StepOrderDialog).</summary>
    void ShowStepOrder() => dialog = Dialog.StepOrder;

    void ShowShortcuts() => dialog = Dialog.Shortcuts;

    void CloseDialog() { dialog = Dialog.None; confirmDelete = null; openMenu = null; }

    /// <summary>Open from the Open dialog: a fresh start, so the linked-diagram trail is cleared.</summary>
    async Task Open(string name)
    {
        if (await Load(name)) trail.Clear();
    }

    async Task<bool> Load(string name)
    {
        try
        {
            Diagram loaded = await Store.LoadAsync(name);
            Reset(keepTrail: true);
            Editor.Load(loaded);
            hasFile = true;
            CloseDialog();
            Fit();
            ShowToast($"Opened {name}");
            return true;
        }
        catch (Exception ex)
        {
            ShowToast($"Could not open {name}: {ex.Message}");
            return false;
        }
    }

    // ───────────── Linked diagrams ─────────────

    /// <summary>Diagrams we drilled down from via link badges, outermost first. Shown as a breadcrumb with Back.</summary>
    readonly List<string> trail = new();

    /// <summary>Where we're going once the unsaved-changes question is answered. KeepTrail = trail entries to keep (-1 = push current).</summary>
    (string Target, int KeepTrail)? pendingNav;

    void FollowLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link)) return;
        if (hasFile && link == D.Name) { ShowToast("That link points to this diagram"); return; }
        if (!Store.Exists(link)) { ShowToast($"Linked diagram “{link}” not found in {Store.Folder}"); return; }
        Navigate(link, -1);
    }

    void GoBack() { if (trail.Count > 0) GoUpTo(trail.Count - 1); }

    /// <summary>Jump back to trail[level], dropping it and everything below it from the trail.</summary>
    void GoUpTo(int level)
    {
        if (level < 0 || level >= trail.Count) return;
        Navigate(trail[level], level);
    }

    void Navigate(string target, int keepTrail)
    {
        Editor.EndEdit();
        if (Editor.Dirty)
        {
            pendingNav = (target, keepTrail);
            dialog = Dialog.ConfirmLeave;
            return;
        }
        _ = DoNavigate(target, keepTrail);
    }

    async Task DoNavigate(string target, int keepTrail)
    {
        string? from = hasFile ? D.Name : null;
        List<string> before = trail.ToList();
        if (!await Load(target)) return;
        trail.Clear();
        if (keepTrail >= 0) trail.AddRange(before.Take(keepTrail));
        else
        {
            trail.AddRange(before);
            if (from is not null) trail.Add(from);
        }
        StateHasChanged();
    }

    async Task ConfirmLeave(bool save)
    {
        if (pendingNav is not { } nav) { CloseDialog(); return; }
        if (save)
        {
            await Save();
            if (Editor.Dirty) return; // save failed: stay put, the toast says why
        }
        pendingNav = null;
        CloseDialog();
        Editor.Dirty = false;
        await DoNavigate(nav.Target, nav.KeepTrail);
    }

    void CancelLeave() { pendingNav = null; CloseDialog(); }

    void DeleteFile(string name)
    {
        Store.Delete(name);
        openFiles = Store.List();
        if (hasFile && D.Name == name) { hasFile = false; Editor.Dirty = true; }
        confirmDelete = null;
        ShowToast($"Deleted {name}");
    }

    async Task Save()
    {
        Editor.EndEdit();
        if (!hasFile) { ShowSaveAs(); return; }
        try
        {
            string path = await Store.SaveAsync(D);
            Editor.Dirty = false;
            ShowToast($"Saved {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            ShowToast($"Save failed: {ex.Message}");
        }
    }

    async Task ConfirmSaveAs()
    {
        D.Name = DiagramStore.SafeName(saveAsName);
        hasFile = true;
        CloseDialog();
        await Save();
    }

    async Task SaveAsKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ConfirmSaveAs();
    }

    /// <summary>One Export menu entry, worked out for the markup. StartsGroup draws a divider before a new format.</summary>
    record ExportMenuItem(ExportChoice Choice, string Label, string Tip, bool StartsGroup);

    /// <summary>The Export menu: every format × area combination the <see cref="ExportService"/> offers.</summary>
    IReadOnlyList<ExportMenuItem> ExportMenuItems()
    {
        List<ExportMenuItem> items = new List<ExportMenuItem>();
        IExportFormat? previous = null;
        foreach (ExportChoice c in Exports.Choices())
        {
            items.Add(new ExportMenuItem(c, ExportService.Label(c, D), ExportService.Tip(c, D), previous is not null && previous != c.Format));
            previous = c.Format;
        }
        return items;
    }

    IReadOnlyList<PngResolution> PngResolutions => PngExportFormat.Resolutions(D);

    /// <summary>Exports next to the saved diagram; the current-view area uses what is on screen now.</summary>
    async Task Export(ExportChoice choice)
    {
        try
        {
            ScreenView screen = new ScreenView(PanX, PanY, Zoom, canvasW, canvasH);
            (string path, string? note) = await Exports.ExportAsync(choice, D, screen, exportOptions);
            ShowToast(note is null ? $"Exported {Path.GetFileName(path)}" : $"Exported {Path.GetFileName(path)} ({note})");
        }
        catch (Exception ex)
        {
            ShowToast($"{choice.Format.Name} export failed: {ex.Message}");
        }
    }

    void SetPngResolution(ChangeEventArgs e) =>
        exportOptions.PngResolution = int.TryParse(e.Value?.ToString(), out int i) ? i : 1;

    void SetPngTransparent(ChangeEventArgs e) => exportOptions.PngTransparent = e.Value is true;

    void ShowToast(string message)
    {
        toast = message;
        toastCts?.Cancel();
        CancellationTokenSource cts = toastCts = new CancellationTokenSource();
        _ = Task.Delay(3500, cts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            toast = null;
            _ = InvokeAsync(StateHasChanged);
        }, TaskScheduler.Default);
    }

    // ───────────── Render helpers (read by Designer.razor) ─────────────
    // The markup only loops and reads values; anything that needs working out lives here.

    static readonly string[] EdgeLabelChips = { "Yes", "No", "Pass", "Fail", "" };

    /// <summary>Palette shapes (outcomes have their own section), numbered for the 1–9 shortcut keys.</summary>
    static readonly List<PaletteShape> PaletteShapes = NodeTypes.All
        .Where(t => !t.Kind.HasOutcome)
        .Select((t, i) => new PaletteShape(t, i + 1))
        .ToList();

    static string OutcomePaletteKey(OutcomeInfo o) => $"{NodeTypes.Outcome}:{o.Key}";

    /// <summary>The classic shapes, each offered again as a circle with its type's icon (<see cref="Node.Round"/>).</summary>
    static readonly List<NodeTypeInfo> RoundShapes = NodeTypes.All.Where(t => t.Kind.CanBeRound).ToList();

    static string RoundPaletteKey(NodeTypeInfo t) => NodeFactory.RoundPrefix + t.Key;

    static string RoundPaletteTip(NodeTypeInfo t) => $"{t.Display} as a circle: drag onto the canvas or click to add";

    static string OutcomePaletteTip(OutcomeInfo o) => $"{o.Display}: {o.Hint}. Drag onto the canvas or click to add.";

    /// <summary>Tooltip for a shape tile in the palette (the tiles have no text of their own).</summary>
    static string PaletteShapeTip(PaletteShape p) => p.HasKey
        ? $"{p.Type.Display}: drag onto the canvas, click to add, or press {p.Number}"
        : $"{p.Type.Display}: drag onto the canvas or click to add";

    /// <summary>Extra lines inside a palette tile (the title band and rows of a list / IPO box), or null.</summary>
    static string? PaletteInnerPath(string type) => NodeTypes.Get(type).Kind.PaletteInnerPath;

    /// <summary>The inside of a list / IPO node (title band, sections, rows) — same markup as the export — or null.</summary>
    static MarkupString? PanelBody(NodeView v) => v.N.Kind.Body(v.N, v.Style) is { } body ? (MarkupString)body : null;

    static string PaletteShapePath(string type) => Geometry.ShapePath(new Node { Type = type, W = 48, H = 28 });

    void StartTextDrag() => paletteDrag = "text";
    void StartCommentDrag() => paletteDrag = "comment";
    void StartGroupDrag() => paletteDrag = "group";

    void SetPageSize(ChangeEventArgs e) => SetPageSize(e.Value?.ToString());
    void SetPortrait() => SetOrientation("portrait");
    void SetLandscape() => SetOrientation("landscape");

    double PrintableW => Math.Max(0, D.Page.WidthPx - 2 * D.Page.MarginPx);
    double PrintableH => Math.Max(0, D.Page.HeightPx - 2 * D.Page.MarginPx);
    string PageCaptionStyle => $"font-size:{F(12 / Zoom)}px";

    double BendHandleSize => 12 / Zoom;

    static string NodeIconTransform(LabelLayout lay) =>
        $"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})";

    static MarkupString NodeIconMarkup(Node n) => (MarkupString)Icons.Get(n.Kind.IconOf(n));

    /// <summary>The halo ring around a major step (same markup as the export), or null.</summary>
    static MarkupString? HaloMarkup(NodeView v) => Geometry.HaloSvg(v.N, v.Style.Stroke) is { } h ? (MarkupString)h : null;

    /// <summary>Major steps are always solid, so the panel's Solid fill box is ticked and greyed out.</summary>

    /// <summary>The step-number badge on a shape (same markup as the export).</summary>
    static MarkupString StepBadgeMarkup(NodeView v, int step) => (MarkupString)Geometry.StepBadgeSvg(v.N, step, v.Style.Stroke);

    static string EdgeLabelTransform(EdgeGeom geo) =>
        $"translate({F(geo.LabelX)},{F(geo.LabelY)}) rotate({F(geo.LabelAngle)})";

    /// <summary>A node's label lines with the tspan line offset (first line 0, then one line height each).</summary>
    static IEnumerable<(string Text, string Dy)> LabelLines(LabelLayout lay) =>
        lay.Lines.Select((line, i) => (line, i == 0 ? "0" : F(Geometry.LineHeight)));

    /// <summary>Description lines with the tspan line offset.</summary>
    static IEnumerable<(string Text, string Dy)> DescLines(LabelLayout lay) =>
        lay.Desc.Select((line, i) => (line, i == 0 ? "0" : F(Geometry.DescLineHeight)));

    /// <summary>What the text width applies to for this shape, for the panel hint.</summary>
    static string TextWidthHint(Node n) =>
        n.Kind.LabelBelow(n) ? "label and description under the shape" : "description under the shape";

    static string TextWidthPlaceholder(Node n) => $"Auto ({F(Geometry.DefaultTextWidth(n))})";

    static string NodePanelTitle(Node n) => n.Kind.PanelTitle(n);


    string DiagramSummary =>
        $"{D.Nodes.Count} shapes · {D.Edges.Count} connectors · {D.TextBoxes.Count(t => !t.IsComment)} text boxes · " +
        $"{D.TextBoxes.Count(t => t.IsComment)} comments · {D.Groups.Count} groups";

    bool SaveAsWillOverwrite => Store.Exists(saveAsName) && DiagramStore.SafeName(saveAsName) != (hasFile ? D.Name : null);

    string SaveAsPath => Path.Combine(Store.Folder, DiagramStore.SafeName(saveAsName) + DiagramStore.Extension);

    IEnumerable<Crumb> Crumbs => trail.Select((name, i) => new Crumb(i, name));

    /// <summary>Canvas background: grey around a page sheet, otherwise the chosen grid (or nothing).</summary>
    string CanvasFill =>
        D.Page.Enabled ? "#e3e7ec" : GridLook.CanvasPattern(grid) is { } pattern ? $"url(#{pattern})" : "transparent";

    /// <summary>The grid drawn on the page sheet, or null for no grid.</summary>
    string? PageGridFill => GridLook.PagePattern(grid) is { } pattern ? $"url(#{pattern})" : null;

    void SetGrid(string key) => grid = key;

    /// <summary>Toolbar angle lock: a view-time setting on the editor, kept while the page is open, not saved in the file.</summary>
    void SetAngleLock(IAngleLock a) => Editor.AngleLock = a;

    string GridTransform => $"translate({F(PanX)},{F(PanY)}) scale({F(Zoom)})";

    MarkupString ArrowMarkerDefs => (MarkupString)string.Concat(D.Edges.Append(D.DefaultLook)
        .Select(x => (Colour: LineLook.Colour(x.Color), x.Weight)).Distinct()
        .Select(m => LineLook.MarkerDef(m.Colour, m.Weight)));

    /// <summary>Groups in drawing order: a group inside another is drawn over it.</summary>
    IEnumerable<GroupView> GroupViews() =>
        Editor.Tree.PaintOrder().Select(g =>
        {
            (double ax, double ay, double aw, double ah) = Geometry.GroupTitleArea(g);
            (double tx, double ty, string anchor) = Geometry.GroupTitlePos(g);
            double width = GroupLook.Width(g.BorderWeight) + (Editor.Selection.Contains(g.Id) ? 1 : 0);
            return new GroupView(g, Palette.Get(g.Color), Editor.Selection.Contains(g.Id), Geometry.GroupBandPath(g), new Rect(ax, ay, aw, ah),
                                 tx, ty, anchor, Geometry.GroupFillOpacity(g), LineLook.Dash(g.Border, width), width);
        });

    /// <summary>Selection outline drawn just outside a group (and around a title shown under it).</summary>
    static Rect GroupSelectionRect(Group g) =>
        new Rect(g.X - 5, g.Y - 5, g.W + 10, g.H + 10 + Geometry.ExtraBelow(g));

    /// <summary>Inline title editor: inside the title area, at least 120px wide.</summary>
    static Rect GroupTitleEditRect(GroupView v) =>
        new Rect(v.TitleArea.X + 8, v.TitleArea.Y + (v.TitleArea.H - 24) / 2, Math.Max(120, v.TitleArea.W - 16), 24);

    IEnumerable<TextBoxView> TextBoxViews() =>
        D.TextBoxes.Select(t =>
        {
            TextStyle ts = TextStyle.For(t);
            return new TextBoxView(t, ts, Editor.Selection.Contains(t.Id), Geometry.Layout(t), Geometry.TextBoxPath(t, ts.Radius));
        });

    /// <summary>Connectors to draw: the ones under the shapes, or (<paramref name="rowLayer"/>) the ones that end on a
    /// row of a list / IPO node, drawn above the shapes so that end isn't hidden.</summary>
    IEnumerable<EdgeView> EdgeViews(bool rowLayer = false)
    {
        foreach (Edge e in D.Edges.Where(e => e.EndsOnRow == rowLayer))
        {
            if (Editor.Ends(e) is not (IBox a, IBox b)) continue;
            double lw = LineLook.Width(e.Weight);
            string lc = LineLook.Colour(e.Color);
            yield return new EdgeView(e, Editor.SelectedEdgeId == e.Id, Geometry.EdgeGeometry(a, b, e), lw, lc, $"url(#{LineLook.MarkerId(lc, e.Weight)})");
        }
    }

    /// <summary>Connector labels that need drawing (or editing), in their own layer above the shapes.</summary>
    IEnumerable<EdgeLabelView> EdgeLabelViews()
    {
        foreach (Edge e in D.Edges)
        {
            bool editing = Editor.EditingId == e.Id;
            if (string.IsNullOrWhiteSpace(e.Label) && !editing) continue;
            if (Editor.Ends(e) is not (IBox a, IBox b)) continue;
            EdgeGeom geo = Geometry.EdgeGeometry(a, b, e);
            yield return new EdgeLabelView(e, Editor.SelectedEdgeId == e.Id, editing, geo, Geometry.LayoutEdgeLabel(e, geo));
        }
    }

    IEnumerable<NodeView> NodeViews() =>
        D.Nodes.Select(n =>
        {
            LabelLayout lay = Geometry.Layout(n);
            Rect? back = Geometry.DrawnTextBack(n, lay) is { } r ? new Rect(r.X, r.Y, r.W, r.H) : null;
            return new NodeView(n, NodeTypes.Get(n.Type), Geometry.OutlineOf(n), NodeStyle.For(n), Editor.Selection.Contains(n.Id), lay, Geometry.ExtraBelow(n), back);
        });

    /// <summary>Where the inline label editor sits: under the shape for labels drawn below, otherwise inside it.</summary>
    static Rect NodeEditRect(NodeView v) =>
        v.N.Kind.LabelEditRect(v.N) is { } own ? own
        : v.Layout.Below
        ? new Rect(v.N.X + v.N.W / 2 - 80, v.N.Y + v.N.H + 4, 160, 44)
        : new Rect(v.N.X + 8, v.N.Y + 6, v.N.W - 16, v.N.H - 12);

    /// <summary>Step number typed in the panel: a whole number makes the shape that step (others move down one).</summary>
    void StepTyped(Node n, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int step)) Editor.SetStep(n, step);
    }

    /// <summary>A shape can run in parallel when there's another step to share a number with (or it already does).</summary>
    bool CanBeParallel(Node n) => Editor.Steps.IsParallel(n) || Editor.Steps.Last > 1;

    /// <summary>"Shares step 4 with Design Review" under the step box, or null when the shape has a number of its own.</summary>
    string? ParallelNote(Node n)
    {
        List<Node> others = Editor.Steps.ParallelWith(n);
        if (others.Count == 0) return null;
        return $"Shares step {n.Step} with {string.Join(", ", others.Select(o => string.IsNullOrWhiteSpace(o.Label) ? NodeTypes.Get(o.Type).Display : o.Label.Replace("\n", " ")))}";
    }

    /// <summary>How many of the selected items are steps (shapes other than outcomes).</summary>
    int SelectedStepCount => D.Nodes.Count(n => Editor.Selection.Contains(n.Id) && n.Kind.IsStep);

    static bool HasProperties(Node n) => n.Properties.Values.Any(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>The live connector drawn while dragging from a port (null when not connecting).</summary>
    ConnectPreview? LiveConnector()
    {
        if (gesture is not ConnectGesture cg || cg.From is not { } from) return null;
        IBox? target = cg.Target;
        (double dropX, double dropY) = cg.DropPoint;
        IBox to = target ?? new TextBox { X = dropX, Y = dropY, W = 0, H = 0 };
        return new ConnectPreview(Geometry.EdgeGeometry(from, to, D.DefaultConnector).D, target);
    }

    /// <summary>Handles on the selected connector: ends (move to another side) and the bend (drag sideways).</summary>
    EdgeHandleView? SelectedEdgeHandles()
    {
        if (Editor.SelectedEdge is not { } he || Editor.Ends(he) is not (IBox ha, IBox hb) || Editor.EditingId == he.Id) return null;
        EdgeRoute hr = Geometry.Route(ha, hb, he);
        List<SideTarget> sides = new();
        if (gesture is EdgeEndGesture eg)
        {
            IBox box = eg.FromEnd ? ha : hb;
            string? current = eg.FromEnd ? hr.FromSide : hr.ToSide;
            foreach (string side in box.ConnectSides)
            {
                (double sx, double sy) = Geometry.SidePoint(box, side);
                sides.Add(new SideTarget(sx, sy, side == current));
            }
        }
        (double bx, double by) = hr.BendHandle;
        (double p0x, double p0y) = hr.Points[0];
        (double p1x, double p1y) = hr.Points[^1];
        return new EdgeHandleView(he.Id, sides, hr.BendIndex >= 0 && he.Style != EdgeStyles.Straight, bx, by, hr.BendIsVertical, p0x, p0y, p1x, p1y);
    }

    /// <summary>Icon picker contents, filtered by the search box (empty categories dropped).</summary>
    List<(string Category, List<string> Names)> FilteredIcons() =>
        Icons.Categories
            .Select(c => (c.Category, Names: c.Names.Where(nm => string.IsNullOrWhiteSpace(iconFilter) || nm.Contains(iconFilter.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()))
            .Where(c => c.Names.Count > 0)
            .ToList();

    int MemberCount(Group g) => Editor.Members(g).Count();

    int OffPageCount => Editor.OutsidePage().Count;

    string? LinkExclude => hasFile ? D.Name : null;

    /// <summary>Inline editor styled like the box itself, so editing looks the same as the finished text.</summary>
    static string TextEditStyle(TextBox t, TextStyle s) =>
        $"font-family:{s.Family};font-size:{F(t.FontSize)}px;line-height:{F(t.FontSize * TextLook.LineSpacing)}px;" +
        $"font-weight:{(t.Bold ? "600" : "400")};color:{s.TextColour};text-align:{t.Align};padding:{F(TextLook.PadY)}px {F(TextLook.PadX)}px;";

    // Small UI handlers that used to be inline lambdas in the markup.

    /// <summary>After the lane creator adds lanes: fit them in view.</summary>
    Task FitAfterLanes() => Run(FitCmd);

    void SetOpenMenu(string? menu) => openMenu = menu;
    void CloseMenus() => openMenu = null;
    void StartPaletteDrag(string key) => paletteDrag = key;
    void DragOver() { }
    void AskDelete(string name) => confirmDelete = name;
    void DiscardAndNew() { Reset(); CloseDialog(); }

    void SetPropValue(Node n, string key, ChangeEventArgs e)
    {
        n.Properties[key] = e.Value?.ToString() ?? string.Empty;
        Editor.MarkDirty();
    }

    void RenameDiagram(ChangeEventArgs e)
    {
        D.Name = DiagramStore.SafeName(e.Value?.ToString() ?? string.Empty);
        Editor.MarkDirty();
    }

    void SetFitOnExport(ChangeEventArgs e) { Editor.Change(); D.Page.FitOnExport = e.Value is true; }
}
