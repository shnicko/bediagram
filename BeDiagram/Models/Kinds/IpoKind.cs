using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Kinds;

/// <summary>An input–process–output box: a title band over three sections of rows (<see cref="IpoSections"/>).</summary>
public class IpoKind : PanelKind
{
    public override string? PaletteInnerPath =>
        "M0,8 H48 M16,8 V28 M32,8 V28 M3,13 H13 V18 H3 Z M19,13 H29 V18 H19 Z M35,13 H45 V18 H35 Z";

    /// <summary>The box is named for its process, so it starts as "Process" rather than the type's long name.</summary>
    public override string NewLabel(Node n) => "Process";

    public override string RetypeLabel(Node n) => "Process";

    /// <summary>One row per section to start with.</summary>
    public override List<ListItem> NewItems() => new List<ListItem>
    {
        new() { Label = "Inputs", Section = IpoSections.Input },
        new() { Label = "Work done", Section = IpoSections.Process },
        new() { Label = "Results", Section = IpoSections.Output },
    };
}
