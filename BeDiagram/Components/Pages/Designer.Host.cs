using BeDiagram.Components.Pages.Commands;
using BeDiagram.Editing;
using BeDiagram.Services.Export;

namespace BeDiagram.Components.Pages;

/// <summary>
/// The Designer as the host of its child components (<see cref="IDesignerHost"/>): the bars, side panels and other parts
/// reach the editor, the command buttons and the page's own actions through this.
/// </summary>
public partial class Designer : IDesignerHost
{
    DiagramEditor IDesignerHost.Editor => Editor;
    CommandButtons IDesignerHost.Buttons => Cmd;
    void IDesignerHost.Redraw() => StateHasChanged();

    void IDesignerHost.ShowStepOrder() => ShowStepOrder();
    void IDesignerHost.ShowShortcuts() => ShowShortcuts();
    void IDesignerHost.NewDiagram() => NewDiagram();
    void IDesignerHost.ShowOpen() => ShowOpen();
    Task IDesignerHost.Export(ExportChoice choice, ExportOptions options) => Export(choice, options);

    void IDesignerHost.FollowLink(string? link) => FollowLink(link);
    string? IDesignerHost.LinkExclude => LinkExclude;
    IReadOnlyList<string> IDesignerHost.Trail => trail;
    void IDesignerHost.GoBack() => GoBack();
    void IDesignerHost.GoUpTo(int level) => GoUpTo(level);

    void IDesignerHost.SetPageSize(string? size) => SetPageSize(size);
    void IDesignerHost.SetLandscape(bool landscape) => SetOrientation(landscape ? "landscape" : "portrait");
    double IDesignerHost.Zoom => Zoom;
    string IDesignerHost.Grid => grid;
    void IDesignerHost.SetGrid(string key) => grid = key;
    string? IDesignerHost.OpenMenu => openMenu;
    void IDesignerHost.SetOpenMenu(string? menu) => openMenu = menu;
    string? IDesignerHost.Toast => toast;
    void IDesignerHost.SetPaletteDrop(Action<double, double> drop) => paletteDrop = drop;
}
