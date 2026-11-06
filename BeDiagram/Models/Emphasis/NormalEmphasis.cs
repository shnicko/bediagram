namespace BeDiagram.Models.Emphasis;

/// <summary>The default look: the shape's own colours, firm outline and soft shadow.</summary>
public class NormalEmphasis : NodeEmphasis
{
    public override string Key => Emphases.Normal;
    public override string Name => "Normal";
    public override string Hint => "The standard look";
}
