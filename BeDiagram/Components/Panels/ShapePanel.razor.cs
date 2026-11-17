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

    Task FilterTyped(ChangeEventArgs e)
    {
        return IconFilterChanged.InvokeAsync(e.Value?.ToString() ?? "");
    }

    static string NodePanelTitle(Node n)
    {
        return n.Kind.PanelTitle(n);
    }

    /// <summary>What the text width applies to for this shape, for the panel hint.</summary>
    static string TextWidthHint(Node n)
    {
        if (n.Kind.LabelBelow(n))
        {
            return "label and description under the shape";
        }
        return "description under the shape";
    }

    static string TextWidthPlaceholder(Node n)
    {
        return $"Auto ({F(TextLayout.DefaultTextWidth(n))})";
    }

    /// <summary>The side buttons under "Text position", in the order shown.</summary>
    static readonly List<TextPlace> TextPlaces = new List<TextPlace>
    {
        new TextPlace(Side.Bottom, "Under", "Text under the shape (the usual place)"),
        new TextPlace(Side.Top, "Above", "Text above the shape"),
        new TextPlace(Side.Right, "Right", "Text to the right of the shape"),
        new TextPlace(Side.Left, "Left", "Text to the left of the shape"),
    };

    /// <summary>A side button is lit when the text is attached on that side (no side stored = under).</summary>
    string PlaceClass(TextPlace place)
    {
        if (Node.FreeLabel is not null)
        {
            return "";
        }
        Side current = Side.Bottom;
        if (Node.LabelSide is not null)
        {
            current = Node.LabelSide.Value;
        }
        if (current == place.Side)
        {
            return "on";
        }
        return "";
    }

    string FreeClass()
    {
        if (Node.FreeLabel is not null)
        {
            return "on";
        }
        return "";
    }

    void PlacePicked(TextPlace place)
    {
        Editor.SetLabelSide(Node, place.Side);
    }

    void FreePicked()
    {
        Editor.FreeText(Node);
    }

    /// <summary>Step number typed in the panel: a whole number makes the shape that step (others move down one).</summary>
    void StepTyped(Node n, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int step))
        {
            Editor.SetStep(n, step);
        }
    }

    /// <summary>A shape can run in parallel when there's another step to share a number with (or it already does).</summary>
    bool CanBeParallel(Node n)
    {
        return Editor.Steps.IsParallel(n) || Editor.Steps.Last > 1;
    }

    /// <summary>"Shares step 4 with Design Review" under the step box, or null when the shape has a number of its own.</summary>
    string? ParallelNote(Node n)
    {
        List<Node> others = Editor.Steps.ParallelWith(n);
        if (others.Count == 0)
        {
            return null;
        }
        List<string> names = new List<string>();
        foreach (Node o in others)
        {
            if (string.IsNullOrWhiteSpace(o.Label))
            {
                names.Add(NodeTypes.Get(o.Type).Display);
            }
            else
            {
                names.Add(o.Label.Replace("\n", " "));
            }
        }
        return $"Shares step {n.Step} with {string.Join(", ", names)}";
    }

    /// <summary>The Type dropdown: options carry the saved type names ("decision").</summary>
    void TypePicked(Node n, ChangeEventArgs e)
    {
        Editor.ChangeType(n, EnumNames.Parse<NodeType>(e.Value?.ToString()));
    }

    /// <summary>Icon picker contents, filtered by the search box (empty categories dropped).</summary>
    List<(string Category, List<string> Names)> FilteredIcons()
    {
        List<(string Category, List<string> Names)> result = new List<(string Category, List<string> Names)>();
        bool all = string.IsNullOrWhiteSpace(IconFilter);
        foreach ((string Category, string[] Names) c in Icons.Categories)
        {
            List<string> names = new List<string>();
            foreach (string nm in c.Names)
            {
                if (all || nm.Contains(IconFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(nm);
                }
            }
            if (names.Count > 0)
            {
                result.Add((c.Category, names));
            }
        }
        return result;
    }

    void SetPropValue(Node n, string key, ChangeEventArgs e)
    {
        n.Properties[key] = e.Value?.ToString() ?? string.Empty;
        Editor.MarkDirty();
    }
}
