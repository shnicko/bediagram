using System.Globalization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;

namespace BeDiagram.Models;

/// <summary>
/// Layout of list and input–process–output nodes ("panels"): a title band, then rows of a fixed height. A list stacks
/// its rows; an IPO box puts them in three sections, side by side or (<see cref="Node.Stacked"/>) top to bottom.
/// The canvas and the SVG export both draw from these results, and connectors find a row's box here
/// (<see cref="End"/>, <see cref="Ends"/>), so a row ends up in the same place everywhere.
/// </summary>
public static class PanelGeometry
{
    /// <summary>Height of the title band.</summary>
    public const double Header = 36;
    /// <summary>Height of one row (title line + description line).</summary>
    public const double RowH = 44;
    public const double RowGap = 6;
    /// <summary>Space between the panel's edge and its rows.</summary>
    public const double Pad = 10;
    /// <summary>Height of an IPO section's title strip.</summary>
    public const double SectionHead = 24;
    /// <summary>Space between IPO columns (the divider runs down its middle).</summary>
    public const double ColGap = 12;
    public const double IconSize = 18;
    public const double TitleFontSize = 12.5;
    public const double SectionFontSize = 11.5;
    /// <summary>Space an empty list or section keeps open, so it still reads as a place to add rows.</summary>
    const double EmptyRoom = 20;
    const double TitleCharWidth = 6.9;
    const double DescCharWidth = 6.1;

    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    static bool IsIpo(Node n) => n.Type == NodeTypes.Ipo;

    /// <summary>The rows, or none (a panel loaded without any, or not a panel).</summary>
    static IReadOnlyList<ListItem> ItemsOf(Node n) => n.Items ?? (IReadOnlyList<ListItem>)Array.Empty<ListItem>();

    /// <summary>An IPO section's rows, in order.</summary>
    public static List<ListItem> InSection(Node n, string section) =>
        ItemsOf(n).Where(i => IpoSections.Of(i.Section) == section).ToList();

    /// <summary>An IPO section's title: the renamed one, or the default.</summary>
    public static string SectionTitle(Node n, string section)
    {
        int i = IpoSections.IndexOf(section);
        string? t = n.SectionTitles is { } titles && i < titles.Count ? titles[i] : null;
        return string.IsNullOrWhiteSpace(t) ? IpoSections.DefaultTitle(section) : t;
    }

    // ───────────── Sizes ─────────────

    static double Stack(int count) => count == 0 ? EmptyRoom : count * RowH + (count - 1) * RowGap;

    /// <summary>The height that fits the rows exactly (the editor sets it after every row change).</summary>
    public static double NeededHeight(Node n)
    {
        if (!IsIpo(n)) return Header + Pad + Stack(ItemsOf(n).Count) + Pad;
        if (!n.Stacked) return Header + SectionHead + Stack(IpoSections.All.Max(s => InSection(n, s).Count)) + Pad;
        return Header + IpoSections.All.Sum(s => SectionHead + Stack(InSection(n, s).Count) + Pad);
    }

    /// <summary>The narrowest the panel can be (three usable columns for a side-by-side IPO box).</summary>
    public static double MinWidth(Node n) => IsIpo(n) && !n.Stacked ? 3 * 110 + 2 * Pad + 2 * ColGap : 160;

    // ───────────── Title band ─────────────

    /// <summary>The tinted band behind the title, rounded on the top corners like the outline.</summary>
    public static string HeaderBandPath(Node n)
    {
        double x = n.X, y = n.Y, w = n.W, r = PanelOutline.Radius;
        return $"M{F(x)},{F(y + Header)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} H{F(x + w - r)} Q{F(x + w)},{F(y)} {F(x + w)},{F(y + r)} V{F(y + Header)} Z";
    }

    /// <summary>The title area (the inline label editor sits here).</summary>
    public static Rect HeaderArea(Node n) => new Rect(n.X, n.Y, n.W, Header);

    /// <summary>The title as a node label layout: one line, centred in the band, cut short with … when too long.</summary>
    public static LabelLayout HeaderLabel(Node n, string label)
    {
        string text = Fit(label.Replace("\r", "").Replace("\n", " "), n.W - 28, 7.4);
        return new LabelLayout(new List<string> { text }, n.X + n.W / 2, n.Y + Header / 2 + 4.5, false, false, 0, 0, 0);
    }

    // ───────────── Sections and rows ─────────────

    /// <summary>The IPO section titles and dividers (empty for a list).</summary>
    public static List<PanelSection> Sections(Node n)
    {
        List<PanelSection> result = new List<PanelSection>();
        if (!IsIpo(n)) return result;
        for (int k = 0; k < IpoSections.All.Count; k++)
        {
            string s = IpoSections.All[k];
            Rect area = SectionArea(n, k);
            string? divider = null;
            if (n.Stacked)
            {
                if (k > 0) divider = $"M{F(n.X)},{F(area.Y)} H{F(n.X + n.W)}";
                result.Add(new PanelSection(s, Fit(SectionTitle(n, s), area.W - 4, 6.8), area.X + 2, area.Y + SectionHead / 2 + 5, "start", divider));
            }
            else
            {
                if (k > 0) divider = $"M{F(area.X - ColGap / 2)},{F(n.Y + Header)} V{F(n.Y + n.H)}";
                result.Add(new PanelSection(s, Fit(SectionTitle(n, s), area.W - 4, 6.8), area.X + area.W / 2, area.Y + SectionHead / 2 + 5, "middle", divider));
            }
        }
        return result;
    }

