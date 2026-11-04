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
    public IEnumerable<ExportChoice> Choices()
    {
        foreach (IExportFormat f in Formats)
        {
            if (!f.UsesArea)
            {
                yield return new ExportChoice(f, null);
                continue;
            }
            foreach (IExportArea a in Areas)
                yield return new ExportChoice(f, a);
        }
    }

    public static string Label(ExportChoice c, Diagram d) =>
        c.Area is null ? c.Format.Name : $"{c.Format.Name} — {c.Area.Label(d)}";

    public static string Tip(ExportChoice c, Diagram d) => c.Area?.Tip(d) ?? "";

    /// <summary>Renders the choice and saves it next to the diagram. Returns the saved path and any note.</summary>
    public async Task<(string Path, string? Note)> ExportAsync(ExportChoice c, Diagram d, ScreenView screen, ExportOptions options)
    {
        ExportView? view = c.Area?.ViewFor(d, screen);
        ExportFile file = await c.Format.RenderAsync(d, view, options);
        string path = await store.SaveBytesAsync(d.Name, (c.Area?.FileSuffix ?? "") + c.Format.Extension, file.Content);
        return (path, file.Note);
    }
}
