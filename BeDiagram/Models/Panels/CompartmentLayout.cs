using System.Net;
using System.Text;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Kinds;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Models.Panels;

/// <summary>
/// UML-style compartments for classes, interfaces, enumerations and entities: a name compartment (with a stereotype
/// line such as «interface» over the name when the kind has one), then one compartment per section with a divider
/// along its top and single-line rows. Rows span the node's full width, so connectors attach to a row at the outline.
/// What a row says (UML text, or an entity's key and type) comes from the <see cref="ModelKind"/>.
/// </summary>
public class CompartmentLayout : PanelLayout
{
    /// <summary>Height of one row.</summary>
    public const double LineH = 20;
    /// <summary>Space above the first row and below the last one in a compartment.</summary>
    public const double CompPad = 5;
    /// <summary>Height an empty compartment keeps, so it still reads as a compartment.</summary>
    const double EmptyH = 12;
    /// <summary>Height of the name compartment with a stereotype line over the name.</summary>
    const double TallHeader = 50;
    /// <summary>Width of an entity's PK / FK gutter.</summary>
    const double Gutter = 40;
    const double Inset = 10;
    const double RowFontSize = 12;
    const double RowCharWidth = 6.6;
    const double KeyFontSize = 10.5;
    const double StereoFontSize = 11;
    /// <summary>Corner radius, matching <see cref="Shapes.RectOutline"/>.</summary>
    const double Radius = 5;

    readonly ModelKind kind;

    public CompartmentLayout(ModelKind kind)
    {
        this.kind = kind;
    }

    static string E(string s)
    {
        return WebUtility.HtmlEncode(s ?? "");
    }

    /// <summary>Height of the name compartment: taller when there's a stereotype line.</summary>
    double HeadH(Node n)
    {
        if (kind.Stereotype(n) is not null)
        {
            return TallHeader;
        }
        return Header;
    }

    /// <summary>The compartments shown: none when <see cref="Node.NameOnly"/>.</summary>
    List<RowSection> Shown(Node n)
    {
        List<RowSection> result = new List<RowSection>();
        if (n.NameOnly)
        {
            return result;
        }
        foreach (RowSection s in kind.Sections)
        {
            result.Add(s);
        }
        return result;
    }

    double CompartmentH(int rows)
    {
        if (rows == 0)
        {
            return EmptyH;
        }
        return CompPad + rows * LineH + CompPad;
    }

    public override double NeededHeight(Node n)
    {
        double h = HeadH(n);
        foreach (RowSection s in Shown(n))
        {
            h += CompartmentH(kind.RowsIn(n, s).Count);
        }
        return h;
    }

    public override double MinWidth(Node n)
    {
        return 140;
    }

    /// <summary>Each shown compartment and its rows, top to bottom.</summary>
    public List<ModelCompartment> Compartments(Node n)
    {
        List<ModelCompartment> result = new List<ModelCompartment>();
        double y = n.Y + HeadH(n);
        double textX = n.X + Inset;
        if (kind.KeyGutter)
        {
            textX = n.X + Gutter + Inset;
        }
        double right = n.X + n.W - Inset;
        foreach (RowSection s in Shown(n))
        {
            List<ListItem> items = kind.RowsIn(n, s);
            double top = y;
            List<ModelLine> lines = new List<ModelLine>();
            double rowY = top + CompPad;
            foreach (ListItem item in items)
            {
                string? type = kind.RowType(item);
                // The type takes the right-hand end of the row; the text gets what's left.
                double room = right - textX;
                if (type is not null)
                {
                    type = Fit(type, Math.Max(40, room * 0.45), RowCharWidth);
                    room -= type.Length * RowCharWidth + 12;
                }
                string text = Fit(kind.RowText(item, s), Math.Max(30, room), RowCharWidth);
                Rect box = new Rect(n.X, rowY, n.W, LineH);
                lines.Add(new ModelLine(item, box, text, textX, rowY + LineH / 2 + 4.2, type, right,
                                        kind.RowKey(item), n.X + Gutter / 2));
                rowY += LineH;
            }
            y += CompartmentH(items.Count);
            result.Add(new ModelCompartment(top, y, lines));
        }
        return result;
    }

    /// <summary>Every row as a panel row, for connectors (hit tests, ports, row ends).</summary>
    public override List<PanelRow> Rows(Node n)
    {
        List<PanelRow> rows = new List<PanelRow>();
        foreach (ModelCompartment c in Compartments(n))
        {
            foreach (ModelLine l in c.Lines)
            {
                rows.Add(new PanelRow(l.Item, l.Box, l.Text, null, l.TextX, l.BaseY, l.BaseY, false, 0, 0, null, 0, 0));
            }
        }
        return rows;
    }

