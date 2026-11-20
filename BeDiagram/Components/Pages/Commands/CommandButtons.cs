using BeDiagram.Editing;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// Commands as buttons: what the menus, toolbar and panels use to run a command, grey it out and show its tooltip.
/// One per page, made by the Designer (the <see cref="ICommandContext"/> the commands run against) and handed to its
/// child components through <see cref="IDesignerHost.Buttons"/>. The keyboard goes through the same commands
/// (<see cref="CommandRegistry.Find"/>), so a button and its shortcut always do the same thing.
/// </summary>
public sealed class CommandButtons
{
    readonly CommandRegistry registry;
    readonly ICommandContext context;
    readonly DiagramEditor editor;

    public CommandButtons(CommandRegistry registry, ICommandContext context, DiagramEditor editor)
    {
        this.registry = registry;
        this.context = context;
        this.editor = editor;
    }

    public async Task Run(IEditorCommand command)
    {
        if (command.CanExecute(context))
        {
            await command.ExecuteAsync(context);
        }
    }

    public bool CanRun(IEditorCommand command)
    {
        return command.CanExecute(context);
    }

    /// <summary>The first shortcut of a command for a menu item, e.g. "Ctrl+S" (empty when it has none).</summary>
    public static string KeyText(IEditorCommand command)
    {
        if (command.Keys.Count == 0)
        {
            return "";
        }
        return command.Keys[0].ToString();
    }

    /// <summary>Tooltip with the first shortcut, e.g. "Undo (Ctrl+Z)".</summary>
    public static string Tip(IEditorCommand command)
    {
        if (command.Keys.Count == 0)
        {
            return command.Name;
        }
        return $"{command.Name} ({command.Keys[0]})";
    }

    /// <summary>Tooltip for the swim-lane buttons (they have no shortcut key, so say what they do or why they're off).</summary>
    public string LanesTip(IEditorCommand command)
    {
        if (CanRun(command))
        {
            return $"{command.Name}: line the lanes up and put their shapes in columns by step number";
        }
        return "Group the shapes of each lane first (select them, Ctrl+G)";
    }

    /// <summary>Tooltip for the auto-layout buttons.</summary>
    public string LayoutTip(IEditorCommand command)
    {
        if (CanRun(command))
        {
            return $"{command.Name}: put every shape in columns by step number (groups move as blocks), then tidy the connectors";
        }
        return "Nothing to lay out yet";
    }

    /// <summary>Tooltip for Tidy: what it will work on, given the selection.</summary>
    public string TidyTip
    {
        get
        {
            if (!CanRun(Tidy))
            {
                return "No connectors yet";
            }
            if (editor.SelectedEdge is not null)
            {
                return "Tidy the selected connector: re-route it round any shape it passes through";
            }
            if (editor.Selection.Count > 0)
            {
                return "Tidy the connectors on the selected items: re-route any that pass through a shape";
            }
            return "Tidy connectors: re-route any that pass through a shape (select items first to tidy just theirs)";
        }
    }

    /// <summary>Tooltip for Place text: what it will work on, given the selection.</summary>
    public string PlaceTextTip
    {
        get
        {
            if (!CanRun(PlaceText))
            {
                return "No shape has text outside it yet (a label under a circle or outcome, or a description)";
            }
            if (editor.Selection.Count > 0)
            {
                return "Move the text of the selected shapes to the side (under, above, right or left) with the fewest connectors through it";
            }
            return "Move the text under shapes to the side (under, above, right or left) with the fewest connectors through it (select shapes first to do just theirs)";
        }
    }

    // The commands that have buttons.
    public IEditorCommand Save
    {
        get
        {
            return registry.Get<SaveCommand>();
        }
    }
    public IEditorCommand SaveAs
    {
        get
        {
            return registry.Get<SaveAsCommand>();
        }
    }
    public IEditorCommand Undo
    {
        get
        {
            return registry.Get<UndoCommand>();
        }
    }
    public IEditorCommand Redo
    {
        get
        {
            return registry.Get<RedoCommand>();
        }
    }
    public IEditorCommand SelectAll
    {
        get
        {
            return registry.Get<SelectAllCommand>();
        }
    }
    public IEditorCommand Duplicate
    {
        get
        {
            return registry.Get<DuplicateCommand>();
        }
    }
    public IEditorCommand Delete
    {
        get
        {
            return registry.Get<DeleteCommand>();
        }
    }
    public IEditorCommand Join
    {
        get
        {
            return registry.Get<JoinCommand>();
        }
    }
    public IEditorCommand Branch
    {
        get
        {
            return registry.Get<QuickBranchCommand>();
        }
    }
    public IEditorCommand Group
    {
        get
        {
            return registry.Get<GroupCommand>();
        }
    }
    public IEditorCommand Ungroup
    {
        get
        {
            return registry.Get<UngroupCommand>();
        }
    }
    public IEditorCommand AddComment
    {
        get
        {
            return registry.Get<AddCommentCommand>();
        }
    }
    public IEditorCommand LanesAcross
    {
        get
        {
            return registry.Get<ArrangeLanesHorizontalCommand>();
        }
    }
    public IEditorCommand LanesDown
    {
        get
        {
            return registry.Get<ArrangeLanesVerticalCommand>();
        }
    }
    public IEditorCommand AngleToggle
    {
        get
        {
            return registry.Get<ToggleAngleLockCommand>();
        }
    }
    public IEditorCommand Tidy
    {
        get
        {
            return registry.Get<TidyConnectorsCommand>();
        }
    }
    public IEditorCommand PlaceText
    {
        get
        {
            return registry.Get<PlaceTextCommand>();
        }
    }
    public IEditorCommand LayoutAcross
    {
        get
        {
            return registry.Get<AutoLayoutHorizontalCommand>();
        }
    }
    public IEditorCommand LayoutDown
    {
        get
        {
            return registry.Get<AutoLayoutVerticalCommand>();
        }
    }
    public IEditorCommand ZoomIn
    {
        get
        {
            return registry.Get<ZoomInCommand>();
        }
    }
    public IEditorCommand ZoomOut
    {
        get
        {
            return registry.Get<ZoomOutCommand>();
        }
    }
    public IEditorCommand ResetZoom
    {
        get
        {
            return registry.Get<ResetZoomCommand>();
        }
    }
    public IEditorCommand Fit
    {
        get
        {
            return registry.Get<FitCommand>();
        }
    }
    public IEditorCommand FitSelection
    {
        get
        {
            return registry.Get<FitSelectionCommand>();
        }
    }
}
