using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Kinds;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Components.Shared;

/// <summary>One row of a class or entity in the side panel: the structured fields its compartment uses
/// (<see cref="ModelRowFields"/>: visibility, name, data type, multiplicity, keys), move and remove.</summary>
public partial class ModelRowEditor
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;
    [Parameter, EditorRequired] public Node Node { get; set; } = default!;
    [Parameter, EditorRequired] public ListItem Item { get; set; } = default!;
    [Parameter, EditorRequired] public RowSection Section { get; set; }
    [Parameter, EditorRequired] public ModelRowFields Fields { get; set; } = default!;

    /// <summary>Called after every change, so the page redraws the canvas.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    bool HasSecondLine
    {
        get
        {
            return Fields.DataType || Fields.Multiplicity || Fields.Keys;
        }
    }

    /// <summary>The visibility as its saved name, or "" for none (the first option).</summary>
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
            if (Item.DataType is null)
            {
                return "";
            }
            return Item.DataType;
        }
    }

    string MultiplicityValue
    {
        get
        {
            if (Item.Multiplicity is null)
            {
                return "";
            }
            return Item.Multiplicity;
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
