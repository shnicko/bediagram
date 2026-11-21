using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Kinds;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Panels;

/// <summary>
/// The row editor window: a floating, movable, resizable window with the rows of the selected list, IPO box, class
/// or entity as a table, one row per line and every field in its own column, so long models are easier to edit than
/// in the side panel. It follows the selection and edits apply as you type, through <see cref="Editing.DiagramEditor"/>
/// like the side panel (undo is recorded when a box gets focus). Opened from the ⤢ on the node or the side panel.
/// </summary>
public partial class RowEditorWindow : DesignerPart
{
    /// <summary>Set once when the window opens (see <c>Designer.RowEditor.cs</c>), so the browser's moves stick.</summary>
    string WindowStyle
    {
        get
        {
            return $"left:{F(Host.RowEditorLeft)}px; top:{F(Host.RowEditorTop)}px;";
        }
    }

    /// <summary>The one selected list, IPO box, class or entity, or null.</summary>
    Node? Selected
    {
        get
        {
            if (Editor.Selection.Count != 1)
            {
                return null;
            }
            if (Editor.FindBox(Editor.Selection[0]) is Node n && n.Kind.IsPanel)
            {
                return n;
            }
            return null;
        }
    }

    /// <summary>The selected node's kind when it is a class, interface, enumeration or entity, otherwise null.</summary>
    ModelKind? Model
    {
        get
        {
            Node? n = Selected;
            if (n is not null && n.Kind is ModelKind m)
            {
                return m;
            }
            return null;
        }
    }

    string Heading
    {
        get
        {
            Node? n = Selected;
            if (n is null)
            {
                return "Rows";
            }
            return $"{n.Kind.NameOf(n)} rows";
        }
    }

    string TitleLabel
    {
        get
        {
            if (Model is not null)
            {
                return "Name";
            }
            return "Title";
        }
    }

    string NameHeader
    {
        get
        {
            if (Model is not null)
            {
                return "Name";
            }
            return "Label";
        }
    }

    string DescriptionTip
    {
        get
        {
            if (Model is not null)
            {
                return "A note about the member, saved with the row (not drawn)";
            }
            return "Smaller text on the row's second line";
        }
    }

    static string TypeHeader(RowGroup g)
    {
        if (g.Section == RowSection.Operation)
        {
            return "Returns";
        }
        return "Type";
    }

    List<RowGroup> Groups
    {
        get
        {
            Node? n = Selected;
            if (n is not null && n.Kind is PanelKind panel)
            {
                return panel.RowGroups(n);
            }
            return new List<RowGroup>();
        }
    }

    /// <summary>The group's rows with their place in it (1, 2, 3, …).</summary>
    static List<(int Number, ListItem Item)> Numbered(RowGroup g)
    {
        List<(int Number, ListItem Item)> result = new List<(int Number, ListItem Item)>();
        for (int i = 0; i < g.Rows.Count; i++)
        {
            result.Add((i + 1, g.Rows[i]));
        }
        return result;
    }

    void TitleTyped(ChangeEventArgs e)
    {
        Node? n = Selected;
        if (n is null)
        {
            return;
        }
        n.Label = e.Value?.ToString() ?? "";
        Editor.MarkDirty();
    }

    void NameOnlyChanged(ChangeEventArgs e)
    {
        Node? n = Selected;
        if (n is not null)
        {
            Editor.SetNameOnly(n, e.Value is true);
        }
    }

    void AbstractChanged(ChangeEventArgs e)
    {
        Node? n = Selected;
        if (n is not null)
        {
            Editor.SetAbstract(n, e.Value is true);
        }
    }

    void NumberRowsChanged(ChangeEventArgs e)
    {
        Node? n = Selected;
        if (n is not null)
        {
            Editor.SetNumberRows(n, e.Value is true);
        }
    }

    void Add(RowGroup g)
    {
        Node? n = Selected;
        if (n is not null)
        {
            Editor.AddItem(n, g.Section);
        }
    }

    void Close()
    {
        Host.CloseRowEditor();
    }
}
