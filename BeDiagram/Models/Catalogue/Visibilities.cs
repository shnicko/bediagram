namespace BeDiagram.Models.Catalogue;

/// <summary>The UML visibility symbols (+ − # ~) and their names, in the order the side panel lists them.</summary>
public static class Visibilities
{
    public static readonly IReadOnlyList<Visibility> All = new List<Visibility>
    {
        Visibility.Public,
        Visibility.Private,
        Visibility.Protected,
        Visibility.Package,
    };

    /// <summary>The symbol drawn before a member's name, or "" when the row has no visibility.</summary>
    public static string Symbol(Visibility? v)
    {
        if (v is null)
        {
            return "";
        }
        switch (v.Value)
        {
            case Visibility.Public:
                return "+";
            case Visibility.Private:
                return "-";
            case Visibility.Protected:
                return "#";
            default:
                return "~";
        }
    }

    /// <summary>The name shown in the side panel ("+ public").</summary>
    public static string Display(Visibility v)
    {
        switch (v)
        {
            case Visibility.Public:
                return "+ public";
            case Visibility.Private:
                return "- private";
            case Visibility.Protected:
                return "# protected";
            default:
                return "~ package";
        }
    }
}
