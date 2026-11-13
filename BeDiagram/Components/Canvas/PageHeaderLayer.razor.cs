using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;
using BeDiagram.Rendering;

namespace BeDiagram.Components.Canvas;

/// <summary>
/// The page title and description at the top of the sheet (<see cref="PageHeader"/>), drawn like text boxes but not
/// selectable: clicking them reaches the canvas, which shows the diagram panel where they are edited. Also a dashed
/// line where the drawing area starts below them. Nothing when the header isn't shown.
/// </summary>
public partial class PageHeaderLayer : CanvasLayer
{
    /// <summary>The title and description, laid out for drawing.</summary>
    List<TextBoxView> Views
    {
        get
        {
            List<TextBoxView> result = new List<TextBoxView>();
            foreach (TextBox t in PageHeader.Boxes(D))
            {
                TextStyle ts = TextStyle.For(t);
                result.Add(new TextBoxView(t, ts, false, TextLayout.Layout(t), SvgShapes.TextBoxPath(t, ts.Radius)));
            }
            return result;
        }
    }

    /// <summary>The drawing area below the header.</summary>
    Rect Area
    {
        get
        {
            return PageHeader.DrawingArea(D);
        }
    }
}
