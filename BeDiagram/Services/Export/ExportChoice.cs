using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>
/// One entry in the Export menu: a format, plus an area when the format uses one, and where the result goes
/// (a file next to the diagram, or the clipboard).
/// </summary>
public record ExportChoice(IExportFormat Format, IExportArea? Area, IExportDestination Destination)
{
    /// <summary>The menu text: the format's name, plus the area (e.g. "PDF — page A4") when it uses one.</summary>
    public string Label(Diagram d)
    {
        if (Area is null)
        {
            return Format.Name;
        }
        return $"{Format.Name} — {Area.Label(d)}";
    }

    /// <summary>The menu tooltip: what the area covers, or nothing.</summary>
    public string Tip(Diagram d)
    {
        if (Area is null)
        {
            return "";
        }
        return Area.Tip(d);
    }
}
