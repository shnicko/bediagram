using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Layout;

/// <summary>
/// A routed connector. <see cref="BendIndex"/> is the segment (Points[i] → Points[i+1]) the user can drag
/// sideways, or -1 when there isn't one. <see cref="FromSide"/>/<see cref="ToSide"/> are the sides actually
/// used (null for an automatic straight end, which aims at the other shape rather than a side).
/// </summary>
public record EdgeRoute(List<(double X, double Y)> Points, int BendIndex, Side? FromSide, Side? ToSide)
{
    public bool BendIsVertical => BendIndex >= 0 && Math.Abs(Points[BendIndex].X - Points[BendIndex + 1].X) < 0.5;
    public (double X, double Y) BendHandle => BendIndex < 0 ? (0, 0)
        : ((Points[BendIndex].X + Points[BendIndex + 1].X) / 2, (Points[BendIndex].Y + Points[BendIndex + 1].Y) / 2);
}
