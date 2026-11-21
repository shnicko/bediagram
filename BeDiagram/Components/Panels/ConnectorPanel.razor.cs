using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Heads;
using BeDiagram.Models.Presets;
using BeDiagram.Models.Serialization;
using BeDiagram.Services;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel for one selected connector: line style, route reset, weight, pattern, colour, arrowheads and ends (UML / crow's foot, relationships), label text, direction, placement and position.</summary>
public partial class ConnectorPanel : DesignerPart
{
    [Parameter, EditorRequired] public Edge Edge { get; set; } = default!;

    /// <summary>Quick labels offered under the label box (the empty one clears it).</summary>
    static readonly string[] EdgeLabelChips = { "Yes", "No", "Pass", "Fail", "" };

    /// <summary>A plain arrow button is lit when the connector has no UML / crow's-foot head and its arrows match.</summary>
    string ArrowsClass(string arrows)
    {
        if (Edge.FromHead is null && Edge.ToHead is null && Edge.Arrows == arrows)
        {
            return "on";
        }
        return "";
    }

    /// <summary>The head at one end as its saved name (the select's option values).</summary>
    string HeadValue(bool atStart)
    {
        if (atStart)
        {
            return EnumNames.Of(Edge.StartHead());
        }
        return EnumNames.Of(Edge.EndHead());
    }

    static string HeadName(IArrowHead h)
    {
        return EnumNames.Of(h.Key);
    }

    string RelationshipClass(Relationship r)
    {
        if (DiagramEditor.IsRelationship(Edge, r))
        {
            return "chip on";
        }
        return "chip";
    }

    void StartPicked(ChangeEventArgs e)
    {
        ArrowHead? head = EnumNames.Parse<ArrowHead>(e.Value?.ToString());
        if (head is not null)
        {
            Editor.SetEdgeHead(Edge, true, head.Value);
        }
    }

    void EndPicked(ChangeEventArgs e)
    {
        ArrowHead? head = EnumNames.Parse<ArrowHead>(e.Value?.ToString());
        if (head is not null)
        {
            Editor.SetEdgeHead(Edge, false, head.Value);
        }
    }

    void Swap()
    {
        Editor.SwapHeads(Edge);
    }

    /// <summary>What above / below mean for the label's text direction (the Label placement tooltip).</summary>
    static string LabelPlaceHint(Edge e)
    {
        if (e.LabelOrient == LabelOrient.Along)
        {
            return "Inline sits on the line; above and below set the text beside it, as the text reads.";
        }
        return "Inline sits on the line; above and below set the text beside it. On up-down lines above = left, below = right.";
    }

    /// <summary>The label's position in whole percent, as the slider shows it.</summary>
    int LabelPercent()
    {
        return (int)Math.Round(Edge.LabelPos * 100);
    }

    /// <summary>False when the label is already at the start end of the slider (greys out ‹).</summary>
    bool CanNudgeBack()
    {
        return LabelPercent() > DiagramEditor.LabelPosMinPercent;
    }

    /// <summary>False when the label is already at the far end of the slider (greys out ›).</summary>
    bool CanNudgeOn()
    {
        return LabelPercent() < DiagramEditor.LabelPosMaxPercent;
    }

    void NudgeBack()
    {
        Editor.NudgeLabelPos(Edge, -1);
    }

    void NudgeOn()
    {
        Editor.NudgeLabelPos(Edge, 1);
    }

    // ───────────── Saved styles ─────────────

    [Inject] StylePresetStore Store { get; set; } = default!;

    /// <summary>The saved connector styles.</summary>
    List<StylePreset> SavedStyles
    {
        get
        {
            return Store.Connectors();
        }
    }

    StylePreset CaptureOne(PresetText text)
    {
        StylePreset p = new StylePreset();
        p.Connector = ConnectorPreset.From(Edge, text);
        return p;
    }

    void ApplyPreset(StylePreset p)
    {
        if (p.Connector is not null)
        {
            Editor.ApplyPreset(Edge, p.Connector);
        }
    }
}
