namespace BeDiagram.Models.Catalogue;

/// <summary>The three sections of an input–process–output box, in drawing order.</summary>
public static class IpoSections
{
    public static readonly IReadOnlyList<RowSection> All = new[] { RowSection.Input, RowSection.Process, RowSection.Output };

    /// <summary>The title a section shows until it's renamed (<see cref="Node.SectionTitles"/>).</summary>
    public static string DefaultTitle(RowSection section) => section switch
    {
        RowSection.Process => "Process",
        RowSection.Output => "Output",
        _ => "Input",
    };

    /// <summary>A row's section, or <see cref="RowSection.Input"/> when it has none (a row copied from a list).</summary>
    public static RowSection Of(RowSection? section) => section ?? RowSection.Input;

    public static int IndexOf(RowSection? section) => (int)Of(section);
}
