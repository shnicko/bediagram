using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Styles;
using BeDiagram.Services;
using BeDiagram.Services.Export;

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
    string? toast;
    string iconFilter = "";
    /// <summary>The menu bar heading whose drop-down is open ("File", "Edit", …), or null.</summary>
    string? openMenu;
    CancellationTokenSource? toastCts;

    ElementReference canvasEl;
    IJSObjectReference? module;
    DotNetObjectReference<Designer>? selfRef;


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

    (double X, double Y) ToWorld(double clientX, double clientY) =>
        ((clientX - canvasLeft - PanX) / Zoom, (clientY - canvasTop - PanY) / Zoom);

    (double X, double Y) ViewCentre() => ((canvasW / 2 - PanX) / Zoom, (canvasH / 2 - PanY) / Zoom);
}
