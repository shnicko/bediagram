using BeDiagram.Components.Pages.Commands;
using BeDiagram.Editing;

namespace BeDiagram.Components.Pages;

/// <summary>
/// The Designer as the host of its child components (<see cref="IDesignerHost"/>): the side panels and other parts
/// reach the editor, the command buttons and the page's own actions through this.
/// </summary>
public partial class Designer : IDesignerHost
{
    DiagramEditor IDesignerHost.Editor => Editor;
    CommandButtons IDesignerHost.Buttons => Cmd;
    void IDesignerHost.Redraw() => StateHasChanged();
    void IDesignerHost.ShowStepOrder() => ShowStepOrder();
    void IDesignerHost.FollowLink(string? link) => FollowLink(link);
    string? IDesignerHost.LinkExclude => LinkExclude;
    void IDesignerHost.SetPageSize(string? size) => SetPageSize(size);
    void IDesignerHost.SetLandscape(bool landscape) => SetOrientation(landscape ? "landscape" : "portrait");
}
