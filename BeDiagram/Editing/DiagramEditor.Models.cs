using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Kinds;
using BeDiagram.Models.Styles;

namespace BeDiagram.Editing;

/// <summary>Data models: packages (groups in the package look) and the structured rows of classes and entities
/// (visibility, data type, multiplicity, keys), abstract classes and the name-only (conceptual) view. Adding,
/// removing and reordering rows is shared with lists (<c>DiagramEditor.Panels.cs</c>).</summary>
public partial class DiagramEditor
{
    // ───────────── Packages ─────────────

    /// <summary>Palette drop: an empty package centred on (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    public void AddPackageAt(double cx, double cy)
    {
        AddGroupAt(cx, cy);
        StylePackage();
    }

    /// <summary>Palette click: with items selected, a package wrapped around them (like Ctrl+G); otherwise an empty
    /// one in the middle of the view.</summary>
    public void AddPackageQuick()
    {
        AddGroupQuick();
        StylePackage();
    }

    /// <summary>The package look on the group just added: the title in a name tag, a grey outline, no fill. Part of
    /// the add's undo step.</summary>
    void StylePackage()
    {
        Group? g = NewlyAdded<Group>();
        if (g is null)
        {
            return;
        }
        g.Title = "Package";
        g.Color = "grey";
        g.Filled = false;
        g.HeaderPos = GroupLook.Top;
        g.HeaderStyle = GroupLook.Tag;
        GrowGroup(g);
    }

    // ───────────── Classes and entities ─────────────

    /// <summary>Marks a class abstract ({abstract} over its name). Resizes to fit. One undo step.</summary>
    public void SetAbstract(Node n, bool isAbstract)
    {
        if (n.Abstract == isAbstract)
        {
            return;
        }
        if (n.Kind is not ModelKind model || !model.CanBeAbstract)
        {
            return;
        }
        Change();
        n.Abstract = isAbstract;
        FitPanel(n);
    }

    /// <summary>Shows only the name (a conceptual model) or the rows too. The rows are kept. One undo step.</summary>
    public void SetNameOnly(Node n, bool nameOnly)
    {
        if (n.NameOnly == nameOnly)
        {
            return;
        }
        Change();
        n.NameOnly = nameOnly;
        FitPanel(n);
    }

    /// <summary>Sets a row's visibility (null = no symbol). One undo step.</summary>
    public void SetItemVisibility(ListItem item, Visibility? visibility)
    {
        if (item.Visibility == visibility)
        {
            return;
        }
        Change();
        item.Visibility = visibility;
    }

    /// <summary>Marks an entity column as (part of) the primary key. One undo step.</summary>
    public void SetPrimaryKey(ListItem item, bool on)
    {
        if (item.PrimaryKey == on)
        {
            return;
        }
        Change();
        item.PrimaryKey = on;
    }

    /// <summary>Marks an entity column as a foreign key. One undo step.</summary>
    public void SetForeignKey(ListItem item, bool on)
    {
        if (item.ForeignKey == on)
        {
            return;
        }
        Change();
        item.ForeignKey = on;
    }
}
