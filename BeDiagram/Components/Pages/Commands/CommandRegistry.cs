using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// The invoker: holds every editor command and finds the one for a key press. Registered as a singleton in
/// Program.cs (commands keep no state; the editor is passed in as <see cref="ICommandContext"/> when one runs).
/// </summary>
public class CommandRegistry
{
    readonly List<IEditorCommand> commands;
    readonly Dictionary<KeyGesture, IEditorCommand> byKey = new Dictionary<KeyGesture, IEditorCommand>();

    public CommandRegistry(IEnumerable<IEditorCommand> commands)
    {
        this.commands = new List<IEditorCommand>(commands);
        foreach (IEditorCommand c in this.commands)
        {
            foreach (KeyGesture k in c.Keys)
            {
                if (!byKey.TryAdd(k, c))
                {
                    throw new InvalidOperationException($"Shortcut {k} is used by both '{byKey[k].Name}' and '{c.Name}'");
                }
            }
        }
    }

    /// <summary>Every command BeDiagram has. To add one: write the class and add it here.</summary>
    public static CommandRegistry CreateDefault()
    {
        List<IEditorCommand> all = new List<IEditorCommand>
        {
            new SaveCommand(), new SaveAsCommand(), new UndoCommand(), new RedoCommand(),
            new SelectAllCommand(), new CancelCommand(), new EditTextCommand(),
            new CopyItemsCommand(), new CutItemsCommand(), new PasteItemsCommand(),
            new CopyStyleCommand(), new PasteStyleCommand(), new PasteSpecialCommand(),
            new FormatPainterCommand(false), new FormatPainterCommand(true),
            new DuplicateCommand(), new DeleteCommand(), new GroupCommand(), new UngroupCommand(), new JoinCommand(), new QuickBranchCommand(),
            new ToggleConnectorStyleCommand(), new ToggleAngleLockCommand(), new ArrangeLanesHorizontalCommand(), new ArrangeLanesVerticalCommand(),
            new TidyConnectorsCommand(), new PlaceTextCommand(), new AutoLayoutHorizontalCommand(), new AutoLayoutVerticalCommand(),
            new AddTextCommand(), new AddCommentCommand(), new AddGroupCommand(),
            new NewDiagramCommand(), new CloseTabCommand(), new NextTabCommand(), new PreviousTabCommand(),
            new FitCommand(), new FitSelectionCommand(), new ZoomInCommand(), new ZoomOutCommand(), new ResetZoomCommand(),
        };
        foreach (bool fine in new bool[] { false, true })
        {
            all.Add(new NudgeCommand("ArrowUp", 0, -1, fine));
            all.Add(new NudgeCommand("ArrowDown", 0, 1, fine));
            all.Add(new NudgeCommand("ArrowLeft", -1, 0, fine));
            all.Add(new NudgeCommand("ArrowRight", 1, 0, fine));
        }
        for (int i = 0; i < Math.Min(9, NodeTypes.All.Count); i++)
        {
            all.Add(new AddShapeCommand(i + 1, NodeTypes.All[i]));
        }
        for (int i = 1; i <= 9; i++)
        {
            all.Add(new GoToTabCommand(i));
        }
        return new CommandRegistry(all);
    }

    public IReadOnlyList<IEditorCommand> All
    {
        get
        {
            return commands;
        }
    }

    /// <summary>
    /// The command for a key press. Shift only matters where a command asks for it (Ctrl+Shift+S, Ctrl+Shift+G,
    /// Shift+arrows); otherwise Shift+key does the same as the key (Shift+J joins, Ctrl+Shift+A selects all).
    /// </summary>
    public IEditorCommand? Find(KeyGesture pressed)
    {
        if (byKey.TryGetValue(pressed, out IEditorCommand? exact))
        {
            return exact;
        }
        if (pressed.Shift && byKey.TryGetValue(pressed.WithoutShift, out IEditorCommand? plain))
        {
            return plain;
        }
        return null;
    }

    /// <summary>The registered instance of a command type, for wiring buttons.</summary>
    public T Get<T>() where T : IEditorCommand
    {
        foreach (IEditorCommand c in commands)
        {
            if (c is T found)
            {
                return found;
            }
        }
        throw new InvalidOperationException($"No {typeof(T).Name} is registered");
    }
}
