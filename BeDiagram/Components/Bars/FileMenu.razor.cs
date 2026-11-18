using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models.Catalogue;
using BeDiagram.Services.Export;

namespace BeDiagram.Components.Bars;

/// <summary>The File menu: new, open, save, export (with the PNG settings), flow direction and page size.</summary>
public partial class FileMenu : DesignerPart
{
    [Inject] ExportService Exports { get; set; } = default!;

    /// <summary>The PNG settings in this menu, used by every export from it (kept while the page is open).</summary>
    readonly ExportOptions exportOptions = new ExportOptions();

    Task Export(ExportChoice choice)
    {
        return Host.Export(choice, exportOptions);
    }

    void PageSizePicked(ChangeEventArgs e)
    {
        Host.SetPageSize(e.Value?.ToString());
    }

    string FlowClass(FlowDirection flow)
    {
        if (D.Flow == flow)
        {
            return "on";
        }
        return "";
    }

    /// <summary>One Export menu entry, worked out for the markup. StartsGroup draws a divider before a new format.</summary>
    record ExportMenuItem(ExportChoice Choice, string Label, string Tip, bool StartsGroup);

    /// <summary>The Export menu: every format × area combination the <see cref="ExportService"/> offers.</summary>
    IReadOnlyList<ExportMenuItem> ExportMenuItems()
    {
        List<ExportMenuItem> items = new List<ExportMenuItem>();
        IExportFormat? previous = null;
        foreach (ExportChoice c in Exports.Choices())
        {
            bool startsGroup = previous is not null && previous != c.Format;
            items.Add(new ExportMenuItem(c, c.Label(D), c.Tip(D), startsGroup));
            previous = c.Format;
        }
        return items;
    }

    IReadOnlyList<PngResolution> PngResolutions
    {
        get
        {
            return PngExportFormat.Resolutions(D);
        }
    }

    void SetPngResolution(ChangeEventArgs e)
    {
        int i;
        if (int.TryParse(e.Value?.ToString(), out i))
        {
            exportOptions.PngResolution = i;
        }
        else
        {
            exportOptions.PngResolution = 1;
        }
    }

    void SetPngTransparent(ChangeEventArgs e)
    {
        exportOptions.PngTransparent = e.Value is true;
    }
}
