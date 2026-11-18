using System.Buffers.Binary;
using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>
/// Puts the export on the clipboard as a picture, ready to paste into Word, PowerPoint, Teams, an email…
/// Takes PNG only: it is the one image type every browser can put on the clipboard.
/// The PNG settings (resolution, transparent background) apply, so the whole page or the current view
/// can be copied at 300 dpi.
/// Scoped, like <see cref="BrowserClipboard"/>: the clipboard belongs to this user's browser.
/// </summary>
public class ClipboardDestination : IExportDestination
{
    const string Png = "image/png";

    readonly IClipboard clipboard;

    public ClipboardDestination(IClipboard clipboard)
    {
        this.clipboard = clipboard;
    }

    public string Heading
    {
        get
        {
            return "Copy to clipboard";
        }
    }

    public string HeadingNote
    {
        get
        {
            return "a picture to paste anywhere";
        }
    }

    public string Verb
    {
        get
        {
            return "copy";
        }
    }

    public bool Accepts(IExportFormat format)
    {
        return format.MediaType == Png;
    }

    public async Task<string> DeliverAsync(Diagram d, ExportChoice choice, ExportFile file)
    {
        await clipboard.CopyAsync(file.Content, choice.Format.MediaType);
        (int width, int height) = PngSize(file.Content);
        return $"Copied {choice.Label(d)} to the clipboard ({width} × {height} px)";
    }

    /// <summary>Width and height from the PNG's IHDR chunk (bytes 16–23), so the toast shows the resolution took effect.</summary>
    static (int Width, int Height) PngSize(byte[] png)
    {
        if (png.Length < 24)
        {
            return (0, 0);
        }
        int width = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16));
        int height = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20));
        return (width, height);
    }
}
