using BeDiagram.Editing;
using BeDiagram.Editing.Lanes;
using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>
/// Offered on a blank diagram: pick which way the lanes run, how many and the gap between them, and
/// <see cref="DiagramEditor.CreateLanes"/> adds them as top-level groups. One direction per diagram — horizontal
/// lanes or vertical lanes, not both.
/// </summary>
public partial class LaneCreator
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;

    /// <summary>Called after the lanes are added (the page fits them in view).</summary>
    [Parameter] public EventCallback OnCreated { get; set; }

    bool Open { get; set; }
    ILaneLayout Layout { get; set; } = LaneLayouts.Horizontal;
    int Count { get; set; } = 3;
    double Gap { get; set; }

    void Toggle() => Open = !Open;

    void Pick(ILaneLayout layout) => Layout = layout;

    string LayoutClass(ILaneLayout layout) => layout == Layout ? "on" : "";

    static string LayoutIcon(ILaneLayout layout) => layout == LaneLayouts.Vertical ? "columns-3" : "rows-3";

    async Task Create()
    {
        Editor.CreateLanes(Layout, Count, Gap);
        Open = false;
        await OnCreated.InvokeAsync();
    }
}
