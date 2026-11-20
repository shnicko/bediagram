namespace BeDiagram.Models.Presets;

/// <summary>
/// What Paste special adds to the look (the look itself always comes). Each part is only offered for the kinds of item
/// that have it (<see cref="SizeFor"/> … <see cref="LinkFor"/>): size for shapes, groups and text boxes; type, properties
/// for shapes; text for all; link for shapes and groups. The last choice is remembered by the <c>StyleClipboard</c>.
/// </summary>
public class PasteParts
{
    /// <summary>Width and height (the item keeps its centre or corner, as when typing a size).</summary>
    public bool Size { get; set; }

    /// <summary>Shape type, outcome and drawn-as-circle.</summary>
    public bool Type { get; set; }

    /// <summary>Label and description of a shape, title of a group, text of a text box, label of a connector.</summary>
    public bool Text { get; set; }

    /// <summary>A shape's properties (rule id, IFC entity, …): replaces the target's properties.</summary>
    public bool Properties { get; set; }

    /// <summary>The linked diagram of a shape or group.</summary>
    public bool Link { get; set; }

    /// <summary>The look only: what an ordinary paste and the format painter copy.</summary>
    public static PasteParts LookOnly()
    {
        return new PasteParts();
    }

    public PasteParts Clone()
    {
        PasteParts p = new PasteParts();
        p.Size = Size;
        p.Type = Type;
        p.Text = Text;
        p.Properties = Properties;
        p.Link = Link;
        return p;
    }

    public static bool SizeFor(CopiedStyle s)
    {
        return s.Connector is null;
    }

    public static bool TypeFor(CopiedStyle s)
    {
        return s.Shape is not null;
    }

    public static bool PropertiesFor(CopiedStyle s)
    {
        return s.Shape is not null;
    }

    public static bool LinkFor(CopiedStyle s)
    {
        return s.Shape is not null || s.Group is not null;
    }
}
