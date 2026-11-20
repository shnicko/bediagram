using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Kinds;
using BeDiagram.Models.Location;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models;

public class Node : IGroupable
{
    public string Id { get; set; } = NewId("n");
    public NodeType Type { get; set; } = NodeType.Process;
    /// <summary>How this node behaves, from its type (Strategy pattern, see <see cref="INodeKind"/>). Not saved.</summary>
    [JsonIgnore]
    public INodeKind Kind
    {
        get
        {
            return NodeTypes.Get(Type).Kind;
        }
    }
    /// <summary>Placed by its centre; the kind says which placement (a list or IPO box keeps the height of its rows).
    /// Not saved.</summary>
    [JsonIgnore]
    public IPlacement Placement
    {
        get
        {
            return Kind.Placement;
        }
    }
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
    /// <summary>Outcome nodes only: pass, fail, rework, conditional or hold (see <see cref="Outcomes"/>).
    /// This carries the meaning; the shape and glyph are just how it is drawn.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<OutcomeType>))]
    public OutcomeType? Outcome { get; set; }
    /// <summary>Place in the flow: 1, 2, 3, … (see <see cref="StepOrder"/>). Shapes that run in parallel share a number.
    /// Always kept, whether or not the numbers are shown; null only on outcomes, which are results rather than steps.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Step { get; set; }
    /// <summary>Major (key step: solid fill and a ring), minor (supporting step: lighter) or normal.
    /// A look only — see <see cref="Emphases"/>. Normal is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public EmphasisLevel Emphasis { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    /// <summary>List and IPO nodes: the rows, in order (IPO rows carry their <see cref="ListItem.Section"/>). Null for other shapes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ListItem>? Items { get; set; }
    /// <summary>IPO nodes: renamed section titles, in input / process / output order. Null (or a blank entry) = the default title.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? SectionTitles { get; set; }
    /// <summary>IPO nodes: sections stacked top to bottom instead of side by side. False (the default) is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Stacked { get; set; }
    /// <summary>List and IPO nodes: show 1, 2, 3, … on the left of each row (an IPO box counts each section from 1).
    /// For display only: the numbers come from the row order and aren't saved. False (the default) is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool NumberRows { get; set; }
    /// <summary>Classes: abstract (shown as {abstract} over the name). False (the default) is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Abstract { get; set; }
    /// <summary>Classes and entities: show the name only, hiding the attributes and operations (a conceptual model).
    /// The rows are kept. False (the default) is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool NameOnly { get; set; }
    /// <summary>Optional longer note drawn in smaller text under the label. For shapes with the label inside,
    /// it sits under the shape. A blank description is stored as null (left out of the file).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description
    {
        get
        {
            return description;
        }
        set
        {
            description = string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
    string? description;
    /// <summary>Wrap width (px) for text drawn outside the shape: a label under a circle or outcome, and the description.
    /// Null = automatic (see <see cref="TextLayout.TextWidth"/>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TextWidth { get; set; }
    /// <summary>Text drawn outside the shape (label below / description) has no white backing, so a tinted group or
    /// anything behind shows through. False (the default) is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ClearTextBack { get; set; }
    /// <summary>Which side of the shape the text outside it sits on (label below a circle or outcome, and the
    /// description): top, right or left. Null (the default, left out of the file) = underneath.
    /// Ignored while the text is dragged free (<see cref="FreeLabel"/>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<Side>))]
    public Side? LabelSide { get; set; }
    /// <summary>Text outside the shape that the user has dragged free: where the centre of the text block sits, from
    /// the shape's centre, so it moves with the shape. A leader line joins it to the shape. Null (the default, left
    /// out of the file) = attached on <see cref="LabelSide"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Offset? FreeLabel { get; set; }
    /// <summary>Name of another saved diagram (without extension) holding this step's more detailed flow. Shows a link badge.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }

    /// <summary>The rows (<see cref="Items"/>), or none for a panel loaded without any or another shape. Not saved.</summary>
    [JsonIgnore]
    public IReadOnlyList<ListItem> Rows
    {
        get
        {
            if (Items is null)
            {
                return Array.Empty<ListItem>();
            }
            return Items;
        }
    }

    /// <summary>IPO nodes: the rows in one section (<see cref="IpoSections"/>), in order.</summary>
    public List<ListItem> RowsIn(RowSection section)
    {
        List<ListItem> result = new List<ListItem>();
        foreach (ListItem item in Rows)
        {
            if (IpoSections.Of(item.Section) == section)
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>IPO nodes: a section's title, the renamed one or the default.</summary>
    public string SectionTitle(RowSection section)
    {
        int i = IpoSections.IndexOf(section);
        string? t = null;
        if (SectionTitles is not null && i < SectionTitles.Count)
        {
            t = SectionTitles[i];
        }
        if (string.IsNullOrWhiteSpace(t))
        {
            return IpoSections.DefaultTitle(section);
        }
        return t;
    }

    /// <summary>A copy with a new id and its own <see cref="Properties"/> and rows (rows keep their ids); every other field (look, link, step, group) is kept.</summary>
    public Node Copy()
    {
        Node c = (Node)MemberwiseClone();
        c.Id = NewId("n");
        c.Properties = new Dictionary<string, string>(Properties);
        c.Items = null;
        if (Items is not null)
        {
            c.Items = new List<ListItem>();
            foreach (ListItem item in Items)
            {
                c.Items.Add(item.Copy());
            }
        }
        c.SectionTitles = null;
        if (SectionTitles is not null)
        {
            c.SectionTitles = new List<string>(SectionTitles);
        }
        return c;
    }

    public static string NewId(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";
    }
}
