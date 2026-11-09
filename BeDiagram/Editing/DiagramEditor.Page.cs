using BeDiagram.Models;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Editing;

/// <summary>Page setup and flow direction.</summary>
public partial class DiagramEditor
{
    // ───────────── Page ─────────────

    /// <summary>Items not fully inside the page (null when there is no page).</summary>
    public List<IBox> OutsidePage()
    {
        if (!Diagram.Page.Enabled) return new();
        double W = Diagram.Page.WidthPx, H = Diagram.Page.HeightPx;
        return Diagram.Nodes.Cast<IBox>().Concat(Diagram.TextBoxes).Concat(Diagram.Groups)
            .Where(b => b.X < -0.5 || b.Y < -0.5 || b.X + b.W > W + 0.5 || b.Y + b.H + Geometry.ExtraBelow(b) > H + 0.5)
            .ToList();
    }

    public bool ContentTooBigForPage()
    {
        if (!Diagram.Page.Enabled || Geometry.ContentBounds(Diagram) is not { } cb) return false;
        double m = Diagram.Page.MarginPx;
        return cb.W > Diagram.Page.WidthPx - 2 * m + 0.5 || cb.H > Diagram.Page.HeightPx - 2 * m + 0.5;
    }

    public void CentreOnPage() => CentreOnPage(checkpoint: true);

    /// <summary>Moves the whole drawing so it is centred inside the page margins.</summary>
    public void CentreOnPage(bool checkpoint)
    {
        if (!Diagram.Page.Enabled || Geometry.ContentBounds(Diagram) is not { } cb) return;
        if (checkpoint) Change();
        double W = Diagram.Page.WidthPx, H = Diagram.Page.HeightPx, m = Diagram.Page.MarginPx;
        double dx = SnapV(m + (W - 2 * m - cb.W) / 2 - cb.X);
        double dy = SnapV(m + (H - 2 * m - cb.H) / 2 - cb.Y);
        foreach (IBox b in Diagram.Nodes.Cast<IBox>().Concat(Diagram.TextBoxes).Concat(Diagram.Groups)) { b.X += dx; b.Y += dy; }
        if (checkpoint && ContentTooBigForPage())
            view.ShowToast("Centred — but the drawing is bigger than the printable area");
    }

    public void SelectOutsidePage()
    {
        Selection.Clear(); SelectedEdgeId = null;
        Selection.AddRange(OutsidePage().Select(b => b.Id));
    }

    public void PageChanged() { Dirty = true; }

    /// <summary>File menu: which way new steps go (<see cref="FlowDirections"/>). One undo step.</summary>
    public void SetFlow(FlowDirection flow)
    {
        if (Diagram.Flow == flow) return;
        Change();
        Diagram.Flow = flow;
    }

    /// <summary>Arrange menu: lock or unlock the size of the swim lanes (<see cref="Diagram.LockLanes"/>). One undo step.</summary>
    public void SetLockLanes(bool locked)
    {
        if (Diagram.LockLanes == locked) return;
        Change();
        Diagram.LockLanes = locked;
    }
}
