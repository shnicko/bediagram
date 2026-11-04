namespace BeDiagram.Models.Layout;

/// <param name="SegAngle">Direction (degrees) of the connector segment the label sits on.</param>
public record EdgeGeom(string D, double LabelX, double LabelY, double LabelAngle, double SegAngle = 0);
