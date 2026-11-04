namespace BeDiagram.Services.Export;

/// <summary>An exported file: its content plus an optional note for the user (e.g. "scaled down to fit").</summary>
public record ExportFile(byte[] Content, string? Note = null);
