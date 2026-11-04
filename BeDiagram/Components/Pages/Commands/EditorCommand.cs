namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// Base for the usual command: synchronous and always available. Override <see cref="CanExecute"/> or
/// <see cref="ExecuteAsync"/> when a command needs to (Save is async, Undo checks there is something to undo).
/// </summary>
public abstract class EditorCommand : IEditorCommand
{
    protected EditorCommand(string name, params KeyGesture[] keys)
    {
        Name = name;
        Keys = keys;
    }

    public string Name { get; }
    public IReadOnlyList<KeyGesture> Keys { get; }

    public virtual bool CanExecute(ICommandContext ctx) => true;

    public virtual Task ExecuteAsync(ICommandContext ctx)
    {
        Execute(ctx);
        return Task.CompletedTask;
    }

    /// <summary>The action itself, for commands that don't need to await anything.</summary>
    protected virtual void Execute(ICommandContext ctx) { }
}
