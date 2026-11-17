using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Location;

/// <summary>
/// The steps every item's placement shares (Template Method): <see cref="Plan"/>, <see cref="Apply"/>,
/// <see cref="Resize"/> and <see cref="Fields"/> always run in the same order and call the parts that differ —
/// <see cref="SetOut"/> / <see cref="TopLeftFor"/> (centre or corner), <see cref="MinSize"/>,
/// <see cref="FixedHeight"/> and <see cref="MoveTo"/> (a group carries its contents).
/// </summary>
public abstract class Placement : IPlacement
{
    public abstract string SetOutName { get; }

    public abstract (double X, double Y) SetOut(IBox b);

    public abstract (double X, double Y) TopLeftFor(double setX, double setY, double w, double h);

    public abstract (double W, double H) MinSize(IBox b);

    public virtual bool FixedHeight(IBox b)
    {
        return false;
    }

    public Rect Fields(IBox b)
    {
        (double x, double y) = SetOut(b);
        return new Rect(x, y, b.W, b.H);
    }

    public Rect? Plan(IBox b, Rect fields)
    {
        // Size first (not below the smallest, a fixed height kept), then where that size puts the corner.
        (double minW, double minH) = MinSize(b);
        double w = Math.Max(minW, fields.W);
        double h = Math.Max(minH, fields.H);
        if (FixedHeight(b))
        {
            h = b.H;
        }
        (double x, double y) = TopLeftFor(fields.X, fields.Y, w, h);
        if (x == b.X && y == b.Y && w == b.W && h == b.H)
        {
            return null;
        }
        return new Rect(x, y, w, h);
    }

    public void Apply(IBox b, Rect box, GroupTree tree, bool carry)
    {
        MoveTo(b, box.X, box.Y, tree, carry);
        b.W = box.W;
        b.H = box.H;
    }

    public void Resize(IBox b, double w, double h)
    {
        (double setX, double setY) = SetOut(b);
        (double x, double y) = TopLeftFor(setX, setY, w, h);
        b.X = x;
        b.Y = y;
        b.W = w;
        b.H = h;
    }

    public virtual void MoveTo(IBox b, double x, double y, GroupTree tree, bool carry)
    {
        b.X = x;
        b.Y = y;
    }
}
