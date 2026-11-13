using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Components.Pages;
using BeDiagram.Models;

namespace BeDiagram.Components.Canvas;

/// <summary>
/// Base class of the canvas layers: one layer per kind of item, drawn in this order by the page so later layers sit on
/// top (page sheet, page title, groups, text boxes, connectors, shapes, connectors ending on a row, connector labels). A layer works
/// out its own view records and hands pointer presses to the page through <see cref="Input"/>.
/// </summary>
public abstract class CanvasLayer : DesignerPart
{
    /// <summary>Where pointer presses on items go (the page starts the gesture).</summary>
    protected ICanvasInput Input => Host.Canvas;

    /// <summary>The four ● connection ports of a box: top, right, bottom and left middles.</summary>
    protected static IEnumerable<(double, double)> Ports(IBox n)
    {
        yield return (n.X + n.W / 2, n.Y);
        yield return (n.X + n.W, n.Y + n.H / 2);
        yield return (n.X + n.W / 2, n.Y + n.H);
        yield return (n.X, n.Y + n.H / 2);
    }

    /// <summary>Inline editors: Enter finishes (Shift+Enter is a new line where the editor allows two).</summary>
    protected void EditKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey) Editor.EndEdit();
    }
}
