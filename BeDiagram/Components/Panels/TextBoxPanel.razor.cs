using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel for one selected text box or comment: text, comment preset, look, size and group.</summary>
public partial class TextBoxPanel : DesignerPart
{
    [Parameter, EditorRequired] public TextBox Box { get; set; } = default!;
}
