using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel for one selected connector: line style, route reset, weight, pattern, colour, arrowheads, label text, direction, placement and position.</summary>
public partial class ConnectorPanel : DesignerPart
{
    [Parameter, EditorRequired] public Edge Edge { get; set; } = default!;

    /// <summary>Quick labels offered under the label box (the empty one clears it).</summary>
    static readonly string[] EdgeLabelChips = { "Yes", "No", "Pass", "Fail", "" };

    /// <summary>What above / below mean for the label's text direction (the Label placement tooltip).</summary>
    static string LabelPlaceHint(Edge e) => e.LabelOrient == LabelOrient.Along
        ? "Inline sits on the line; above and below set the text beside it, as the text reads."
        : "Inline sits on the line; above and below set the text beside it. On up-down lines above = left, below = right.";
}
