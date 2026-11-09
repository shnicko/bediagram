using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using static BeDiagram.Services.Export.Pdf.PdfSyntax;
using static BeDiagram.Services.Export.Pdf.SvgValues;

namespace BeDiagram.Services.Export.Pdf;

/// <summary>
/// Turns the SVG produced by <see cref="SvgDiagramWriter"/> into a one-page vector PDF, with no third-party libraries.
/// It understands the SVG subset BeDiagram writes (and the Lucide icons): g/rect/path/circle/ellipse/line/
/// polyline/polygon/text/tspan, transforms, markers, rectangular clip paths, opacity, dashes and the
/// standard PDF fonts (Helvetica / Times / Courier). Rendering from the SVG keeps one geometry for all exports.
/// Registered once in Program.cs and given to <see cref="PdfExportFormat"/>.
/// </summary>
public class PdfWriter
{
    /// <summary>One page PDF, sized from the SVG's width/height (mm or px).</summary>
    public byte[] FromSvg(string svg, string title)
    {
        XElement root = XDocument.Parse(svg).Root ?? throw new InvalidOperationException("Empty SVG");
        List<double> vb = Nums(Attr(root, "viewBox") ?? "");
        double vbX = 0, vbY = 0, vbW, vbH;
        double wPt = Length(Attr(root, "width"), out double wPx), hPt = Length(Attr(root, "height"), out double hPx);
        if (vb.Count == 4) { vbX = vb[0]; vbY = vb[1]; vbW = vb[2]; vbH = vb[3]; }
        else { vbW = wPx; vbH = hPx; }
        if (wPt <= 0) wPt = vbW * PtPerPx;
        if (hPt <= 0) hPt = vbH * PtPerPx;

        PdfRenderer r = new PdfRenderer();
        foreach (XElement el in root.Descendants())
        {
            string? id = Attr(el, "id");
            if (id is null) continue;
            if (el.Name.LocalName == "marker") r.Markers[id] = el;
            else if (el.Name.LocalName == "clipPath") r.Clips[id] = el;
        }
        // SVG is y-down from the viewBox origin; PDF is y-up in points.
        double sx = wPt / vbW, sy = hPt / vbH;
        r.Out.Append($"{F(sx)} 0 0 {F(-sy)} {F(-vbX * sx)} {F(hPt + vbY * sy)} cm\n");
        r.Children(root, PdfStyle.Default);
        return Write(r, wPt, hPt, title);
    }

    byte[] Write(PdfRenderer r, double wPt, double hPt, string title)
    {
        byte[] content = Encoding.ASCII.GetBytes(r.Out.ToString());
        byte[] packed;
        using (MemoryStream ms = new MemoryStream())
        {
            using (ZLibStream z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) z.Write(content);
            packed = ms.ToArray();
        }

        List<byte[]> objects = new List<byte[]>();
        int Add(string s) { objects.Add(Encoding.ASCII.GetBytes(s)); return objects.Count; }
        int AddRaw(byte[] b) { objects.Add(b); return objects.Count; }

        // 1 catalog, 2 pages, 3 page, 4 content, then fonts, graphics states and info.
        Add("<< /Type /Catalog /Pages 2 0 R >>");
        Add("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        Add(""); // page — filled in once resource object numbers are known
        byte[] head = Encoding.ASCII.GetBytes($"<< /Length {packed.Length} /Filter /FlateDecode >>\nstream\n");
        byte[] tail = Encoding.ASCII.GetBytes("\nendstream");
        AddRaw(head.Concat(packed).Concat(tail).ToArray());

        StringBuilder fontRefs = new StringBuilder();
        foreach (int f in r.FontsUsed)
        {
            int n = Add($"<< /Type /Font /Subtype /Type1 /BaseFont /{r.Fonts.BaseName(f)} /Encoding /WinAnsiEncoding >>");
            fontRefs.Append($"/F{f} {n} 0 R ");
        }
        StringBuilder gsRefs = new StringBuilder();
        foreach (((double, double) key, string? name) in r.Alpha)
        {
            int n = Add($"<< /Type /ExtGState /ca {F(key.Item1)} /CA {F(key.Item2)} >>");
            gsRefs.Append($"/{name} {n} 0 R ");
        }
        int info = Add($"<< /Title {PdfSyntax.String(title)} /Producer (BeDiagram) /CreationDate (D:{DateTime.UtcNow:yyyyMMddHHmmss}Z) >>");

        string resources = "<< /ProcSet [/PDF /Text]" +
            (fontRefs.Length > 0 ? $" /Font << {fontRefs}>>" : "") +
            (gsRefs.Length > 0 ? $" /ExtGState << {gsRefs}>>" : "") + " >>";
        objects[2] = Encoding.ASCII.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {F(wPt)} {F(hPt)}] /Resources {resources} /Contents 4 0 R >>");

        using MemoryStream outStream = new MemoryStream();
        void W(string s) { byte[] b = Encoding.ASCII.GetBytes(s); outStream.Write(b); }
        W("%PDF-1.4\n");
        outStream.Write(new byte[] { (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n' });
        List<long> offsets = new List<long>();
        for (int k = 0; k < objects.Count; k++)
        {
            offsets.Add(outStream.Position);
            W($"{k + 1} 0 obj\n");
            outStream.Write(objects[k]);
            W("\nendobj\n");
        }
        long xref = outStream.Position;
        W($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (long o in offsets) W($"{o:D10} 00000 n \n");
        W($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info {info} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return outStream.ToArray();
    }
}
