namespace BeDiagram.Components.Pages.Views;

public record EdgeHandleView(string EdgeId, List<SideTarget> Sides, bool HasBend, double BendX, double BendY, bool BendVertical,
                             double StartX, double StartY, double EndX, double EndY);
