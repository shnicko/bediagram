using System.Text.Json.Serialization;
using BeDiagram.Models.Styles;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Models;

/// <summary>
/// A coloured container. Members are the shapes, text boxes and other groups whose GroupId points here, so groups
/// can nest (see <see cref="GroupTree"/>, the Composite view of that hierarchy).
/// </summary>
public class Group : IGroupable
{
    public const double Header = 32;
    public const double Pad = 20;

    public string Id { get; set; } = Node.NewId("g");
    public string Title { get; set; } = "Group";
    public string Color { get; set; } = "blue";
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 360;
    public double H { get; set; } = 220;
    /// <summary>The group this group sits inside (null at the top level). Left out of the file when null.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GroupId { get; set; }
    /// <summary>Name of another saved diagram (without extension) holding this group's more detailed flow. Shows a link badge.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }
    /// <summary>Tinted background (true) or no fill, just the outline (false).</summary>
    public bool Filled { get; set; } = true;
    /// <summary>Outline pattern: a <see cref="LineLook"/> pattern key ("solid", "dashed", "dotted", ...).</summary>
    public string Border { get; set; } = LineLook.Solid;
    /// <summary>Outline weight: "light", "regular" or "heavy" (see <see cref="GroupLook.Width"/>).</summary>
    public string BorderWeight { get; set; } = LineLook.Regular;
    /// <summary>Where the title goes: "top" or "bottom" (see <see cref="GroupLook"/>).</summary>
    public string HeaderPos { get; set; } = GroupLook.Top;
    /// <summary>"band" = title on a tinted strip inside the group; "text" = plain bold title
    /// (inside at the top, or centred under the box when the title is at the bottom).</summary>
    public string HeaderStyle { get; set; } = GroupLook.Band;

    /// <summary>A copy with a new id; every other field (title, colour, fill, border, title style, link, parent group) is kept.</summary>
    public Group Copy() { Group c = (Group)MemberwiseClone(); c.Id = Node.NewId("g"); return c; }

    // ───────────── Title geometry (top / bottom, band / plain text) ─────────────


    /// <summary>A plain-text title at the bottom sits under the box, outside it.</summary>
    public bool TitleBelowBox() => HeaderPos == GroupLook.Bottom && HeaderStyle == GroupLook.Text;

    /// <summary>Space the title takes inside the top of the group.</summary>
    public double InsetTop() => HeaderPos == GroupLook.Bottom ? 0 : Header;

    /// <summary>Space the title takes inside the bottom of the group (a band at the bottom).</summary>
    public double InsetBottom() => HeaderPos == GroupLook.Bottom && HeaderStyle == GroupLook.Band ? Header : 0;

    /// <summary>The title area: the band (top or bottom), the top strip for a plain title, or the strip under the box.
    /// Used to draw the band, as the drag handle, and for the inline title editor.</summary>
    public (double X, double Y, double W, double H) TitleArea()
    {
        if (TitleBelowBox()) return (X, Y + H + 2, W, Header - 4);
        if (HeaderPos == GroupLook.Bottom) return (X, Y + H - Header, W, Header);
        return (X, Y, W, Header);
    }

    /// <summary>Title baseline and anchor: left in a band or top strip, centred under the box.</summary>
    public (double X, double Y, string Anchor) TitlePos()
    {
        (double x, double y, double w, double h) = TitleArea();
        return TitleBelowBox() ? (x + w / 2, y + 17, "middle") : (x + 14, y + 21, "start");
    }

    /// <summary>The tinted band behind the title (rounded on the outside corners), or null for a plain-text title.</summary>
    public string? BandPath()
    {
        if (HeaderStyle != GroupLook.Band) return null;
        double x = X, w = W, r = 10;
        if (HeaderPos == GroupLook.Bottom)
        {
            double y0 = Y + H - Header, y1 = Y + H;
            return $"M{F(x)},{F(y0)} V{F(y1 - r)} Q{F(x)},{F(y1)} {F(x + r)},{F(y1)} H{F(x + w - r)} Q{F(x + w)},{F(y1)} {F(x + w)},{F(y1 - r)} V{F(y0)} Z";
        }
        double y = Y;
        return $"M{F(x)},{F(y + Header)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} H{F(x + w - r)} Q{F(x + w)},{F(y)} {F(x + w)},{F(y + r)} V{F(y + Header)} Z";
    }

    /// <summary>Fill opacity of the group body (0 = no fill; the body stays clickable).</summary>
    public double FillOpacity() => Filled ? 0.6 : 0;
}
