namespace BeDiagram.Services.Export;

/// <summary>Puts bytes of one media type (e.g. "image/png") on the user's clipboard.</summary>
public interface IClipboard
{
    Task CopyAsync(byte[] content, string mediaType);
}
