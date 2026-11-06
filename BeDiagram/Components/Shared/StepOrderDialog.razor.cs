using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>
/// The step order pop-up: every step in order, as numbered slots. Drag a shape into the gap between two slots to make
/// it a step of its own there, or onto a slot to run it in parallel with that step; the ↑ / ↓ and parallel buttons do
/// the same without dragging. Every change goes straight into the diagram through <see cref="DiagramEditor"/>
/// (one undo step each), so the numbers on the canvas swap as you go.
/// </summary>
public partial class StepOrderDialog
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;
    /// <summary>Raised after every change, so the page behind redraws the canvas.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>The shape being dragged in the list (null when not dragging).</summary>
    Node? dragging;
    /// <summary>The gap or slot under the pointer while dragging, for the drop highlight.</summary>
    int? overGap, overSlot;

    Diagram D => Editor.Diagram;

    /// <summary>The steps grouped by number, in order; parallel steps share a slot (top to bottom, then left to right).</summary>
    List<(int Step, List<Node> Nodes)> Slots =>
        Editor.Steps.Ordered().GroupBy(n => n.Step!.Value).Select(g => (g.Key, g.ToList())).ToList();

    /// <summary>The gap after the last slot (dropping there puts a shape at the end).</summary>
    int EndGap => Editor.Steps.Next;

    static string NameOf(Node n) => string.IsNullOrWhiteSpace(n.Label) ? NodeTypes.Get(n.Type).Display : n.Label;

    /// <summary>The shape's type, plus the title of the group holding it (so lanes are easy to tell apart).</summary>
    string MetaOf(Node n) =>
        Editor.Tree.Parent(n) is Group g ? $"{NodeTypes.Get(n.Type).Display} · {g.Title}" : NodeTypes.Get(n.Type).Display;

    bool IsSelected(Node n) => Editor.Selection.Contains(n.Id);

    bool IsParallel(Node n) => Editor.Steps.IsParallel(n);

    bool CanEarlier(Node n) => Editor.Steps.CanMoveBefore(n, Editor.Steps.EarlierGap(n));

    bool CanLater(Node n) => Editor.Steps.CanMoveBefore(n, Editor.Steps.LaterGap(n));

    bool CanJoinAbove(Node n) => n.Step is int s && Editor.Steps.CanJoin(n, s - 1);

    string ItemClass(Node n) => "step-item" + (IsSelected(n) ? " selected" : "") + (ReferenceEquals(dragging, n) ? " dragging" : "");

    string GapClass(int gap) =>
        "step-gap" + (dragging is not null && overGap == gap && Editor.Steps.CanMoveBefore(dragging, gap) ? " over" : "");

    string SlotClass(int step) =>
        "step-slot" + (dragging is not null && overSlot == step && Editor.Steps.CanJoin(dragging, step) ? " over" : "");

    string EarlierTip(Node n) => IsParallel(n) ? "Take out of the parallel step, just before it" : "One place earlier";

    string LaterTip(Node n) => IsParallel(n) ? "Take out of the parallel step, just after it" : "One place later";

    // ───── Dragging ─────

    void StartDrag(Node n) { dragging = n; overGap = null; overSlot = null; }

    void EndDrag() { dragging = null; overGap = null; overSlot = null; }

    void EnterGap(int gap) { overGap = gap; overSlot = null; }

    void EnterSlot(int step) { overSlot = step; overGap = null; }

    Task DropInGap(int gap)
    {
        Node? n = dragging;
        EndDrag();
        if (n is null) return Task.CompletedTask;
        Editor.MoveStepBefore(n, gap);
        return OnChanged.InvokeAsync();
    }

    Task DropOnSlot(int step)
    {
        Node? n = dragging;
        EndDrag();
        if (n is null) return Task.CompletedTask;
        Editor.JoinStep(n, step);
        return OnChanged.InvokeAsync();
    }

    // ───── Buttons ─────

    Task Earlier(Node n) { Editor.MoveStepEarlier(n); return OnChanged.InvokeAsync(); }

    Task Later(Node n) { Editor.MoveStepLater(n); return OnChanged.InvokeAsync(); }

    /// <summary>Parallel button: a parallel step becomes its own step straight after; otherwise it joins the step above.</summary>
    Task ToggleParallel(Node n)
    {
        if (IsParallel(n)) Editor.SetParallel(n, false);
        else if (n.Step is int s) Editor.JoinStep(n, s - 1);
        return OnChanged.InvokeAsync();
    }

    /// <summary>Clicking a name selects that shape on the canvas, so you can see which one it is.</summary>
    Task Pick(Node n) { Editor.SelectOnly(n.Id); return OnChanged.InvokeAsync(); }

    Task ShowNumbers(ChangeEventArgs e) { Editor.SetShowSteps(e.Value is true); return OnChanged.InvokeAsync(); }

    Task Undo() { Editor.Undo(); return OnChanged.InvokeAsync(); }

    Task Close() => OnClose.InvokeAsync();
}
