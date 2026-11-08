using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages.Views;

public record PaletteShape(NodeTypeInfo Type, int Number)
{
    /// <summary>Only the first nine shapes have a number key.</summary>
    public bool HasKey => Number <= 9;
}
