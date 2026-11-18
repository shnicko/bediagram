using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>Saves the export as a file next to the saved diagram, e.g. "Name.png" or "Name.view.pdf".</summary>
public class FileDestination : IExportDestination
{
    readonly DiagramStore store;

    public FileDestination(DiagramStore store)
    {
        this.store = store;
    }

    public string Heading
    {
        get
        {
            return "Export";
        }
    }

    public string HeadingNote
    {
        get
        {
            return "next to the saved diagram";
        }
    }

    public string Verb
    {
        get
        {
            return "export";
        }
    }

    /// <summary>Every format can be saved as a file.</summary>
    public bool Accepts(IExportFormat format)
    {
        return true;
    }

    public async Task<string> DeliverAsync(Diagram d, ExportChoice choice, ExportFile file)
    {
        // The area's suffix keeps "current view" files from overwriting the page export.
        string suffix = "";
        if (choice.Area is not null)
        {
            suffix = choice.Area.FileSuffix;
        }
        string path = await store.SaveBytesAsync(d.Name, suffix + choice.Format.Extension, file.Content);
        return $"Exported {Path.GetFileName(path)}";
    }
}
