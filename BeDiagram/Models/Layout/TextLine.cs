namespace BeDiagram.Models.Layout;

/// <summary>One positioned line of text-box text (baseline coordinates).</summary>
public record TextLine(string Text, double X, double Y);
