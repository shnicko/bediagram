using Microsoft.JSInterop;

namespace BeDiagram.Services.Export;

/// <summary>Draws an SVG document to a PNG of the given pixel size.</summary>
public interface IPngRasterizer
{
    Task<byte[]> RasterizeAsync(string svg, int width, int height);
}

/// <summary>
/// Rasterises in the user's browser (wwwroot/js/export.js): the SVG is drawn onto a canvas and the PNG bytes
/// are streamed back. No extra packages, and text uses the same fonts as the editor.
/// Scoped: one per browser connection, because it talks to that browser.
/// </summary>
public class BrowserPngRasterizer : IPngRasterizer, IAsyncDisposable
{
    const long MaxBytes = 200L * 1024 * 1024;

    readonly IJSRuntime js;
    IJSObjectReference? module;

    public BrowserPngRasterizer(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<byte[]> RasterizeAsync(string svg, int width, int height)
    {
        module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/export.js");
        IJSStreamReference png = await module.InvokeAsync<IJSStreamReference>("svgToPng", svg, width, height);
        await using Stream stream = await png.OpenReadStreamAsync(MaxBytes);
        using MemoryStream copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        return copy.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null) return;
        try { await module.DisposeAsync(); }
        catch (JSDisconnectedException) { }
    }
}
