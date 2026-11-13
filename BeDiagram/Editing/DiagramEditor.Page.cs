using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;

namespace BeDiagram.Editing;

/// <summary>Page setup, the page title and description, and flow direction.</summary>
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

    /// <summary>Items not fully inside the page, or reaching up into the page title (an empty list when there is no page).</summary>
    public List<IBox> OutsidePage()
    {
        List<IBox> result = new List<IBox>();
        if (!Diagram.Page.Enabled)
        {
            return result;
        }
        double W = Diagram.Page.WidthPx;
        double H = Diagram.Page.HeightPx;
        // With a title shown, the top of the page is where the header ends (the gap under it is allowed).
        double top = 0;
        if (PageHeader.Shown(Diagram))
        {
            top = Diagram.Page.MarginPx + PageHeader.Height(Diagram) - PageHeader.Gap;
        }
        foreach (IBox b in PageItems())
        {
            bool outside = b.X < -0.5 || b.Y < top - 0.5 || b.X + b.W > W + 0.5 || b.Y + b.H + TextLayout.ExtraBelow(b) > H + 0.5;
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
        Rect area = PageHeader.DrawingArea(Diagram);
        return cb.W > area.W + 0.5 || cb.H > area.H + 0.5;
    }

    public void CentreOnPage()
    {
        CentreOnPage(checkpoint: true);
    }

    /// <summary>Moves the whole drawing so it is centred inside the page margins, below the page title.</summary>
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
        Rect area = PageHeader.DrawingArea(Diagram);
        double dx = SnapV(area.X + (area.W - cb.W) / 2 - cb.X);
        double dy = SnapV(area.Y + (area.H - cb.H) / 2 - cb.Y);
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

    // ───────────── Page title and description ─────────────

    /// <summary>Sets <see cref="Diagram.Title"/>; blank clears it. One undo step.</summary>
    public void SetTitle(string? title)
    {
        string? value = BlankToNull(title);
        if (Diagram.Title == value)
        {
            return;
        }
        Change();
        Diagram.Title = value;
    }

    /// <summary>Sets <see cref="Diagram.Description"/>; blank clears it. One undo step.</summary>
    public void SetDescription(string? description)
    {
        string? value = BlankToNull(description);
        if (Diagram.Description == value)
        {
            return;
        }
        Change();
        Diagram.Description = value;
    }

    /// <summary>Shows or hides the title and description on the page (<see cref="Diagram.HideHeader"/>). One undo step.</summary>
    public void SetShowHeader(bool show)
    {
        if (Diagram.HideHeader == !show)
        {
            return;
        }
        Change();
        Diagram.HideHeader = !show;
    }

    /// <summary>
    /// The title's look (or the description's), for the style controls to change. A diagram without its own look gets
    /// the starting look here; that isn't a change (it looks the same), so it is no undo step and doesn't mark the
    /// diagram unsaved. Diagrams that never have their header styled keep these out of the file.
    /// </summary>
    public TextBox HeaderLook(bool description)
    {
        if (description)
        {
            if (Diagram.DescriptionLook is null)
            {
                Diagram.DescriptionLook = PageHeaderLooks.NewDescriptionLook();
            }
            return Diagram.DescriptionLook;
        }
        if (Diagram.TitleLook is null)
        {
            Diagram.TitleLook = PageHeaderLooks.NewTitleLook();
        }
        return Diagram.TitleLook;
    }

    /// <summary>Puts the title and description back to their starting looks. One undo step.</summary>
    public void ResetHeaderLooks()
    {
        Change();
        Diagram.TitleLook = null;
        Diagram.DescriptionLook = null;
    }

    /// <summary>Null for empty or white-space text; otherwise the text with trailing spaces and line breaks removed.</summary>
    static string? BlankToNull(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        return text.TrimEnd();
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
