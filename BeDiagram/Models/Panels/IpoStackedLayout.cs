using System.Globalization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Panels;

/// <summary>An IPO box with its sections one under another (narrower, for slides and portrait pages).</summary>
public class IpoStackedLayout : IpoLayout
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Every section's title strip and rows, one after another.</summary>
    public override double NeededHeight(Node n) =>
        Header + IpoSections.All.Sum(s => SectionHead + Stack(n.RowsIn(s).Count) + Pad);

    public override double MinWidth(Node n) => 160;

    protected override Rect SectionArea(Node n, int k)
    {
        double y = n.Y + Header;
        for (int i = 0; i < k; i++) y += SectionHead + Stack(n.RowsIn(IpoSections.All[i]).Count) + Pad;
        return new Rect(n.X + Pad, y, n.W - 2 * Pad, SectionHead + Stack(n.RowsIn(IpoSections.All[k]).Count));
    }

    /// <summary>Title on the left of its strip; a full-width divider above every section but the first.</summary>
    protected override PanelSection Section(Node n, int k, Rect area, string title)
    {
        string? divider = k > 0 ? $"M{F(n.X)},{F(area.Y)} H{F(n.X + n.W)}" : null;
        return new PanelSection(IpoSections.All[k], title, area.X + 2, area.Y + SectionHead / 2 + 5, "start", divider);
    }
}
