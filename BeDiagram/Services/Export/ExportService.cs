using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>
/// The context of the strategy pattern: holds every registered <see cref="IExportDestination"/>,
/// <see cref="IExportFormat"/> and <see cref="IExportArea"/>, lists the combinations for the menu, and runs a chosen
/// one (area → view window, format → bytes, destination → saved file or clipboard).
/// The Designer only talks to this class, so adding a destination, format or area is a new class + one line in Program.cs.
/// </summary>
public class ExportService
{
    public ExportService(IEnumerable<IExportDestination> destinations, IEnumerable<IExportFormat> formats, IEnumerable<IExportArea> areas)
    {
        // Menu order = registration order in Program.cs.
        Destinations = destinations.ToList();
        Formats = formats.ToList();
        Areas = areas.ToList();
    }

    public IReadOnlyList<IExportDestination> Destinations { get; }
    public IReadOnlyList<IExportFormat> Formats { get; }
    public IReadOnlyList<IExportArea> Areas { get; }

    /// <summary>
    /// Every menu entry, grouped by destination: for each format the destination takes,
    /// each area for formats that use one, a single entry for those that don't.
    /// </summary>
    public List<ExportChoice> Choices()
    {
        List<ExportChoice> result = new List<ExportChoice>();
        foreach (IExportDestination dest in Destinations)
        {
            foreach (IExportFormat f in Formats)
            {
                if (!dest.Accepts(f))
                {
                    continue;
                }
                if (!f.UsesArea)
                {
                    result.Add(new ExportChoice(f, null, dest));
                    continue;
                }
                foreach (IExportArea a in Areas)
                {
                    result.Add(new ExportChoice(f, a, dest));
                }
            }
        }
        return result;
    }

    /// <summary>Renders the choice and hands it to its destination. Returns the message for the toast.</summary>
    public async Task<string> ExportAsync(ExportChoice c, Diagram d, ScreenView screen, ExportOptions options)
    {
        ExportView? view = null;
        if (c.Area is not null)
        {
            view = c.Area.ViewFor(d, screen);
        }
        ExportFile file = await c.Format.RenderAsync(d, view, options);
        string message = await c.Destination.DeliverAsync(d, c, file);
        if (file.Note is not null)
        {
            message = $"{message} ({file.Note})";
        }
        return message;
    }
}
