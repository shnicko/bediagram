namespace BeDiagram.Models.Presets;

/// <summary>
/// Which text a style saves along with the look. For a shape: its label, its description, both or neither. Groups,
/// text boxes and connectors have one text (title, text, label), saved with <see cref="Label"/> or <see cref="Both"/>.
/// Only used while saving: a saved style keeps the text it holds and leaves the rest out.
/// </summary>
public enum PresetText
{
    None,
    Label,
    Description,
    Both,
}
