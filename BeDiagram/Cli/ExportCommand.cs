using BeDiagram.Models;
using BeDiagram.Services.Export;
using BeDiagram.Services.Export.Pdf;

namespace BeDiagram.Cli;

/// <summary>
/// <c>export</c>: writes the whole page (or drawing extents) of each diagram as SVG, PDF and / or Mermaid, through the
/// same <see cref="IExportFormat"/> classes as the Export menu. The files go next to the diagram, or into <c>--out</c>.
/// PNG is drawn by the browser, so it is only available in the app.
/// </summary>
public class ExportCommand : DiagramFileCommand
{
    readonly List<(string Flag, IExportFormat Format)> formats;

    public ExportCommand()
    {
        SvgDiagramWriter writer = new SvgDiagramWriter();
        formats = new List<(string Flag, IExportFormat Format)>();
        formats.Add(("svg", new SvgExportFormat(writer)));
        formats.Add(("pdf", new PdfExportFormat(writer, new PdfWriter())));
        formats.Add(("mermaid", new MermaidExportFormat()));
    }

    public override string Name
    {
        get
        {
            return "export";
        }
    }

    public override string Summary
    {
        get
        {
            return "Export each diagram's page as SVG, PDF or Mermaid";
        }
    }

    public override string Usage
    {
        get
        {
            return "<files or folders> [--svg] [--pdf] [--mermaid] [--out <folder>]   (default: --svg --pdf)";
        }
    }

    protected override async Task<bool> RunOn(string path, Diagram d, CliArguments args, TextWriter output)
    {
        if (args.Has("png"))
        {
            output.WriteLine("PNG is drawn by the browser, so it can only be exported from the app. Use --svg or --pdf.");
            return false;
        }

        // The formats asked for, or SVG and PDF when none were.
        List<IExportFormat> chosen = new List<IExportFormat>();
        foreach ((string flag, IExportFormat format) in formats)
        {
            if (args.Has(flag))
            {
                chosen.Add(format);
            }
        }
        if (chosen.Count == 0)
        {
            chosen.Add(formats[0].Format);
            chosen.Add(formats[1].Format);
        }

        string? folder = args.Option("out");
        if (folder is null)
        {
            folder = Path.GetDirectoryName(Path.GetFullPath(path));
        }
        Directory.CreateDirectory(folder!);

        List<string> written = new List<string>();
        foreach (IExportFormat format in chosen)
        {
            ExportFile file = await format.RenderAsync(d, null, new ExportOptions());
            string target = Path.Combine(folder!, DiagramFile.NameOf(path) + format.Extension);
            await File.WriteAllBytesAsync(target, file.Content);
            written.Add(Path.GetFileName(target));
            if (file.Note is not null)
            {
                written.Add("(" + file.Note + ")");
            }
        }
        output.WriteLine(path + ": " + string.Join(", ", written));
        return true;
    }
}
