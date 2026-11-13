using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;
using BeDiagram.Services;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel when nothing is selected: the diagram's name and summary, step numbers, swim lanes, auto-layout and the page (paper size, orientation, margins).</summary>
public partial class DiagramPanel : DesignerPart
{
    void PageSizePicked(ChangeEventArgs e)
    {
        Host.SetPageSize(e.Value?.ToString());
    }

    int OffPageCount
    {
        get
        {
            return Editor.OutsidePage().Count;
        }
    }

    string DiagramSummary
    {
        get
        {
            int textBoxes = 0;
            int comments = 0;
            foreach (TextBox t in D.TextBoxes)
            {
                if (t.IsComment)
                {
                    comments++;
                }
                else
                {
                    textBoxes++;
                }
            }
            return $"{D.Nodes.Count} shapes · {D.Edges.Count} connectors · {textBoxes} text boxes · " +
                   $"{comments} comments · {D.Groups.Count} groups";
        }
    }

    void SetFitOnExport(ChangeEventArgs e)
    {
        Editor.Change();
        D.Page.FitOnExport = e.Value is true;
    }

    /// <summary>The diagram's name without its folder (the name box).</summary>
    string DiagramLeaf
    {
        get
        {
            return DiagramStore.LeafOf(D.Name);
        }
    }

    /// <summary>The folder the diagram is saved in ("" = the top of the storage folder).</summary>
    string DiagramFolder
    {
        get
        {
            return DiagramStore.FolderOf(D.Name);
        }
    }

    /// <summary>Renames the diagram in its own folder (Save as moves it to another).</summary>
    void RenameDiagram(ChangeEventArgs e)
    {
        string typed = e.Value?.ToString() ?? string.Empty;
        string leaf = DiagramStore.LeafOf(DiagramStore.SafeName(typed));
        D.Name = DiagramStore.SafeName(DiagramStore.Join(DiagramFolder, leaf));
        Editor.MarkDirty();
    }
}
