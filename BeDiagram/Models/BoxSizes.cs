namespace BeDiagram.Models;

/// <summary>How small a box may be made by hand (resize grips, Match size, the group's size fields).</summary>
public static class BoxSizes
{
    /// <summary>Smallest size of a group.</summary>
    public const double GroupMinW = 160, GroupMinH = 100;

    /// <summary>
    /// The smallest width and height <paramref name="b"/> may have: a group 160 × 100, a text box 40 × 24, a shape what
    /// its kind allows (a list / IPO node can't get shorter than its rows), anything else 80 × 40.
    /// </summary>
    public static (double W, double H) MinSize(IBox b)
    {
        if (b is Group)
        {
            return (GroupMinW, GroupMinH);
        }
        if (b is TextBox)
        {
            return (40, 24);
        }
        if (b is Node n)
        {
            return n.Kind.MinSize(n);
        }
        return (80, 40);
    }
}
