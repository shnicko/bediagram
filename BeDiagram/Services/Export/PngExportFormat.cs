using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;
using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>
/// PNG image. The SVG export is drawn to pixels by an <see cref="IPngRasterizer"/> (the browser by default),
/// so the PNG looks exactly like the SVG / PDF. The file records its resolution (PNG pHYs chunk),
/// so a page export inserts into Word / PowerPoint at its paper size.
/// </summary>
public partial class PngExportFormat : IExportFormat
{
    // Browsers refuse canvases much bigger than this (Chrome: 16384 px a side; Safari / Firefox: area limits).
    const int MaxSide = 16000;
    const double MaxPixels = 100_000_000;

    readonly IPngRasterizer rasterizer;
    readonly SvgDiagramWriter writer;

    public PngExportFormat(IPngRasterizer rasterizer, SvgDiagramWriter writer)
    {
        this.rasterizer = rasterizer;
        this.writer = writer;
    }

    public string Name => "PNG image";
    public string Extension => ".png";
    public bool UsesArea => true;

    /// <summary>
    /// Resolution choices. On a page they're print resolutions (dpi); on an open canvas, multiples of screen size.
    /// 96 dpi is screen size, as page sizes convert at 96 px per inch (<see cref="PageSetup.PxPerMm"/>).
    /// </summary>
    public static IReadOnlyList<PngResolution> Resolutions(Diagram d) => d.Page.Enabled
        ? [new PngResolution("96 dpi (screen)", 1), new PngResolution("150 dpi", 150 / 96.0), new PngResolution("300 dpi (print)", 300 / 96.0)]
        : [new PngResolution("1× (screen size)", 1), new PngResolution("2×", 2), new PngResolution("3×", 3)];

    public async Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options)
    {
        string svg = writer.Write(d, view, background: !options.PngTransparent);
        (double svgW, double svgH) = SvgSizePx(svg);

        IReadOnlyList<PngResolution> choices = Resolutions(d);
        double scale = choices[Math.Clamp(options.PngResolution, 0, choices.Count - 1)].Scale;

        string? note = null;
        double limit = Math.Min(Math.Min(MaxSide / (svgW * scale), MaxSide / (svgH * scale)),
                                Math.Sqrt(MaxPixels / (svgW * scale * svgH * scale)));
        if (limit < 1)
        {
            scale *= limit;
            note = $"reduced to {scale * 96:0} dpi — the image was too large for the browser";
        }

        int width = Math.Max(1, (int)Math.Round(svgW * scale));
        int height = Math.Max(1, (int)Math.Round(svgH * scale));
        byte[] png = await rasterizer.RasterizeAsync(svg, width, height);
        return new ExportFile(WithResolution(png, 96 * scale), note);
    }

    /// <summary>Reads the root width / height of an exported SVG in px ("297mm" or "1200").</summary>
    static (double W, double H) SvgSizePx(string svg)
    {
        Match root = SvgRoot().Match(svg);
        string tag = root.Success ? root.Value : "";
        return (Length(tag, "width"), Length(tag, "height"));
    }

    static double Length(string tag, string name)
    {
        Match m = Regex.Match(tag, $"\\s{name}=\"([0-9.]+)(mm|px)?\"");
        if (!m.Success) return 400;
        double v = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        return m.Groups[2].Value == "mm" ? v * PageSetup.PxPerMm : v;
    }

    [GeneratedRegex("<svg\\b[^>]*>")]
    private static partial Regex SvgRoot();

    /// <summary>Inserts a pHYs chunk (pixels per metre) after the IHDR chunk, replacing any the browser wrote.</summary>
    static byte[] WithResolution(byte[] png, double dpi)
    {
        const int signature = 8;
        if (png.Length < signature + 25) return png;
        int ihdrEnd = signature + 8 + (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(signature)) + 4;

        List<byte> output = new List<byte>(png.Length + 21);
        output.AddRange(png.AsSpan(0, ihdrEnd).ToArray());

        uint ppm = (uint)Math.Round(dpi / 0.0254);
        byte[] chunk = new byte[4 + 4 + 9 + 4];
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(0), 9);
        chunk[4] = (byte)'p'; chunk[5] = (byte)'H'; chunk[6] = (byte)'Y'; chunk[7] = (byte)'s';
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8), ppm);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(12), ppm);
        chunk[16] = 1; // unit: metre
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(17), Crc32(chunk.AsSpan(4, 13)));
        output.AddRange(chunk);

        // Copy the remaining chunks, dropping any existing pHYs.
        int pos = ihdrEnd;
        while (pos + 12 <= png.Length)
        {
            int len = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(pos));
            int total = 12 + len;
            if (pos + total > png.Length) break;
            bool isPhys = png[pos + 4] == 'p' && png[pos + 5] == 'H' && png[pos + 6] == 'Y' && png[pos + 7] == 's';
            if (!isPhys) output.AddRange(png.AsSpan(pos, total).ToArray());
            pos += total;
        }
        return output.ToArray();
    }

    static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }
}
