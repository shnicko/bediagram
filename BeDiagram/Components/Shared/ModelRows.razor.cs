using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Kinds;

namespace BeDiagram.Components.Shared;

/// <summary>
/// The rows of a class, interface, enumeration or entity in the side panel, one block per compartment, plus the
/// name-only and abstract switches. All changes go through <see cref="DiagramEditor"/> (one undo step each; typing
/// records undo when the box gets focus).
/// </summary>
public partial class ModelRows
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public Node Node { get; set; } = default!;

    /// <summary>Called after every change, so the page redraws the canvas.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    /// <summary>The node's kind (the panel only shows this component for model nodes).</summary>
    ModelKind Model
    {
        get
        {
            return (ModelKind)Node.Kind;
        }
    }

    Task NameOnlyChanged(ChangeEventArgs e)
    {
        Editor.SetNameOnly(Node, e.Value is true);
        return OnChanged.InvokeAsync();
    }

    Task AbstractChanged(ChangeEventArgs e)
    {
        Editor.SetAbstract(Node, e.Value is true);
        return OnChanged.InvokeAsync();
    }

    Task Add(RowSection section)
    {
        Editor.AddItem(Node, section);
        return OnChanged.InvokeAsync();
    }

    Task Changed()
    {
        return OnChanged.InvokeAsync();
    }
}
