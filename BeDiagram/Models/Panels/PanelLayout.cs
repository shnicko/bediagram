using System.Globalization;
using System.Net;
using System.Text;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Models.Panels;

/// <summary>
/// What every panel layout shares: the sizes, the title band, how one row is laid out, the SVG markup, and finding a
/// row for a connector. Subclasses decide only where the rows and sections go and how big the panel must be.
/// </summary>
public abstract class PanelLayout : IPanelLayout
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
    public const string RowText = "#1f2933";
    public const string RowDesc = "#55606e";
    /// <summary>Space an empty list or section keeps open, so it still reads as a place to add rows.</summary>
    const double EmptyRoom = 20;
    const double TitleCharWidth = 6.9;
    const double DescCharWidth = 6.1;
    /// <summary>Width of the number gutter on the left of a numbered row: one digit, or two and more.</summary>
    const double NumberGutter = 30;
    const double NumberGutterWide = 38;
    /// <summary>Space between a row's left edge (or the number divider) and its icon or text.</summary>
    const double RowInset = 12;
    const double AfterDivider = 10;

    static string E(string s)
    {
        return WebUtility.HtmlEncode(s ?? "");
    }

    public abstract double NeededHeight(Node n);

    public abstract double MinWidth(Node n);

    public virtual List<PanelSection> Sections(Node n)
    {
        return new List<PanelSection>();
    }

    public abstract List<PanelRow> Rows(Node n);

    /// <summary>Height of <paramref name="count"/> rows one under another (an empty stack keeps a little room).</summary>
    protected double Stack(int count)
    {
        if (count == 0)
        {
            return EmptyRoom;
        }
        return count * RowH + (count - 1) * RowGap;
    }

    /// <summary>Rows <paramref name="items"/> one under another, from (<paramref name="x"/>, <paramref name="top"/>).
    /// When <paramref name="numbered"/>, each row shows its place in this stack, starting at 1.</summary>
    protected void AddStack(List<PanelRow> rows, IReadOnlyList<ListItem> items, double x, double top, double w, bool numbered)
    {
        // Every row in a stack gets the same gutter, so the dividers line up once there are 10 or more rows.
        double gutter = 0;
        if (numbered)
        {
            gutter = items.Count >= 10 ? NumberGutterWide : NumberGutter;
        }
        for (int i = 0; i < items.Count; i++)
        {
            string? number = numbered ? (i + 1).ToString(CultureInfo.InvariantCulture) : null;
            rows.Add(Row(items[i], x, top, w, number, gutter));
            top += RowH + RowGap;
        }
    }

    /// <summary>One row: number gutter (when <paramref name="number"/> is set), then the icon, then the title and description.</summary>
    PanelRow Row(ListItem item, double x, double y, double w, string? number, double gutter)
    {
        bool icon = Icons.Exists(item.Icon);
        double contentX = x + RowInset;
        if (number is not null)
        {
            contentX = x + gutter + AfterDivider;
        }
        double textX = icon ? contentX + IconSize + 8 : contentX;
        double room = x + w - 10 - textX;
        string label = string.IsNullOrWhiteSpace(item.Label) ? "Item" : item.Label.Replace("\n", " ");
        string title = Fit(label, room, TitleCharWidth);
        string? desc = null;
        if (item.Description is not null)
        {
            desc = Fit(item.Description.Replace("\r", "").Replace("\n", " "), room, DescCharWidth);
        }
        double titleY = desc is null ? y + RowH / 2 + 4.5 : y + 18;
        return new PanelRow(item, new Rect(x, y, w, RowH), title, desc, textX, titleY, y + 33,
                            icon, contentX, y + (RowH - IconSize) / 2, number, x + gutter / 2, x + gutter);
    }

    /// <summary>Cuts text to fit a width, ending in … when it had to be cut.</summary>
    protected string Fit(string text, double width, double charWidth)
    {
        int max = Math.Max(3, (int)(width / charWidth));
        text = text.Trim();
        if (text.Length <= max)
        {
            return text;
        }
        return text[..(max - 1)].TrimEnd() + "…";
    }

    // ───────────── Title band ─────────────

    public LabelLayout HeaderLabel(Node n, string label)
    {
        string text = Fit(label.Replace("\r", "").Replace("\n", " "), n.W - 28, 7.4);
        return new LabelLayout(new List<string> { text }, n.X + n.W / 2, n.Y + Header / 2 + 4.5, false, false, 0, 0, 0);
    }

    public Rect TitleEditRect(Node n)
    {
        return new Rect(n.X + 8, n.Y + 4, n.W - 16, Header - 8);
    }

    /// <summary>The tinted band behind the title, rounded on the top corners like the outline.</summary>
    string HeaderBandPath(Node n)
    {
        double x = n.X;
        double y = n.Y;
        double w = n.W;
        double r = PanelOutline.Radius;
        return $"M{F(x)},{F(y + Header)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} H{F(x + w - r)} Q{F(x + w)},{F(y)} {F(x + w)},{F(y + r)} V{F(y + Header)} Z";
    }

    // ───────────── SVG ─────────────

    public string Body(Node n, NodeStyle st)
    {
        StringBuilder sb = new StringBuilder();
        // On a solid fill the band and dividers are white, otherwise a tint of the outline colour.
        string tint = n.Filled ? "#ffffff" : st.Stroke;
        sb.Append($"<path d=\"{HeaderBandPath(n)}\" fill=\"{tint}\" fill-opacity=\"{(n.Filled ? "0.18" : "0.14")}\"/>");
        foreach (PanelSection s in Sections(n))
        {
            if (s.Divider is not null)
            {
                sb.Append($"<path d=\"{s.Divider}\" fill=\"none\" stroke=\"{tint}\" stroke-opacity=\"0.4\" stroke-width=\"1\"/>");
            }
            sb.Append($"<text x=\"{F(s.TitleX)}\" y=\"{F(s.TitleY)}\" font-size=\"{F(SectionFontSize)}\" font-weight=\"600\" text-anchor=\"{s.Anchor}\" fill=\"{(n.Filled ? "#ffffff" : st.Stroke)}\">{E(s.Title)}</text>");
        }
        foreach (PanelRow r in Rows(n))
        {
            sb.Append($"<rect x=\"{F(r.Box.X)}\" y=\"{F(r.Box.Y)}\" width=\"{F(r.Box.W)}\" height=\"{F(r.Box.H)}\" rx=\"8\" fill=\"#ffffff\" stroke=\"{st.Stroke}\" stroke-opacity=\"0.6\" stroke-width=\"1.2\"/>");
            // Numbered rows: the number centred in a gutter on the left, with a faint divider after it.
            if (r.Number is not null)
            {
                sb.Append($"<text x=\"{F(r.NumberX)}\" y=\"{F(r.Box.Y + RowH / 2 + 4.5)}\" font-size=\"{F(TitleFontSize)}\" font-weight=\"600\" text-anchor=\"middle\" fill=\"{st.Stroke}\">{r.Number}</text>");
                sb.Append($"<line x1=\"{F(r.DividerX)}\" y1=\"{F(r.Box.Y + 8)}\" x2=\"{F(r.DividerX)}\" y2=\"{F(r.Box.Y + RowH - 8)}\" stroke=\"{st.Stroke}\" stroke-opacity=\"0.3\" stroke-width=\"1\"/>");
            }
            if (r.HasIcon)
            {
                sb.Append($"<g transform=\"translate({F(r.IconX)},{F(r.IconY)}) scale({F(IconSize / 24)})\" fill=\"none\" stroke=\"{st.Stroke}\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(r.Item.Icon)}</g>");
            }
            sb.Append($"<text x=\"{F(r.TextX)}\" y=\"{F(r.TitleY)}\" font-size=\"{F(TitleFontSize)}\" font-weight=\"600\" fill=\"{RowText}\">{E(r.Title)}</text>");
            if (r.Desc is not null)
            {
                sb.Append($"<text x=\"{F(r.TextX)}\" y=\"{F(r.DescY)}\" font-size=\"{F(TextLayout.DescFontSize)}\" fill=\"{RowDesc}\">{E(r.Desc)}</text>");
            }
        }
        return sb.ToString();
    }

    // ───────────── Connecting to rows ─────────────

    public ItemBox? RowAt(Node n, double x, double y)
    {
        foreach (PanelRow r in Rows(n))
        {
            if (x >= r.Box.X && x <= r.Box.X + r.Box.W && y >= r.Box.Y && y <= r.Box.Y + r.Box.H)
            {
                return Box(n, r);
            }
        }
        return null;
    }

    public ItemBox? RowBox(Node n, string itemId)
    {
        foreach (PanelRow r in Rows(n))
        {
            if (r.Item.Id == itemId)
            {
                return Box(n, r);
            }
        }
        return null;
    }

    ItemBox Box(Node n, PanelRow r)
    {
        return new ItemBox(n.Id, r.Item.Id, r.Box.X, r.Box.Y, r.Box.W, r.Box.H);
    }

    public IEnumerable<(double X, double Y, string ItemId)> RowPorts(Node n)
    {
        List<(double X, double Y, string ItemId)> ports = new List<(double X, double Y, string ItemId)>();
        foreach (PanelRow r in Rows(n))
        {
            double cy = r.Box.Y + r.Box.H / 2;
            ports.Add((r.Box.X, cy, r.Item.Id));
            ports.Add((r.Box.X + r.Box.W, cy, r.Item.Id));
        }
        return ports;
    }
}
