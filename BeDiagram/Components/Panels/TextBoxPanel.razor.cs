using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;
using BeDiagram.Models.Layout;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel for one selected text box or comment: text, comment preset, look, position and size, and
/// group.</summary>
public partial class TextBoxPanel : DesignerPart
{
    [Parameter, EditorRequired] public TextBox Box { get; set; } = default!;

    /// <summary>The position and size fields' values: the top-left corner and the size, as for a group.</summary>
    Rect BoxBounds
    {
        get
        {
            return new Rect(Box.X, Box.Y, Box.W, Box.H);
        }
    }

    double BoxMinW
    {
        get
        {
            return BoxSizes.MinSize(Box).W;
        }
    }

    double BoxMinH
    {
        get
        {
            return BoxSizes.MinSize(Box).H;
        }
    }

    void SetBounds(Rect r)
    {
        Editor.SetTextBoxBounds(Box, r.X, r.Y, r.W, r.H);
    }
}
