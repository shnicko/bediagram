using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Panels;

/// <summary>
/// An input–process–output box: three sections (<see cref="IpoSections"/>), each a title strip over its rows.
/// Subclasses place the sections: side by side (<see cref="IpoColumnsLayout"/>) or one under another
/// (<see cref="IpoStackedLayout"/>).
/// </summary>
public abstract class IpoLayout : PanelLayout
{
    /// <summary>Section <paramref name="k"/>'s area: its title strip followed by its rows.</summary>
    protected abstract Rect SectionArea(Node n, int k);

    /// <summary>Section <paramref name="k"/>'s title (already fitted to <paramref name="area"/>) and the divider before it.</summary>
    protected abstract PanelSection Section(Node n, int k, Rect area, string title);

    public override List<PanelSection> Sections(Node n)
    {
        List<PanelSection> result = new List<PanelSection>();
        for (int k = 0; k < IpoSections.All.Count; k++)
        {
            Rect area = SectionArea(n, k);
            result.Add(Section(n, k, area, Fit(n.SectionTitle(IpoSections.All[k]), area.W - 4, 6.8)));
        }
        return result;
    }

    public override List<PanelRow> Rows(Node n)
    {
        List<PanelRow> rows = new List<PanelRow>();
        for (int k = 0; k < IpoSections.All.Count; k++)
        {
            Rect area = SectionArea(n, k);
            AddStack(rows, n.RowsIn(IpoSections.All[k]), area.X, area.Y + SectionHead, area.W);
        }
        return rows;
    }
}
