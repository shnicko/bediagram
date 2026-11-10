using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Serialization;
using BeDiagram.Models.Text;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel for one selected shape: label, description, step, type, size, look, outcome or icon, rows of a list / IPO box, link, group and properties.</summary>
public partial class ShapePanel : DesignerPart
{
    [Parameter, EditorRequired] public Node Node { get; set; } = default!;

    /// <summary>The icon search text. Kept by the page (<c>@bind-IconFilter</c>), so it stays while other items are selected.</summary>
    [Parameter] public string IconFilter { get; set; } = "";
    [Parameter] public EventCallback<string> IconFilterChanged { get; set; }

    Task FilterTyped(ChangeEventArgs e) => IconFilterChanged.InvokeAsync(e.Value?.ToString() ?? "");

    static string NodePanelTitle(Node n) => n.Kind.PanelTitle(n);

    /// <summary>What the text width applies to for this shape, for the panel hint.</summary>
    static string TextWidthHint(Node n) =>
        n.Kind.LabelBelow(n) ? "label and description under the shape" : "description under the shape";

    static string TextWidthPlaceholder(Node n) => $"Auto ({F(TextLayout.DefaultTextWidth(n))})";

    /// <summary>Step number typed in the panel: a whole number makes the shape that step (others move down one).</summary>
    void StepTyped(Node n, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int step)) Editor.SetStep(n, step);
    }

    /// <summary>A shape can run in parallel when there's another step to share a number with (or it already does).</summary>
    bool CanBeParallel(Node n) => Editor.Steps.IsParallel(n) || Editor.Steps.Last > 1;

    /// <summary>"Shares step 4 with Design Review" under the step box, or null when the shape has a number of its own.</summary>
    string? ParallelNote(Node n)
    {
        List<Node> others = Editor.Steps.ParallelWith(n);
        if (others.Count == 0) return null;
        return $"Shares step {n.Step} with {string.Join(", ", others.Select(o => string.IsNullOrWhiteSpace(o.Label) ? NodeTypes.Get(o.Type).Display : o.Label.Replace("\n", " ")))}";
    }

    /// <summary>The Type dropdown: options carry the saved type names ("decision").</summary>
    void TypePicked(Node n, ChangeEventArgs e) => Editor.ChangeType(n, EnumNames.Parse<NodeType>(e.Value?.ToString()));

    /// <summary>Icon picker contents, filtered by the search box (empty categories dropped).</summary>
    List<(string Category, List<string> Names)> FilteredIcons() =>
        Icons.Categories
            .Select(c => (c.Category, Names: c.Names.Where(nm => string.IsNullOrWhiteSpace(IconFilter) || nm.Contains(IconFilter.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()))
            .Where(c => c.Names.Count > 0)
            .ToList();

    void SetPropValue(Node n, string key, ChangeEventArgs e)
    {
        n.Properties[key] = e.Value?.ToString() ?? string.Empty;
        Editor.MarkDirty();
    }
}
