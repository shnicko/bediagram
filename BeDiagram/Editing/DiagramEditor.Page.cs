using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Text;

namespace BeDiagram.Editing;

/// <summary>Page setup and flow direction.</summary>
public partial class DiagramEditor
{
    // ───────────── Page ─────────────

    /// <summary>Every shape, text box and group, in that order.</summary>
    List<IBox> PageItems()
    {
        List<IBox> result = new List<IBox>();
        foreach (Node n in Diagram.Nodes)
        {
            result.Add(n);
        }
        foreach (TextBox t in Diagram.TextBoxes)
        {
            result.Add(t);
        }
        foreach (Group g in Diagram.Groups)
        {
            result.Add(g);
        }
        return result;
    }

    /// <summary>Items not fully inside the page (an empty list when there is no page).</summary>
    public List<IBox> OutsidePage()
    {
        List<IBox> result = new List<IBox>();
        if (!Diagram.Page.Enabled)
        {
            return result;
        }
        double W = Diagram.Page.WidthPx;
        double H = Diagram.Page.HeightPx;
        foreach (IBox b in PageItems())
        {
            bool outside = b.X < -0.5 || b.Y < -0.5 || b.X + b.W > W + 0.5 || b.Y + b.H + TextLayout.ExtraBelow(b) > H + 0.5;
            if (outside)
            {
                result.Add(b);
            }
        }
        return result;
    }

    public bool ContentTooBigForPage()
    {
        if (!Diagram.Page.Enabled)
        {
            return false;
        }
        (double X, double Y, double W, double H)? bounds = Boxes.ContentBounds(Diagram);
        if (bounds is null)
        {
            return false;
        }
        (double X, double Y, double W, double H) cb = bounds.Value;
        double m = Diagram.Page.MarginPx;
        return cb.W > Diagram.Page.WidthPx - 2 * m + 0.5 || cb.H > Diagram.Page.HeightPx - 2 * m + 0.5;
    }

    public void CentreOnPage()
    {
        CentreOnPage(checkpoint: true);
    }

    /// <summary>Moves the whole drawing so it is centred inside the page margins.</summary>
    public void CentreOnPage(bool checkpoint)
    {
        if (!Diagram.Page.Enabled)
        {
            return;
        }
        (double X, double Y, double W, double H)? bounds = Boxes.ContentBounds(Diagram);
        if (bounds is null)
        {
            return;
        }
        (double X, double Y, double W, double H) cb = bounds.Value;
        if (checkpoint)
        {
            Change();
        }
        double W = Diagram.Page.WidthPx;
        double H = Diagram.Page.HeightPx;
        double m = Diagram.Page.MarginPx;
        double dx = SnapV(m + (W - 2 * m - cb.W) / 2 - cb.X);
        double dy = SnapV(m + (H - 2 * m - cb.H) / 2 - cb.Y);
        foreach (IBox b in PageItems())
        {
            b.X += dx;
            b.Y += dy;
        }
        if (checkpoint && ContentTooBigForPage())
        {
            view.ShowToast("Centred — but the drawing is bigger than the printable area");
        }
    }

    public void SelectOutsidePage()
    {
        Selection.Clear();
        SelectedEdgeId = null;
        foreach (IBox b in OutsidePage())
        {
            Selection.Add(b.Id);
        }
    }

    public void PageChanged()
    {
        Dirty = true;
    }

    /// <summary>File menu: which way new steps go (<see cref="FlowDirections"/>). One undo step.</summary>
    public void SetFlow(FlowDirection flow)
    {
        if (Diagram.Flow == flow)
        {
            return;
        }
        Change();
        Diagram.Flow = flow;
    }

    /// <summary>Arrange menu: lock or unlock the size of the swim lanes (<see cref="Diagram.LockLanes"/>). One undo step.</summary>
    public void SetLockLanes(bool locked)
    {
        if (Diagram.LockLanes == locked)
        {
            return;
        }
        Change();
        Diagram.LockLanes = locked;
    }
}
