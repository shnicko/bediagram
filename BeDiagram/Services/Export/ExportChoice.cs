namespace BeDiagram.Services.Export;

/// <summary>One entry in the Export menu: a format, plus an area when the format uses one.</summary>
public record ExportChoice(IExportFormat Format, IExportArea? Area);
