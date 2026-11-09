using BeDiagram.Models;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Editing;

/// <summary>List and input–process–output nodes: adding, removing, reordering and editing their rows, IPO section
/// titles and direction. Row heights are fixed, so after every change the node is sized to fit (<see cref="FitPanel"/>).</summary>
public partial class DiagramEditor
{
    // ───────────── Lists and IPO boxes ─────────────

    /// <summary>The boxes a connector runs between: a row where it ends on one (see <see cref="PanelGeometry.End"/>).</summary>
    public (IBox A, IBox B)? Ends(Edge e) => PanelGeometry.Ends(e, id => FindBox(id));

    /// <summary>Sizes a list / IPO node to fit its rows (height exactly, width at least the minimum) and grows the
    /// groups around it. Doesn't record undo: callers have already called <see cref="Change"/>.</summary>
    public void FitPanel(Node n)
    {
        if (!n.Kind.IsPanel) return;
        n.Items ??= new List<ListItem>();
        n.H = PanelGeometry.NeededHeight(n);
        n.W = Math.Max(n.W, PanelGeometry.MinWidth(n));
        GrowAround(n);
    }

    /// <summary>Adds a row at the end of a list, or of an IPO section, and returns it.</summary>
    public ListItem AddItem(Node n, string? section = null)
    {
        Change();
        n.Items ??= new List<ListItem>();
        bool ipo = n.Type == NodeTypes.Ipo;
        string? key = ipo ? IpoSections.Of(section) : null;
        int count = ipo ? PanelGeometry.InSection(n, key!).Count : n.Items.Count;
        ListItem item = new ListItem { Label = $"Item {count + 1}", Section = key };
        // In an IPO box the new row goes after the last row of its section, keeping the file in reading order.
        int at = ipo ? n.Items.FindLastIndex(i => IpoSections.IndexOf(i.Section) <= IpoSections.IndexOf(key)) + 1 : n.Items.Count;
        n.Items.Insert(at, item);
        FitPanel(n);
        return item;
    }

    /// <summary>Removes a row, and the connectors that end on it.</summary>
    public void RemoveItem(Node n, string itemId)
    {
        if (n.Items?.FirstOrDefault(i => i.Id == itemId) is not { } item) return;
        Change();
        n.Items.Remove(item);
        Diagram.Edges.RemoveAll(e => (e.From == n.Id && e.FromItem == itemId) || (e.To == n.Id && e.ToItem == itemId));
        FitPanel(n);
    }

    /// <summary>True when the row can move one place up (<paramref name="delta"/> −1) or down (+1) within its list or section.</summary>
    public bool CanMoveItem(Node n, string itemId, int delta)
    {
        List<ListItem> peers = Peers(n, itemId);
        int i = peers.FindIndex(x => x.Id == itemId);
        return i >= 0 && i + delta >= 0 && i + delta < peers.Count;
    }

    /// <summary>Swaps a row with the one above (−1) or below (+1) it in its list or section.</summary>
    public void MoveItem(Node n, string itemId, int delta)
    {
        if (!CanMoveItem(n, itemId, delta) || n.Items is null) return;
        List<ListItem> peers = Peers(n, itemId);
        ListItem a = peers.First(x => x.Id == itemId);
        ListItem b = peers[peers.IndexOf(a) + delta];
        Change();
        int ia = n.Items.IndexOf(a), ib = n.Items.IndexOf(b);
        (n.Items[ia], n.Items[ib]) = (n.Items[ib], n.Items[ia]);
    }

    /// <summary>Moves a row to another IPO section (to the end of it).</summary>
    public void SetItemSection(Node n, string itemId, string? section)
    {
        if (n.Items?.FirstOrDefault(i => i.Id == itemId) is not { } item || IpoSections.Of(section) == IpoSections.Of(item.Section)) return;
        Change();
        n.Items.Remove(item);
        item.Section = IpoSections.Of(section);
        n.Items.Insert(n.Items.FindLastIndex(i => IpoSections.IndexOf(i.Section) <= IpoSections.IndexOf(item.Section)) + 1, item);
        FitPanel(n);
    }

    public void SetItemIcon(Node n, ListItem item, string? icon)
    {
        if (item.Icon == icon) return;
        Change();
        item.Icon = icon;
    }

    /// <summary>Renames an IPO section; a blank title goes back to the default.</summary>
    public void SetSectionTitle(Node n, string section, string? title)
    {
        int i = IpoSections.IndexOf(section);
        List<string> titles = n.SectionTitles ?? new List<string>();
        while (titles.Count < IpoSections.All.Count) titles.Add("");
        titles[i] = string.IsNullOrWhiteSpace(title) || title.Trim() == IpoSections.DefaultTitle(section) ? "" : title.Trim();
        n.SectionTitles = titles.All(string.IsNullOrEmpty) ? null : titles;
        MarkDirty();
    }

    /// <summary>IPO sections side by side (false) or stacked top to bottom (true). Resizes to fit.</summary>
    public void SetStacked(Node n, bool stacked)
    {
        if (n.Stacked == stacked) return;
        Change();
        n.Stacked = stacked;
        if (!stacked) n.W = Math.Max(n.W, NodeTypes.Get(NodeTypes.Ipo).W);
        else n.W = Math.Min(n.W, NodeTypes.Get(NodeTypes.List).W + 60);
        FitPanel(n);
    }

    /// <summary>The rows a row is ordered among: the whole list, or its IPO section.</summary>
    static List<ListItem> Peers(Node n, string itemId)
    {
        if (n.Items is null) return new List<ListItem>();
        if (n.Type != NodeTypes.Ipo) return n.Items;
        string section = IpoSections.Of(n.Items.FirstOrDefault(i => i.Id == itemId)?.Section);
        return PanelGeometry.InSection(n, section);
    }

    /// <summary>After a type change: a new list / IPO node gets starter rows (or keeps the rows it had), and a node that
    /// stops being one drops its rows, with connectors on them moving to the node itself.</summary>
    void RetypeRows(Node n)
    {
        if (n.Kind.IsPanel)
        {
            n.Items ??= n.Kind.NewItems() ?? new List<ListItem>();
            foreach (ListItem i in n.Items) i.Section = n.Type == NodeTypes.Ipo ? IpoSections.Of(i.Section) : null;
            if (n.Type == NodeTypes.Ipo) n.Items = n.Items.OrderBy(i => IpoSections.IndexOf(i.Section)).ToList();
            FitPanel(n);
            return;
        }
        n.Items = null;
        n.SectionTitles = null;
        n.Stacked = false;
        foreach (Edge e in Diagram.Edges)
        {
            if (e.From == n.Id) e.FromItem = null;
            if (e.To == n.Id) e.ToItem = null;
        }
    }
}
