namespace BeDiagram.Models.Location;

/// <summary>
/// A group: placed by its top-left corner like a text box, not smaller than <see cref="MinW"/> × <see cref="MinH"/>,
/// and moving it carries everything inside it at any depth (Composite, see <see cref="GroupTree"/>) unless only the box
/// should move (shapes locked in place).
/// </summary>
public class GroupPlacement : CornerPlacement
{
    public const double MinW = 160;
    public const double MinH = 100;

    public override (double W, double H) MinSize(IBox b)
    {
        return (MinW, MinH);
    }

    public override void MoveTo(IBox b, double x, double y, GroupTree tree, bool carry)
    {
        double dx = x - b.X;
        double dy = y - b.Y;
        b.X = x;
        b.Y = y;
        if (!carry)
        {
            return;
        }
        // Only groups get this placement, so the box is a group and its members follow it.
        Group g = (Group)b;
        foreach (IGroupable m in tree.Descendants(g))
        {
            m.X += dx;
            m.Y += dy;
        }
    }
}
