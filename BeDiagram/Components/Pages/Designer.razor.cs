using BeDiagram.Models.Serialization;
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
    /// <summary>What a drop from the palette adds at the drop point (world x, y): set when a tile drag starts.</summary>
    Action<double, double>? paletteDrop;
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

    // ───────────── Panel helpers ─────────────

    /// <summary>Small inline sample of a line look for the panel buttons.</summary>
    static string LabelPlaceHint(Edge e) => e.LabelOrient == LabelOrient.Along
        ? "Inline sits on the line; above and below set the text beside it, as the text reads."
        : "Inline sits on the line; above and below set the text beside it. On up-down lines above = left, below = right.";

    // ───────────── Inline text editing (the editor tracks what is being edited) ─────────────

    void EditKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey) Editor.EndEdit();
    }
}
