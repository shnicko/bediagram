namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// Command pattern: one editor action (save, duplicate, nudge, zoom…) as an object with its name, its keyboard
/// shortcuts and whether it can run right now. The keyboard handler and the toolbar / panel buttons all go through
/// <see cref="CommandRegistry"/>, so an action, its shortcut and its tooltip are defined once.
/// Commands do their work through <see cref="ICommandContext"/> (the receiver: the Designer).
/// </summary>
public interface IEditorCommand
{
    /// <summary>Shown in tooltips, e.g. "Undo" → "Undo (Ctrl+Z)".</summary>
    string Name { get; }

    /// <summary>Shortcuts that run the command. The first one is shown in tooltips.</summary>
    IReadOnlyList<KeyGesture> Keys { get; }

    /// <summary>False when the command would do nothing (e.g. Undo with nothing to undo); buttons show as disabled.</summary>
    bool CanExecute(ICommandContext ctx);

    Task ExecuteAsync(ICommandContext ctx);
}
