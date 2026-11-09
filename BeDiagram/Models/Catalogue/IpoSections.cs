namespace BeDiagram.Models.Catalogue;

/// <summary>The three sections of an input–process–output box, in drawing order.</summary>
public static class IpoSections
{
    public static readonly IReadOnlyList<IpoSection> All = new[] { IpoSection.Input, IpoSection.Process, IpoSection.Output };

    /// <summary>The title a section shows until it's renamed (<see cref="Node.SectionTitles"/>).</summary>
    public static string DefaultTitle(IpoSection section) => section switch
    {
        IpoSection.Process => "Process",
        IpoSection.Output => "Output",
        _ => "Input",
    };

    /// <summary>A row's section, or <see cref="IpoSection.Input"/> when it has none (a row copied from a list).</summary>
    public static IpoSection Of(IpoSection? section) => section ?? IpoSection.Input;

    public static int IndexOf(IpoSection? section) => (int)Of(section);
}
