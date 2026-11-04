namespace BeDiagram.Components.Pages.Gestures;

/// <summary>Right/middle-button drag: moves the view. Works in screen px, not world coordinates.</summary>
public class PanGesture : IGesture
{
    readonly IEditorContext ctx;
    double lastX, lastY;

    public PanGesture(IEditorContext ctx, double clientX, double clientY)
    {
        this.ctx = ctx;
        (lastX, lastY) = (clientX, clientY);
    }

    public bool Move(GesturePoint p)
    {
        ctx.PanBy(p.ClientX - lastX, p.ClientY - lastY);
        (lastX, lastY) = (p.ClientX, p.ClientY);
        return true;
    }

    public void Up() { }

    public void Cancel() { }
}