    /// <summary>An IPO section's area: its title strip followed by its rows.</summary>
    static Rect SectionArea(Node n, int k)
    {
        if (n.Stacked)
        {
            double y = n.Y + Header;
            for (int i = 0; i < k; i++) y += SectionHead + Stack(InSection(n, IpoSections.All[i]).Count) + Pad;
            return new Rect(n.X + Pad, y, n.W - 2 * Pad, SectionHead + Stack(InSection(n, IpoSections.All[k]).Count));
        }
        double colW = (n.W - 2 * Pad - 2 * ColGap) / 3;
        return new Rect(n.X + Pad + k * (colW + ColGap), n.Y + Header, colW, n.H - Header - Pad);
    }

    /// <summary>Every row, in drawing order, with where its text and icon go.</summary>
    public static List<PanelRow> Rows(Node n)
    {
        List<PanelRow> rows = new List<PanelRow>();
        if (!n.Kind.IsPanel) return rows;
        if (!IsIpo(n))
        {
            double top = n.Y + Header + Pad;
            foreach (ListItem item in ItemsOf(n))
            {
                rows.Add(Row(item, n.X + Pad, top, n.W - 2 * Pad));
                top += RowH + RowGap;
            }
            return rows;
        }
        for (int k = 0; k < IpoSections.All.Count; k++)
        {
            Rect area = SectionArea(n, k);
            double top = area.Y + SectionHead;
            foreach (ListItem item in InSection(n, IpoSections.All[k]))
            {
                rows.Add(Row(item, area.X, top, area.W));
                top += RowH + RowGap;
            }
        }
        return rows;
    }

    static PanelRow Row(ListItem item, double x, double y, double w)
    {
        bool icon = Icons.Exists(item.Icon);
        double textX = x + (icon ? 12 + IconSize + 8 : 12);
        double room = x + w - 10 - textX;
        string title = Fit(string.IsNullOrWhiteSpace(item.Label) ? "Item" : item.Label.Replace("\n", " "), room, TitleCharWidth);
        string? desc = item.Description is { } d ? Fit(d.Replace("\r", "").Replace("\n", " "), room, DescCharWidth) : null;
        double titleY = desc is null ? y + RowH / 2 + 4.5 : y + 18;
        return new PanelRow(item, new Rect(x, y, w, RowH), title, desc, textX, titleY, y + 33,
                            icon, x + 12, y + (RowH - IconSize) / 2);
    }

    /// <summary>Cuts text to fit a width, ending in … when it had to be cut.</summary>
    static string Fit(string text, double width, double charWidth)
    {
        int max = Math.Max(3, (int)(width / charWidth));
        text = text.Trim();
        return text.Length <= max ? text : text[..(max - 1)].TrimEnd() + "…";
    }

    // ───────────── Connecting to rows ─────────────

    /// <summary>The row of <paramref name="n"/> under a world point, as a connectable box (null when not on a row).</summary>
    public static ItemBox? RowAt(Node n, double x, double y)
    {
        foreach (PanelRow r in Rows(n))
            if (x >= r.Box.X && x <= r.Box.X + r.Box.W && y >= r.Box.Y && y <= r.Box.Y + r.Box.H)
                return Box(n, r);
        return null;
    }

    static ItemBox Box(Node n, PanelRow r) => new ItemBox(n.Id, r.Item.Id, r.Box.X, r.Box.Y, r.Box.W, r.Box.H);

    /// <summary>What a connector end attaches to: the row <paramref name="itemId"/> of a panel, or the box itself
    /// (no row asked for, not a panel, or the row has gone).</summary>
    public static IBox End(IBox box, string? itemId)
    {
        if (itemId is null || box is not Node n || !n.Kind.IsPanel) return box;
        PanelRow? row = Rows(n).FirstOrDefault(r => r.Item.Id == itemId);
        return row is null ? box : Box(n, row);
    }

    /// <summary>Both ends of a connector (rows where it names them), or null when either item is missing.</summary>
    public static (IBox A, IBox B)? Ends(Edge e, Func<string, IBox?> find) =>
        find(e.From) is { } a && find(e.To) is { } b ? (End(a, e.FromItem), End(b, e.ToItem)) : null;

    /// <summary>The ● ports on each row: the middle of its left and right sides.</summary>
    public static IEnumerable<(double X, double Y, string ItemId)> RowPorts(Node n)
    {
        foreach (PanelRow r in Rows(n))
        {
            double cy = r.Box.Y + r.Box.H / 2;
            yield return (r.Box.X, cy, r.Item.Id);
            yield return (r.Box.X + r.Box.W, cy, r.Item.Id);
        }
    }

    /// <summary>The sides a connector end can be pinned to: left / right on a row, all four otherwise.</summary>
    public static IReadOnlyList<string> SidesOf(IBox end) => end is ItemBox ? Sides.LeftRight : Sides.All;

    /// <summary>A row end leaves from its left or right side only: a pinned left / right is kept, anything else
    /// becomes the side facing the other end.</summary>
    public static string RowSide(IBox row, IBox other, string? side) =>
        side is Sides.Left or Sides.Right ? side
        : other.X + other.W / 2 < row.X + row.W / 2 ? Sides.Left : Sides.Right;
}
