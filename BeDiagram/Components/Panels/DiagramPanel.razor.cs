using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;
using BeDiagram.Services;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel when nothing is selected: the diagram's name and summary, step numbers, swim lanes, auto-layout and the page (paper size, orientation, margins).</summary>
public partial class DiagramPanel : DesignerPart
{
    void PageSizePicked(ChangeEventArgs e) => Host.SetPageSize(e.Value?.ToString());

    bool PageIsLandscape => D.Page.Size == PageSetup.Custom ? D.Page.CustomW > D.Page.CustomH : D.Page.Landscape;

    int OffPageCount => Editor.OutsidePage().Count;

    string DiagramSummary =>
        $"{D.Nodes.Count} shapes · {D.Edges.Count} connectors · {D.TextBoxes.Count(t => !t.IsComment)} text boxes · " +
        $"{D.TextBoxes.Count(t => t.IsComment)} comments · {D.Groups.Count} groups";

    void SetFitOnExport(ChangeEventArgs e) { Editor.Change(); D.Page.FitOnExport = e.Value is true; }

    void RenameDiagram(ChangeEventArgs e)
    {
        D.Name = DiagramStore.SafeName(e.Value?.ToString() ?? string.Empty);
        Editor.MarkDirty();
    }
}
