using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Panels;

/// <summary>A list: its rows one under another below the title band.</summary>
public class ListLayout : PanelLayout
{
    public override double NeededHeight(Node n) => Header + Pad + Stack(n.Rows.Count) + Pad;

    public override double MinWidth(Node n) => 160;

    public override List<PanelRow> Rows(Node n)
    {
        List<PanelRow> rows = new List<PanelRow>();
        AddStack(rows, n.Rows, n.X + Pad, n.Y + Header + Pad, n.W - 2 * Pad);
        return rows;
    }
}
