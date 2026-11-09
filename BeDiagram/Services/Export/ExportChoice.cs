using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>One entry in the Export menu: a format, plus an area when the format uses one.</summary>
public record ExportChoice(IExportFormat Format, IExportArea? Area)
{
    /// <summary>The menu text: the format's name, plus the area (e.g. "PDF — page A4") when it uses one.</summary>
    public string Label(Diagram d) => Area is null ? Format.Name : $"{Format.Name} — {Area.Label(d)}";

    /// <summary>The menu tooltip: what the area covers, or nothing.</summary>
    public string Tip(Diagram d) => Area?.Tip(d) ?? "";
}
