using BeDiagram.Editing;
using BeDiagram.Models;

namespace BeDiagram.Components.Pages;

/// <summary>
/// One open diagram on the Designer page: its editor (the diagram, selection, undo and the unsaved-changes flag),
/// whether it has a file, where the view is panned and zoomed to, and the linked-diagram breadcrumb. The page shows
/// one tab at a time (<c>Designer.active</c>) and only that one takes input, so only the active tab can be edited.
/// </summary>
public class DiagramTab
{
    public DiagramTab(DiagramEditor editor)
    {
        Editor = editor;
    }

    /// <summary>Tells the tabs apart in the tab strip.</summary>
    public string Id { get; } = Node.NewId("tab");

    /// <summary>The diagram and everything done to it.</summary>
    public DiagramEditor Editor { get; }

    /// <summary>True once the diagram has been opened from or saved to a file.</summary>
    public bool HasFile { get; set; }

    // View: kept per tab, so switching back shows the diagram where it was left.
    public double PanX { get; set; } = 40;
    public double PanY { get; set; } = 40;
    public double Zoom { get; set; } = 1;

    /// <summary>Diagrams this tab drilled down from via link badges, outermost first.</summary>
    public List<string> Trail { get; } = new List<string>();
}
