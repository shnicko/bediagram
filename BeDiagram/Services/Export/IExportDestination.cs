using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>
/// Strategy for where an export goes once a format has made it: saved as a file next to the diagram
/// (<see cref="FileDestination"/>) or put on the clipboard (<see cref="ClipboardDestination"/>).
/// Each destination says which formats it takes, so the Export menu lists destination × format × area.
/// To add one: write a class that implements this interface and register it in Program.cs.
/// </summary>
public interface IExportDestination
{
    /// <summary>Menu heading above this destination's entries, e.g. "Copy to clipboard".</summary>
    string Heading { get; }

    /// <summary>Grey text after the heading, e.g. "next to the saved diagram".</summary>
    string HeadingNote { get; }

    /// <summary>One word for the failure message, e.g. "export" → "PNG image export failed: …".</summary>
    string Verb { get; }

    /// <summary>True when this destination can take what the format makes (the clipboard only takes PNG).</summary>
    bool Accepts(IExportFormat format);

    /// <summary>Delivers the rendered file and returns the message for the toast, e.g. "Exported Name.png".</summary>
    Task<string> DeliverAsync(Diagram d, ExportChoice choice, ExportFile file);
}
