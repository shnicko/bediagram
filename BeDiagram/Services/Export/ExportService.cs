using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>
/// The context of the strategy pattern: holds every registered <see cref="IExportFormat"/> and <see cref="IExportArea"/>,
/// lists the combinations for the menu, and runs a chosen one (render with the format, then save next to the diagram).
/// The Designer only talks to this class, so adding a format or area is a new class + one line in Program.cs.
/// </summary>
public class ExportService
{
    readonly DiagramStore store;

    public ExportService(IEnumerable<IExportFormat> formats, IEnumerable<IExportArea> areas, DiagramStore store)
    {
        // Menu order = registration order in Program.cs.
        Formats = formats.ToList();
        Areas = areas.ToList();
        this.store = store;
    }

    public IReadOnlyList<IExportFormat> Formats { get; }
    public IReadOnlyList<IExportArea> Areas { get; }

    /// <summary>Every menu entry: each area for formats that use one, a single entry for those that don't.</summary>
    public List<ExportChoice> Choices()
    {
        List<ExportChoice> result = new List<ExportChoice>();
        foreach (IExportFormat f in Formats)
        {
            if (!f.UsesArea)
            {
                result.Add(new ExportChoice(f, null));
                continue;
            }
            foreach (IExportArea a in Areas)
            {
                result.Add(new ExportChoice(f, a));
            }
        }
        return result;
    }

    /// <summary>Renders the choice and saves it next to the diagram. Returns the saved path and any note.</summary>
    public async Task<(string Path, string? Note)> ExportAsync(ExportChoice c, Diagram d, ScreenView screen, ExportOptions options)
    {
        ExportView? view = null;
        string suffix = "";
        if (c.Area is not null)
        {
            view = c.Area.ViewFor(d, screen);
            suffix = c.Area.FileSuffix;
        }
        ExportFile file = await c.Format.RenderAsync(d, view, options);
        string path = await store.SaveBytesAsync(d.Name, suffix + c.Format.Extension, file.Content);
        return (path, file.Note);
    }
}
