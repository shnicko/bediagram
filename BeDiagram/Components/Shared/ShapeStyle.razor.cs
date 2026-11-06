using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Emphasis;
using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>
/// A shape's look: colour, solid fill and emphasis. Used by the shape panel (one shape) and the multi-selection panel
/// (every selected shape), so a click restyles them all in one undo step. A button shows as on only when every shape
/// has that setting.
/// </summary>
public partial class ShapeStyle
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<Node> Nodes { get; set; } = default!;

    string On(Func<Node, bool> test) => Nodes.Count > 0 && Nodes.All(test) ? "on" : "";

    static bool Forced(Node n) => Emphases.Of(n).ForcesSolidFill;

    bool AllFilled => Nodes.All(n => n.Filled);

    /// <summary>Major steps always have a solid fill, so the box is ticked and greyed out when every shape is major.</summary>
    bool FillForced => Nodes.All(Forced);

    bool FillChecked => Nodes.All(n => n.Filled || Forced(n));

    string FillTitle => FillForced ? "Major steps always have a solid fill" : "";
}
