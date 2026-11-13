using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Text;

/// <summary>
/// The title and description at the top of a fixed page (<see cref="Diagram.Title"/>, <see cref="Diagram.Description"/>).
/// They are laid out as text boxes made from their looks (<see cref="TitleLook"/>, <see cref="DescriptionLook"/>):
/// at the top margin, as wide as the area inside the margins, each as tall as its text. They are not items of the
/// drawing (not in <see cref="Diagram.TextBoxes"/>), so selecting, moving, layout and export scaling leave them alone,
/// and the drawing goes in the area below them (<see cref="DrawingArea"/>). Shared by the canvas, the export and the editor.
/// </summary>
public static class PageHeader
{
    /// <summary>Space between the header and the drawing area below it.</summary>
    public const double Gap = 12;

    /// <summary>True when the header is drawn: a fixed page, the header switched on, and a title or description to show.</summary>
    public static bool Shown(Diagram d)
    {
        if (!d.Page.Enabled || d.HideHeader)
        {
            return false;
        }
        return !string.IsNullOrWhiteSpace(d.Title) || !string.IsNullOrWhiteSpace(d.Description);
    }

    /// <summary>
    /// The header's text boxes, placed on the page: the title (if any) at the top margin, then the description under
    /// it. Copies of the looks, so changing them never changes the diagram. Empty when the header isn't shown.
    /// </summary>
    public static List<TextBox> Boxes(Diagram d)
    {
        List<TextBox> result = new List<TextBox>();
        if (!Shown(d))
        {
            return result;
        }
        double m = d.Page.MarginPx;
        double width = Math.Max(20, d.Page.WidthPx - 2 * m);
        double y = m;
        if (!string.IsNullOrWhiteSpace(d.Title))
        {
            TextBox title = Place(TitleLook(d), d.Title.Trim(), m, y, width);
            result.Add(title);
            y = y + title.H;
        }
        if (!string.IsNullOrWhiteSpace(d.Description))
        {
            TextBox description = Place(DescriptionLook(d), d.Description.Trim(), m, y, width);
            result.Add(description);
        }
        return result;
    }

    /// <summary>The title's look: the diagram's own, or the starting look when it has none.</summary>
    public static TextBox TitleLook(Diagram d)
    {
        if (d.TitleLook is not null)
        {
            return d.TitleLook;
        }
        return PageHeaderLooks.NewTitleLook();
    }

    /// <summary>The description's look: the diagram's own, or the starting look when it has none.</summary>
    public static TextBox DescriptionLook(Diagram d)
    {
        if (d.DescriptionLook is not null)
        {
            return d.DescriptionLook;
        }
        return PageHeaderLooks.NewDescriptionLook();
    }

    /// <summary>A copy of <paramref name="look"/> holding <paramref name="text"/> at (x, y), <paramref name="width"/>
    /// wide and as tall as the wrapped text.</summary>
    static TextBox Place(TextBox look, string text, double x, double y, double width)
    {
        TextBox box = look.Copy();
        box.Text = text;
        box.IsComment = false;
        box.GroupId = null;
        box.X = x;
        box.Y = y;
        box.W = width;
        box.H = 0;
        // Height from the text: lines plus padding, worked out the same way as "Fit height to text".
        box.H = TextLayout.Layout(box).ContentHeight;
        return box;
    }

    /// <summary>How much of the page the header takes from the top margin down, gap included (0 when not shown).</summary>
    public static double Height(Diagram d)
    {
        List<TextBox> boxes = Boxes(d);
        if (boxes.Count == 0)
        {
            return 0;
        }
        TextBox last = boxes[boxes.Count - 1];
        return last.Y + last.H - d.Page.MarginPx + Gap;
    }

    /// <summary>
    /// Where the drawing goes on the page: inside the margins and below the header. Centre on page, the
    /// "too big" check, new swim lanes and scale-to-fit on export all use this. Only meaningful on a fixed page.
    /// </summary>
    public static Rect DrawingArea(Diagram d)
    {
        double m = d.Page.MarginPx;
        double header = Height(d);
        double w = Math.Max(0, d.Page.WidthPx - 2 * m);
        double h = Math.Max(0, d.Page.HeightPx - 2 * m - header);
        return new Rect(m, m + header, w, h);
    }
}
