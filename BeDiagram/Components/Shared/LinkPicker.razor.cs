using BeDiagram.Services;
using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>Panel section: pick the saved diagram that holds the more detailed flow.</summary>
public partial class LinkPicker
{
    [Inject] DiagramStore Store { get; set; } = default!;

    /// <summary>The current link (diagram name), or null.</summary>
    [Parameter] public string? Current { get; set; }
    /// <summary>Name of the open diagram, left out of the list (a diagram can't link to itself). Null when unsaved.</summary>
    [Parameter] public string? Exclude { get; set; }
    [Parameter] public EventCallback<string?> OnChange { get; set; }
    [Parameter] public EventCallback<string> OnFollow { get; set; }

    List<string> Names = new();

    bool Has => !string.IsNullOrWhiteSpace(Current);
    bool Missing => Has && !Store.Exists(Current!);

    protected override void OnParametersSet()
    {
        Names = Store.List().Select(f => f.Name).Where(nm => nm != Exclude).ToList();
    }

    Task Changed(ChangeEventArgs e)
    {
        string? value = e.Value?.ToString();
        return OnChange.InvokeAsync(string.IsNullOrEmpty(value) ? null : value);
    }

    Task Open() => OnFollow.InvokeAsync(Current!);
}
