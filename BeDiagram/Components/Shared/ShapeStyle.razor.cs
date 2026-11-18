using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Emphasis;

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

    /// <summary>Called after every change, so the page redraws the canvas (a click here only re-renders this component).</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    Task Set(Action<Node> change)
    {
        Editor.Restyle(Nodes, change);
        return OnChanged.InvokeAsync();
    }

    Task SetEmphasis(EmphasisLevel level)
    {
        Editor.SetEmphasis(Nodes, level);
        return OnChanged.InvokeAsync();
    }

    /// <summary>"on" when there are shapes and every one passes <paramref name="test"/>.</summary>
    string On(Func<Node, bool> test)
    {
        if (Nodes.Count == 0)
        {
            return "";
        }
        foreach (Node n in Nodes)
        {
            if (!test(n))
            {
                return "";
            }
        }
        return "on";
    }

    static bool Forced(Node n)
    {
        return Emphases.Of(n).ForcesSolidFill;
    }

    /// <summary>True when every shape passes <paramref name="test"/> (also when there are none).</summary>
    bool Every(Func<Node, bool> test)
    {
        foreach (Node n in Nodes)
        {
            if (!test(n))
            {
                return false;
            }
        }
        return true;
    }

    bool AllFilled
    {
        get
        {
            return Every(n => n.Filled);
        }
    }

    /// <summary>Major steps always have a solid fill, so the box is ticked and greyed out when every shape is major.</summary>
    bool FillForced
    {
        get
        {
            return Every(Forced);
        }
    }

    bool FillChecked
    {
        get
        {
            return Every(n => n.Filled || Forced(n));
        }
    }

    string FillTitle
    {
        get
        {
            if (FillForced)
            {
                return "Major steps always have a solid fill";
            }
            return "";
        }
    }
}
