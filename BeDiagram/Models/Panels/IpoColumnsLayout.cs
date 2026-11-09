using System.Globalization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Panels;

/// <summary>An IPO box with Input | Process | Output side by side, as equal columns with a divider between them.</summary>
public class IpoColumnsLayout : IpoLayout
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>As tall as the fullest section.</summary>
    public override double NeededHeight(Node n) =>
        Header + SectionHead + Stack(IpoSections.All.Max(s => n.RowsIn(s).Count)) + Pad;

    /// <summary>Three usable columns.</summary>
    public override double MinWidth(Node n) => 3 * 110 + 2 * Pad + 2 * ColGap;

    protected override Rect SectionArea(Node n, int k)
    {
        double colW = (n.W - 2 * Pad - 2 * ColGap) / 3;
        return new Rect(n.X + Pad + k * (colW + ColGap), n.Y + Header, colW, n.H - Header - Pad);
    }

    /// <summary>Title centred over the column; a vertical divider down the gap before it.</summary>
    protected override PanelSection Section(Node n, int k, Rect area, string title)
    {
        string? divider = k > 0 ? $"M{F(area.X - ColGap / 2)},{F(n.Y + Header)} V{F(n.Y + n.H)}" : null;
        return new PanelSection(IpoSections.All[k], title, area.X + area.W / 2, area.Y + SectionHead / 2 + 5, "middle", divider);
    }
}
