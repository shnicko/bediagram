using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Services;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel when nothing is selected: the diagram's name and summary, step numbers, swim lanes, auto-layout and the page (paper size, orientation, margins).</summary>
public partial class DiagramPanel : DesignerPart
{
    void PageSizePicked(ChangeEventArgs e) => Host.SetPageSize(e.Value?.ToString());

    int OffPageCount => Editor.OutsidePage().Count;

    string DiagramSummary =>
        $"{D.Nodes.Count} shapes · {D.Edges.Count} connectors · {D.TextBoxes.Count(t => !t.IsComment)} text boxes · " +
        $"{D.TextBoxes.Count(t => t.IsComment)} comments · {D.Groups.Count} groups";

    void SetFitOnExport(ChangeEventArgs e) { Editor.Change(); D.Page.FitOnExport = e.Value is true; }

    /// <summary>The diagram's name without its folder (the name box).</summary>
    string DiagramLeaf => DiagramStore.LeafOf(D.Name);

    /// <summary>The folder the diagram is saved in ("" = the top of the storage folder).</summary>
    string DiagramFolder => DiagramStore.FolderOf(D.Name);

    /// <summary>Renames the diagram in its own folder (Save as moves it to another).</summary>
    void RenameDiagram(ChangeEventArgs e)
    {
        D.Name = DiagramStore.SafeName(DiagramStore.Join(DiagramFolder, DiagramStore.LeafOf(DiagramStore.SafeName(e.Value?.ToString() ?? string.Empty))));
        Editor.MarkDirty();
    }
}
