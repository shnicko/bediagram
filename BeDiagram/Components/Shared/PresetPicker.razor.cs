using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Models.Presets;
using BeDiagram.Services;

namespace BeDiagram.Components.Shared;

/// <summary>
/// The Saved styles block of a style panel (shapes, groups, text boxes, connectors): pick a saved style to apply or
/// delete, and save the selected item's look under a name, with or without its text. The parent says which styles
/// to list, how to make a style from its item and how to apply one; this component saves and deletes in the
/// <see cref="StylePresetStore"/>.
/// </summary>
public partial class PresetPicker
{
    [Inject] StylePresetStore Store { get; set; } = default!;

    /// <summary>The saved styles of this kind.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<StylePreset> Presets { get; set; } = default!;

    /// <summary>Makes a style from the selected item with the chosen text; null when it can't be saved (several
    /// items selected).</summary>
    [Parameter] public Func<PresetText, StylePreset>? Capture { get; set; }

    /// <summary>The text choices offered when saving (<see cref="PresetTexts.ForShape"/> or <see cref="PresetTexts.ForOneText"/>).</summary>
    [Parameter] public (PresetText Key, string Name)[] TextChoices { get; set; } = PresetTexts.ForOneText;

    /// <summary>Applies the chosen style to the selected items.</summary>
    [Parameter] public EventCallback<StylePreset> OnApply { get; set; }

    /// <summary>Called after a save or delete, so the page redraws (the sidebar lists the styles too).</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    string ChosenId { get; set; } = "";

    string NewName { get; set; } = "";

    PresetText Text { get; set; } = PresetText.None;

    /// <summary>True after the first click on delete: the second click deletes.</summary>
    bool deleteArmed;

    StylePreset? Chosen
    {
        get
        {
            foreach (StylePreset p in Presets)
            {
                if (p.Id == ChosenId)
                {
                    return p;
                }
            }
            return null;
        }
    }

    bool HasName
    {
        get
        {
            return NewName.Trim().Length > 0;
        }
    }

    /// <summary>The saved style a save would replace (same kind, same name), or null.</summary>
    StylePreset? Replacing
    {
        get
        {
            if (!HasName)
            {
                return null;
            }
            foreach (StylePreset p in Presets)
            {
                if (string.Equals(p.Name, NewName.Trim(), StringComparison.CurrentCultureIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }
    }

    string SaveText
    {
        get
        {
            if (Replacing is not null)
            {
                return "Replace";
            }
            return "Save";
        }
    }

    string ApplyTitle
    {
        get
        {
            if (Chosen is null)
            {
                return "Choose a style first";
            }
            return $"Apply “{Chosen.Name}” to the selection";
        }
    }

    string DeleteText
    {
        get
        {
            if (deleteArmed)
            {
                return "Delete?";
            }
            return "✕";
        }
    }

    string DeleteClass
    {
        get
        {
            if (deleteArmed)
            {
                return "danger";
            }
            return "";
        }
    }

    string DeleteTitle
    {
        get
        {
            if (Chosen is null)
            {
                return "Choose a style first";
            }
            if (deleteArmed)
            {
                return $"Click again to delete “{Chosen.Name}” for good";
            }
            return $"Delete the saved style “{Chosen.Name}”";
        }
    }

    string TextClass(PresetText k)
    {
        if (k == Text)
        {
            return "on";
        }
        return "";
    }

    void TextPicked(PresetText k)
    {
        Text = k;
    }

    void Disarm()
    {
        deleteArmed = false;
    }

    async Task Apply()
    {
        StylePreset? p = Chosen;
        if (p is null)
        {
            return;
        }
        deleteArmed = false;
        await OnApply.InvokeAsync(p);
    }

    async Task DeleteClicked()
    {
        StylePreset? p = Chosen;
        if (p is null)
        {
            return;
        }
        if (!deleteArmed)
        {
            deleteArmed = true;
            return;
        }
        deleteArmed = false;
        Store.Delete(p.Id);
        ChosenId = "";
        await OnChanged.InvokeAsync();
    }

    async Task Save()
    {
        if (Capture is null || !HasName)
        {
            return;
        }
        StylePreset p = Capture(Text);
        p.Name = NewName.Trim();
        Store.Save(p);
        NewName = "";
        ChosenId = p.Id;
        await OnChanged.InvokeAsync();
    }

    async Task NameKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await Save();
        }
    }
}
