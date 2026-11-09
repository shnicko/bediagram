using BeDiagram.Components.Pages;
using BeDiagram.Editing.Angles;

namespace BeDiagram.Components.Bars;

/// <summary>The Edit menu: undo / redo, select, duplicate, delete, join, branch, group, and the auto-join, snap and angle lock settings.</summary>
public partial class EditMenu : DesignerPart
{

    /// <summary>Toolbar angle lock: a view-time setting on the editor, kept while the page is open, not saved in the file.</summary>
    void SetAngleLock(IAngleLock a) => Editor.AngleLock = a;
}
