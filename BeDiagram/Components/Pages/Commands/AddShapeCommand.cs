using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>Number keys 1–9: add the matching shape from the palette (joined to the selection when Auto-join is on).</summary>
public class AddShapeCommand : EditorCommand
{
    readonly string type;

    /// <param name="number">The key, 1–9.</param>
    public AddShapeCommand(int number, NodeTypeInfo shape)
        : base($"Add {shape.Display}", new KeyGesture(number.ToString()))
    {
        type = shape.Key;
    }

    protected override void Execute(ICommandContext ctx) => ctx.AddShape(type);
}
