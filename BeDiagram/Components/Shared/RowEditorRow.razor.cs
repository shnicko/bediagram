using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Kinds;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Components.Shared;

/// <summary>One row in the row editor window's table: its number, the structured fields its block uses
/// (<see cref="ModelRowFields"/>), label or name, description, and move / remove. Changes go through
/// <see cref="DiagramEditor"/> like the side panel (typing records undo when the box gets focus).</summary>
public partial class RowEditorRow
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;
    [Parameter, EditorRequired] public Node Node { get; set; } = default!;
    [Parameter, EditorRequired] public ListItem Item { get; set; } = default!;

    /// <summary>The row's place in its block (1, 2, 3, …).</summary>
    [Parameter] public int Number { get; set; }

    /// <summary>The block's section (null in a list): operations get "name(params)" and "Returns" hints.</summary>
    [Parameter] public RowSection? Section { get; set; }

    [Parameter, EditorRequired] public ModelRowFields Fields { get; set; } = default!;

    /// <summary>Called after every change, so the page redraws the canvas.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    string VisibilityValue
    {
        get
        {
            if (Item.Visibility is null)
            {
                return "";
            }
            return EnumNames.Of(Item.Visibility.Value);
        }
    }

    string TypeValue
    {
        get
        {
            return Item.DataType ?? "";
        }
    }

    string MultiplicityValue
    {
        get
        {
            return Item.Multiplicity ?? "";
        }
    }

    string DescriptionValue
    {
        get
        {
            return Item.Description ?? "";
        }
    }

    string NamePlaceholder
    {
        get
        {
            if (Section == RowSection.Operation)
            {
                return "name(params)";
            }
            return "name";
        }
    }

    string TypePlaceholder
    {
        get
        {
            if (Section == RowSection.Operation)
            {
                return "return type";
            }
            return "type";
        }
    }

    Task VisibilityPicked(ChangeEventArgs e)
    {
        string? name = e.Value?.ToString();
        Visibility? v = null;
        if (!string.IsNullOrEmpty(name))
        {
            v = EnumNames.Parse<Visibility>(name);
        }
        Editor.SetItemVisibility(Item, v);
        return OnChanged.InvokeAsync();
    }

    Task LabelTyped(ChangeEventArgs e)
    {
        Item.Label = e.Value?.ToString() ?? "";
        Editor.MarkDirty();
        return OnChanged.InvokeAsync();
    }

    Task TypeTyped(ChangeEventArgs e)
    {
        Item.DataType = e.Value?.ToString();
        Editor.MarkDirty();
        return OnChanged.InvokeAsync();
    }

    Task MultiplicityTyped(ChangeEventArgs e)
    {
        Item.Multiplicity = e.Value?.ToString();
        Editor.MarkDirty();
        return OnChanged.InvokeAsync();
    }

    Task DescriptionTyped(ChangeEventArgs e)
    {
        Item.Description = e.Value?.ToString();
        Editor.MarkDirty();
        return OnChanged.InvokeAsync();
    }

    Task PrimaryKeyChanged(ChangeEventArgs e)
    {
        Editor.SetPrimaryKey(Item, e.Value is true);
        return OnChanged.InvokeAsync();
    }

    Task ForeignKeyChanged(ChangeEventArgs e)
    {
        Editor.SetForeignKey(Item, e.Value is true);
        return OnChanged.InvokeAsync();
    }

    Task Up()
    {
        Editor.MoveItem(Node, Item.Id, -1);
        return OnChanged.InvokeAsync();
    }

    Task Down()
    {
        Editor.MoveItem(Node, Item.Id, 1);
        return OnChanged.InvokeAsync();
    }

    Task Remove()
    {
        Editor.RemoveItem(Node, Item.Id);
        return OnChanged.InvokeAsync();
    }
}
