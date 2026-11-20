namespace BeDiagram.Components.Pages.Views;

/// <summary>The open head picker: which connector, which end, and where (world coordinates) it opened.</summary>
public record HeadPick(string EdgeId, bool AtStart, double X, double Y);
