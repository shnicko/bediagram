namespace BeDiagram.Services.Export;

/// <summary>Draws an SVG document to a PNG of the given pixel size.</summary>
public interface IPngRasterizer
{
    Task<byte[]> RasterizeAsync(string svg, int width, int height);
}
