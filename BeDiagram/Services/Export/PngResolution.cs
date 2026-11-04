namespace BeDiagram.Services.Export;

/// <summary>One choice in the PNG resolution list: menu text and the scale applied to the SVG's size in px.</summary>
public record PngResolution(string Label, double Scale);
