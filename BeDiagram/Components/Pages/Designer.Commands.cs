using BeDiagram.Components.Pages.Commands;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BeDiagram.Components.Pages;

/// <summary>
/// Running editor commands (Command pattern, see Commands/IEditorCommand.cs): from the keyboard (OnKey) and from
/// toolbar / panel buttons (Run, CanRun, Tip and the *Cmd properties the markup reads).
/// </summary>
public partial class Designer
{
    [Inject] CommandRegistry Commands { get; set; } = default!;

    /// <summary>Key press forwarded by designer.js (only keys it knows about, and not while typing in a field).</summary>
    [JSInvokable]
    public async Task OnKey(string key, bool ctrl, bool shift)
    {
        if (dialog != Dialog.None)
        {
            if (key == "Escape") CloseDialog();
            StateHasChanged();
            return;
        }
        IEditorCommand? command = Commands.Find(KeyGesture.From(key, ctrl, shift));
        if (command is null || !command.CanExecute(this)) return;
        await command.ExecuteAsync(this);
        StateHasChanged();
    }

    async Task Run(IEditorCommand command)
    {
        if (command.CanExecute(this)) await command.ExecuteAsync(this);
    }

    bool CanRun(IEditorCommand command) => command.CanExecute(this);

    /// <summary>Tooltip with the first shortcut, e.g. "Undo (Ctrl+Z)".</summary>
    static string Tip(IEditorCommand command) =>
        command.Keys.Count == 0 ? command.Name : $"{command.Name} ({command.Keys[0]})";

    // Commands wired to buttons in Designer.razor.
    IEditorCommand SaveCmd => Commands.Get<SaveCommand>();
    IEditorCommand SaveAsCmd => Commands.Get<SaveAsCommand>();
    IEditorCommand UndoCmd => Commands.Get<UndoCommand>();
    IEditorCommand RedoCmd => Commands.Get<RedoCommand>();
    IEditorCommand ZoomInCmd => Commands.Get<ZoomInCommand>();
    IEditorCommand ZoomOutCmd => Commands.Get<ZoomOutCommand>();
    IEditorCommand FitCmd => Commands.Get<FitCommand>();
    IEditorCommand FitSelectionCmd => Commands.Get<FitSelectionCommand>();
    IEditorCommand DeleteCmd => Commands.Get<DeleteCommand>();
    IEditorCommand JoinCmd => Commands.Get<JoinCommand>();
    IEditorCommand GroupCmd => Commands.Get<GroupCommand>();
    IEditorCommand UngroupCmd => Commands.Get<UngroupCommand>();
    IEditorCommand AddCommentCmd => Commands.Get<AddCommentCommand>();
}
