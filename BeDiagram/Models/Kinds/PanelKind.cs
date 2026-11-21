using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Location;
using BeDiagram.Models.Panels;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Kinds;

/// <summary>
/// List and input–process–output nodes ("panels"): a titled box holding rows (<see cref="Node.Items"/>). The node's
/// label is the title in the band at the top. How the inside is laid out and drawn is the panel's
/// <see cref="IPanelLayout"/> (<see cref="LayoutOf"/>); how rows are added, ordered and tidied differs between a list
/// and an IPO box (sections), so each subclass answers those.
/// </summary>
public abstract class PanelKind : NodeKind
{
    public override bool IsPanel
    {
        get
        {
            return true;
        }
    }

    /// <summary>Placed by its centre, always as tall as its rows.</summary>
    public override IPlacement Placement
    {
        get
        {
            return Placements.Panel;
        }
    }

    /// <summary>The layout strategy for this node: a list, or an IPO box side by side or stacked.</summary>
    public abstract IPanelLayout LayoutOf(Node n);

    public override LabelLayout LayoutLabel(Node n, string label)
    {
        return LayoutOf(n).HeaderLabel(n, label);
    }

    public override (double W, double H) MinSize(Node n)
    {
        return (LayoutOf(n).MinWidth(n), LayoutOf(n).NeededHeight(n));
    }

    public override Rect? LabelEditRect(Node n)
    {
        return LayoutOf(n).TitleEditRect(n);
    }

    public override string? Body(Node n, NodeStyle style)
    {
        return LayoutOf(n).Body(n, style);
    }

    public override IBox EndAt(Node n, string? itemId)
    {
        if (itemId is null)
        {
            return n;
        }
        ItemBox? row = LayoutOf(n).RowBox(n, itemId);
        if (row is not null)
        {
            return row;
        }
        return n;
    }

    public override ItemBox? RowAt(Node n, double x, double y)
    {
        return LayoutOf(n).RowAt(n, x, y);
    }

    public override IEnumerable<(double X, double Y, string ItemId)> RowPorts(Node n)
    {
        return LayoutOf(n).RowPorts(n);
    }

    public abstract override List<ListItem> NewItems();

    /// <summary>A new panel gets its starter rows, and is exactly as tall as they need (rows have a fixed height).</summary>
    public override void Init(Node n, OutcomeType? outcome)
    {
        n.Items = NewItems();
        n.H = LayoutOf(n).NeededHeight(n);
    }

    /// <summary>Adds a new row (to the end of the list, or of IPO section <paramref name="section"/>) and returns it.</summary>
    public abstract ListItem AddRow(Node n, RowSection? section);

    /// <summary>The rows a row is ordered among: the whole list, or its IPO section.</summary>
    public abstract List<ListItem> Peers(Node n, string itemId);

    /// <summary>After a type change to this kind: starter rows if it had none, and rows made to fit (sections set or cleared).</summary>
    public abstract void TidyRows(Node n);
}
