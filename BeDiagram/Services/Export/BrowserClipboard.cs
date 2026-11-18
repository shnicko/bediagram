using Microsoft.JSInterop;

namespace BeDiagram.Services.Export;

/// <summary>
/// Writes to the clipboard of the user's browser (wwwroot/js/export.js, <c>copyToClipboard</c>): the bytes are
/// streamed to the browser, which puts them on the clipboard with the async Clipboard API.
/// Browsers only allow this on https or localhost.
/// Scoped: one per browser connection, because it talks to that browser.
/// </summary>
public class BrowserClipboard : IClipboard, IAsyncDisposable
{
    readonly IJSRuntime js;
    IJSObjectReference? module;

    public BrowserClipboard(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task CopyAsync(byte[] content, string mediaType)
    {
        if (module is null)
        {
            module = await js.InvokeAsync<IJSObjectReference>("import", "./js/export.js");
        }
        // A stream rather than a byte[] argument, so a large 300 dpi image isn't one huge message.
        using MemoryStream stream = new MemoryStream(content);
        using DotNetStreamReference streamRef = new DotNetStreamReference(stream);
        await module.InvokeVoidAsync("copyToClipboard", streamRef, mediaType);
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null)
        {
            return;
        }
        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