    // ───────────── Name compartment ─────────────

    /// <summary>The name, centred in the name compartment (under the stereotype line when there is one).</summary>
    public override LabelLayout HeaderLabel(Node n, string label)
    {
        string text = Fit(label.Replace("\r", "").Replace("\n", " "), n.W - 20, 7.4);
        double baseY = n.Y + Header / 2 + 4.5;
        if (kind.Stereotype(n) is not null)
        {
            baseY = n.Y + 36;
        }
        return new LabelLayout(new List<string> { text }, n.X + n.W / 2, baseY, false, false, 0, 0, 0);
    }

    public override Rect TitleEditRect(Node n)
    {
        double top = n.Y + 4;
        if (kind.Stereotype(n) is not null)
        {
            top = n.Y + 18;
        }
        return new Rect(n.X + 8, top, n.W - 16, Header - 8);
    }

    /// <summary>The tinted name compartment, rounded on the top corners like the outline (all four when name only).</summary>
    string HeadPath(Node n)
    {
        double x = n.X;
        double y = n.Y;
        double w = n.W;
        double r = Radius;
        double h = HeadH(n);
        if (Shown(n).Count == 0)
        {
            return Shapes.Paths.RoundRect(x, y, w, n.H, r);
        }
        return $"M{F(x)},{F(y + h)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} H{F(x + w - r)} Q{F(x + w)},{F(y)} {F(x + w)},{F(y + r)} V{F(y + h)} Z";
    }

    // ───────────── SVG ─────────────

    public override string Body(Node n, NodeStyle st)
    {
        StringBuilder sb = new StringBuilder();
        // On a solid fill the band, lines and row text are white, otherwise tints of the outline colour.
        string tint = st.Stroke;
        string rowText = RowText;
        string typeText = RowDesc;
        if (n.Filled)
        {
            tint = "#ffffff";
            rowText = st.Text;
            typeText = st.Text;
        }
        sb.Append($"<path d=\"{HeadPath(n)}\" fill=\"{tint}\" fill-opacity=\"{(n.Filled ? "0.18" : "0.14")}\"/>");
        string? stereotype = kind.Stereotype(n);
        if (stereotype is not null)
        {
            string s = Fit(stereotype, n.W - 20, 6.2);
            sb.Append($"<text x=\"{F(n.X + n.W / 2)}\" y=\"{F(n.Y + 17)}\" font-size=\"{F(StereoFontSize)}\" text-anchor=\"middle\" fill=\"{(n.Filled ? st.Text : st.Stroke)}\">{E(s)}</text>");
        }
        foreach (ModelCompartment c in Compartments(n))
        {
            sb.Append($"<path d=\"M{F(n.X)},{F(c.Top)} H{F(n.X + n.W)}\" fill=\"none\" stroke=\"{st.Stroke}\" stroke-width=\"1\"/>");
            // Entities: a faint line down the key gutter.
            if (kind.KeyGutter)
            {
                sb.Append($"<path d=\"M{F(n.X + Gutter)},{F(c.Top)} V{F(c.Bottom)}\" fill=\"none\" stroke=\"{tint}\" stroke-opacity=\"0.4\" stroke-width=\"1\"/>");
            }
            foreach (ModelLine l in c.Lines)
            {
                if (l.Key is not null)
                {
                    sb.Append($"<text x=\"{F(l.KeyX)}\" y=\"{F(l.BaseY - 0.5)}\" font-size=\"{F(KeyFontSize)}\" font-weight=\"700\" text-anchor=\"middle\" fill=\"{(n.Filled ? st.Text : st.Stroke)}\">{E(l.Key)}</text>");
                }
                string weight = "";
                if (l.Item.PrimaryKey)
                {
                    weight = " font-weight=\"600\"";
                }
                sb.Append($"<text x=\"{F(l.TextX)}\" y=\"{F(l.BaseY)}\" font-size=\"{F(RowFontSize)}\"{weight} fill=\"{rowText}\">{E(l.Text)}</text>");
                if (l.Type is not null)
                {
                    sb.Append($"<text x=\"{F(l.TypeX)}\" y=\"{F(l.BaseY)}\" font-size=\"{F(RowFontSize)}\" text-anchor=\"end\" fill=\"{typeText}\">{E(l.Type)}</text>");
                }
            }
        }
        return sb.ToString();
    }
}
