using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Kinds;

namespace BeDiagram.Models;

public class Node : IGroupable
{
    public string Id { get; set; } = NewId("n");
    public string Type { get; set; } = NodeTypes.Process;
    /// <summary>How this node behaves, from its type (Strategy pattern, see <see cref="INodeKind"/>). Not saved.</summary>
    [JsonIgnore]
    public INodeKind Kind => NodeTypes.Get(Type).Kind;
    public string Label { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 160;
    public double H { get; set; } = 64;
    public string? GroupId { get; set; }
    /// <summary>Optional Lucide icon name (see <see cref="Icons"/>). Circles show it centred; other shapes show it beside the label.</summary>
    public string? Icon { get; set; }
    /// <summary>Optional colour key from <see cref="Palette"/>; null = the type's default colours.</summary>
    public string? Color { get; set; }
    /// <summary>Drawn as a circle with an icon in the middle and the label underneath (presentation style), keeping the
    /// type and its meaning. Only for the classic shapes (see <see cref="INodeKind.CanBeRound"/>). False is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Round { get; set; }
    /// <summary>Solid fill with white text/icon (presentation style).</summary>
    public bool Filled { get; set; }
    /// <summary>Outcome nodes only: "pass", "fail", "rework", "conditional" or "hold" (see <see cref="Outcomes"/>).
    /// This carries the meaning; the shape and glyph are just how it is drawn.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Outcome { get; set; }
    /// <summary>Place in the flow: 1, 2, 3, … (see <see cref="StepOrder"/>). Shapes that run in parallel share a number.
    /// Always kept, whether or not the numbers are shown; null only on outcomes, which are results rather than steps.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Step { get; set; }
    /// <summary>"major" (key step: solid fill and a ring), "minor" (supporting step: lighter) or null for normal.
    /// A look only — see <see cref="Emphases"/>. "normal" is stored as null (left out of the file).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Emphasis
    {
        get => emphasis;
        set => emphasis = value is null || value == Emphases.Normal ? null : value;
    }
    string? emphasis;
    public Dictionary<string, string> Properties { get; set; } = new();
    /// <summary>List and IPO nodes: the rows, in order (IPO rows carry their <see cref="ListItem.Section"/>). Null for other shapes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ListItem>? Items { get; set; }
    /// <summary>IPO nodes: renamed section titles, in input / process / output order. Null (or a blank entry) = the default title.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? SectionTitles { get; set; }
    /// <summary>IPO nodes: sections stacked top to bottom instead of side by side. False (the default) is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Stacked { get; set; }
    /// <summary>Optional longer note drawn in smaller text under the label. For shapes with the label inside,
    /// it sits under the shape. A blank description is stored as null (left out of the file).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description
    {
        get => description;
        set => description = string.IsNullOrWhiteSpace(value) ? null : value;
    }
    string? description;
    /// <summary>Wrap width (px) for text drawn outside the shape: a label under a circle or outcome, and the description.
    /// Null = automatic (see <see cref="Geometry.TextWidth"/>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TextWidth { get; set; }
    /// <summary>Text drawn outside the shape (label below / description) has no white backing, so a tinted group or
    /// anything behind shows through. False (the default) is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ClearTextBack { get; set; }
    /// <summary>Name of another saved diagram (without extension) holding this step's more detailed flow. Shows a link badge.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }

    /// <summary>The rows (<see cref="Items"/>), or none for a panel loaded without any or another shape. Not saved.</summary>
    [JsonIgnore]
    public IReadOnlyList<ListItem> Rows => Items ?? (IReadOnlyList<ListItem>)Array.Empty<ListItem>();

    /// <summary>IPO nodes: the rows in one section (<see cref="IpoSections"/>), in order.</summary>
    public List<ListItem> RowsIn(string section) => Rows.Where(i => IpoSections.Of(i.Section) == section).ToList();

    /// <summary>IPO nodes: a section's title, the renamed one or the default.</summary>
    public string SectionTitle(string section)
    {
        int i = IpoSections.IndexOf(section);
        string? t = SectionTitles is { } titles && i < titles.Count ? titles[i] : null;
        return string.IsNullOrWhiteSpace(t) ? IpoSections.DefaultTitle(section) : t;
    }

    /// <summary>A copy with a new id and its own <see cref="Properties"/> and rows (rows keep their ids); every other field (look, link, step, group) is kept.</summary>
    public Node Copy()
    {
        Node c = (Node)MemberwiseClone();
        c.Id = NewId("n");
        c.Properties = new Dictionary<string, string>(Properties);
        c.Items = Items?.Select(i => i.Copy()).ToList();
        c.SectionTitles = SectionTitles is null ? null : new List<string>(SectionTitles);
        return c;
    }

    public static string NewId(string prefix) => $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";
}
