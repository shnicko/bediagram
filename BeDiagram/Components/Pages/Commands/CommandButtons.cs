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
        if (command.CanExecute(context)) await command.ExecuteAsync(context);
    }

    public bool CanRun(IEditorCommand command) => command.CanExecute(context);

    /// <summary>The first shortcut of a command for a menu item, e.g. "Ctrl+S" (empty when it has none).</summary>
    public static string KeyText(IEditorCommand command) => command.Keys.Count == 0 ? "" : command.Keys[0].ToString();

    /// <summary>Tooltip with the first shortcut, e.g. "Undo (Ctrl+Z)".</summary>
    public static string Tip(IEditorCommand command) =>
        command.Keys.Count == 0 ? command.Name : $"{command.Name} ({command.Keys[0]})";

    /// <summary>Tooltip for the swim-lane buttons (they have no shortcut key, so say what they do or why they're off).</summary>
    public string LanesTip(IEditorCommand command) => CanRun(command)
        ? $"{command.Name}: line the lanes up and put their shapes in columns by step number"
        : "Group the shapes of each lane first (select them, Ctrl+G)";

    /// <summary>Tooltip for the auto-layout buttons.</summary>
    public string LayoutTip(IEditorCommand command) => CanRun(command)
        ? $"{command.Name}: put every shape in columns by step number (groups move as blocks), then tidy the connectors"
        : "Nothing to lay out yet";

    /// <summary>Tooltip for Tidy: what it will work on, given the selection.</summary>
    public string TidyTip => !CanRun(Tidy) ? "No connectors yet"
        : editor.SelectedEdge is not null ? "Tidy the selected connector: re-route it round any shape it passes through"
        : editor.Selection.Count > 0 ? "Tidy the connectors on the selected items: re-route any that pass through a shape"
        : "Tidy connectors: re-route any that pass through a shape (select items first to tidy just theirs)";

    // The commands that have buttons.
    public IEditorCommand Save => registry.Get<SaveCommand>();
    public IEditorCommand SaveAs => registry.Get<SaveAsCommand>();
    public IEditorCommand Undo => registry.Get<UndoCommand>();
    public IEditorCommand Redo => registry.Get<RedoCommand>();
    public IEditorCommand SelectAll => registry.Get<SelectAllCommand>();
    public IEditorCommand Duplicate => registry.Get<DuplicateCommand>();
    public IEditorCommand Delete => registry.Get<DeleteCommand>();
    public IEditorCommand Join => registry.Get<JoinCommand>();
    public IEditorCommand Branch => registry.Get<QuickBranchCommand>();
    public IEditorCommand Group => registry.Get<GroupCommand>();
    public IEditorCommand Ungroup => registry.Get<UngroupCommand>();
    public IEditorCommand AddComment => registry.Get<AddCommentCommand>();
    public IEditorCommand LanesAcross => registry.Get<ArrangeLanesHorizontalCommand>();
    public IEditorCommand LanesDown => registry.Get<ArrangeLanesVerticalCommand>();
    public IEditorCommand Tidy => registry.Get<TidyConnectorsCommand>();
    public IEditorCommand LayoutAcross => registry.Get<AutoLayoutHorizontalCommand>();
    public IEditorCommand LayoutDown => registry.Get<AutoLayoutVerticalCommand>();
    public IEditorCommand ZoomIn => registry.Get<ZoomInCommand>();
    public IEditorCommand ZoomOut => registry.Get<ZoomOutCommand>();
    public IEditorCommand ResetZoom => registry.Get<ResetZoomCommand>();
    public IEditorCommand Fit => registry.Get<FitCommand>();
    public IEditorCommand FitSelection => registry.Get<FitSelectionCommand>();
}
