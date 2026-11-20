using System.Text.Json.Serialization;
using BeDiagram.Models.Location;
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
    /// <summary>Placed by its top-left corner, and moving it carries what's inside. Not saved.</summary>
    [JsonIgnore]
    public IPlacement Placement
    {
        get
        {
            return Placements.Group;
        }
    }
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
    /// (inside at the top, or centred under the box when the title is at the bottom); "tag" = title in an outlined
    /// tag in the corner, as for a UML package.</summary>
    public string HeaderStyle { get; set; } = GroupLook.Band;

    /// <summary>A copy with a new id; every other field (title, colour, fill, border, title style, link, parent group) is kept.</summary>
    public Group Copy()
    {
        Group c = (Group)MemberwiseClone();
        c.Id = Node.NewId("g");
        return c;
    }

    // ───────────── Title geometry (top / bottom, band / plain text) ─────────────


    /// <summary>A plain-text title at the bottom sits under the box, outside it.</summary>
    public bool TitleBelowBox()
    {
        return HeaderPos == GroupLook.Bottom && HeaderStyle == GroupLook.Text;
    }

    /// <summary>Space the title takes inside the top of the group.</summary>
    public double InsetTop()
    {
        if (HeaderPos == GroupLook.Bottom)
        {
            return 0;
        }
        return Header;
    }

    /// <summary>Space the title takes inside the bottom of the group (a band at the bottom).</summary>
    public double InsetBottom()
    {
        if (HeaderPos == GroupLook.Bottom && HeaderStyle != GroupLook.Text)
        {
            return Header;
        }
        return 0;
    }

    /// <summary>The title area: the band (top or bottom), the top strip for a plain title, or the strip under the box.
    /// Used to draw the band, as the drag handle, and for the inline title editor.</summary>
    public (double X, double Y, double W, double H) TitleArea()
    {
        if (TitleBelowBox())
        {
            return (X, Y + H + 2, W, Header - 4);
        }
        if (HeaderPos == GroupLook.Bottom)
        {
            return (X, Y + H - Header, W, Header);
        }
        return (X, Y, W, Header);
    }

    /// <summary>Title baseline and anchor: left in a band or top strip, centred under the box.</summary>
    public (double X, double Y, string Anchor) TitlePos()
    {
        (double x, double y, double w, double h) = TitleArea();
        if (TitleBelowBox())
        {
            return (x + w / 2, y + 17, "middle");
        }
        return (x + 14, y + 21, "start");
    }

    /// <summary>The tinted band behind the title (rounded on the outside corners), the name tag, or null for a
    /// plain-text title.</summary>
    public string? BandPath()
    {
        if (HeaderStyle == GroupLook.Tag)
        {
            return TagPath();
        }
        if (HeaderStyle != GroupLook.Band)
        {
            return null;
        }
        double x = X;
        double w = W;
        double r = 10;
        if (HeaderPos == GroupLook.Bottom)
        {
            double y0 = Y + H - Header;
            double y1 = Y + H;
            return $"M{F(x)},{F(y0)} V{F(y1 - r)} Q{F(x)},{F(y1)} {F(x + r)},{F(y1)} H{F(x + w - r)} Q{F(x + w)},{F(y1)} {F(x + w)},{F(y1 - r)} V{F(y0)} Z";
        }
        double y = Y;
        return $"M{F(x)},{F(y + Header)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} H{F(x + w - r)} Q{F(x + w)},{F(y)} {F(x + w)},{F(y + r)} V{F(y + Header)} Z";
    }

    /// <summary>The band is outlined (the name tag), not only tinted.</summary>
    public bool BandOutlined()
    {
        return HeaderStyle == GroupLook.Tag;
    }

    /// <summary>The name tag: as wide as the title, in the top-left corner (bottom-left when the title is at the
    /// bottom), with its inner corner cut off like a UML frame's name.</summary>
    string TagPath()
    {
        const double cut = 8;
        const double r = 10;
        double x = X;
        // Width from the title (13px semibold), never wider than the group.
        double w = Math.Min(W, Math.Max(60, Title.Length * 7.6 + 28 + cut));
        if (HeaderPos == GroupLook.Bottom)
        {
            double y0 = Y + H - Header;
            double y1 = Y + H;
            return $"M{F(x)},{F(y0)} H{F(x + w - cut)} L{F(x + w)},{F(y0 + cut)} V{F(y1)} H{F(x + r)} Q{F(x)},{F(y1)} {F(x)},{F(y1 - r)} Z";
        }
        double y = Y;
        return $"M{F(x)},{F(y + Header)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} H{F(x + w)} V{F(y + Header - cut)} L{F(x + w - cut)},{F(y + Header)} Z";
    }

    /// <summary>Fill opacity of the group body (0 = no fill; the body stays clickable).</summary>
    public double FillOpacity()
    {
        if (Filled)
        {
            return 0.6;
        }
        return 0;
    }
}
