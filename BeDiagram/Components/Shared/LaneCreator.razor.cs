using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Editing.Lanes;
using BeDiagram.Models;

namespace BeDiagram.Components.Shared;

/// <summary>
/// Offered on a blank diagram: pick which way the lanes run, the canvas size (open canvas or a paper size, which the
/// lanes then fill), how many lanes, the gap between them and whether their size is locked, and
/// <see cref="DiagramEditor.CreateLanes"/> adds them as top-level groups and sets the flow direction to match. One direction per diagram — horizontal
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
    string PageSize { get; set; } = PageSetup.None;
    string Orientation { get; set; } = Landscape;
    bool LockSize { get; set; } = true;

    const string Portrait = "portrait", Landscape = "landscape";

    bool HasPage => PageSize != PageSetup.None;

    string SizeHint => HasPage ? "The lanes fill the page inside its margins." : "Each lane starts 1200 × 240 px.";

    void Toggle() => Open = !Open;

    void Pick(ILaneLayout layout) => Layout = layout;

    void SetPageSize(ChangeEventArgs e) => PageSize = e.Value?.ToString() ?? PageSetup.None;

    void PickOrientation(string orientation) => Orientation = orientation;

    string OrientationClass(string orientation) => orientation == Orientation ? "on" : "";

    string LayoutClass(ILaneLayout layout) => layout == Layout ? "on" : "";

    static string LayoutIcon(ILaneLayout layout) => layout == LaneLayouts.Vertical ? "columns-3" : "rows-3";

    async Task Create()
    {
        Editor.CreateLanes(Layout, Count, Gap, PageSize, Orientation, LockSize);
        Open = false;
        await OnCreated.InvokeAsync();
    }
}
