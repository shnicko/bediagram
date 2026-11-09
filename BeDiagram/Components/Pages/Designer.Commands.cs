using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BeDiagram.Components.Pages.Commands;

namespace BeDiagram.Components.Pages;

/// <summary>
/// Running editor commands (Command pattern, see Commands/IEditorCommand.cs): from the keyboard (OnKey) and from
/// toolbar / panel buttons (Run, CanRun, Tip and <see cref="CommandButtons"/>, read as Cmd.Save, Cmd.Delete, …).
/// </summary>
public partial class Designer
{
    [Inject] CommandRegistry Commands { get; set; } = default!;

    /// <summary>Key press forwarded by designer.js (only keys it knows about, and not while typing in a field).</summary>
    [JSInvokable]
    public async Task OnKey(string key, bool ctrl, bool shift)
    {
        if (openMenu is not null && key == "Escape")
        {
            CloseMenus();
            StateHasChanged();
            return;
        }
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

    /// <summary>The commands with buttons (made on first use: <see cref="Commands"/> is injected after construction).</summary>
    CommandButtons Cmd => buttons ??= new CommandButtons(Commands, this, Editor);
    CommandButtons? buttons;

    Task Run(IEditorCommand command) => Cmd.Run(command);

    bool CanRun(IEditorCommand command) => Cmd.CanRun(command);

    static string KeyText(IEditorCommand command) => CommandButtons.KeyText(command);

    static string Tip(IEditorCommand command) => CommandButtons.Tip(command);
}
