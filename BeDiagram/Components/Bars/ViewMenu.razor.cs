using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;

namespace BeDiagram.Components.Bars;

/// <summary>The View menu: step numbers, row ports only, and zoom.</summary>
public partial class ViewMenu : DesignerPart
{
    void RowPortsOnlyChanged(ChangeEventArgs e)
    {
        Editor.SetRowPortsOnly(e.Value is true);
    }
}
