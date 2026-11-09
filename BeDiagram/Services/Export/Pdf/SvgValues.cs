using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BeDiagram.Services.Export.Pdf;

/// <summary>
/// Reading values out of the SVG: attributes (including <c>style="…"</c>), numbers and number lists, <c>url(#id)</c>
/// references, lengths in mm / cm / in / pt / px, and colours. Small pure functions shared by every part of the PDF
/// writer, so kept static and internal.
/// </summary>
internal static class SvgValues
{
    public const double PtPerPx = 0.75, PtPerMm = 72 / 25.4;
    public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string? Attr(XElement e, string name)
    {
        string? v = e.Attribute(name)?.Value;
        if (v is null && e.Attribute("style")?.Value is { } style)
        {
            foreach (string part in style.Split(';'))
            {
                string[] kv = part.Split(':', 2);
                if (kv.Length == 2 && kv[0].Trim() == name) return kv[1].Trim();
            }
        }
        return v;
    }

    public static double Num(string? s, double fallback) =>
        s is not null && double.TryParse(s.Trim().TrimEnd('%'), NumberStyles.Float, Inv, out double v) ? v : fallback;

    public static List<double> Nums(string s)
    {
        SvgPathTokens t = new SvgPathTokens(s);
        List<double> list = new List<double>();
        while (t.More()) list.Add(t.Num());
        return list;
    }

    public static string? ClipRef(string v)
    {
        Match m = System.Text.RegularExpressions.Regex.Match(v, @"url\(\s*#([^)\s]+)\s*\)");
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>Width/height attribute in points (mm or px); px value returned too.</summary>
    public static double Length(string? s, out double px)
    {
        px = 0;
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim();
        if (s.EndsWith("mm")) { double mm = Num(s[..^2], 0); px = mm * 96 / 25.4; return mm * PtPerMm; }
        if (s.EndsWith("cm")) { double cm = Num(s[..^2], 0); px = cm * 960 / 25.4; return cm * 10 * PtPerMm; }
        if (s.EndsWith("in")) { double inch = Num(s[..^2], 0); px = inch * 96; return inch * 72; }
        if (s.EndsWith("pt")) { double pt = Num(s[..^2], 0); px = pt / PtPerPx; return pt; }
        if (s.EndsWith("%")) return 0;
        px = Num(s.Replace("px", ""), 0);
        return px * PtPerPx;
    }

    static readonly Dictionary<string, string> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = "#000000", ["white"] = "#ffffff", ["red"] = "#ff0000", ["green"] = "#008000", ["blue"] = "#0000ff",
        ["gray"] = "#808080", ["grey"] = "#808080", ["yellow"] = "#ffff00", ["orange"] = "#ffa500",
    };

    public static (double R, double G, double B)? Colour(string? c)
    {
        if (string.IsNullOrWhiteSpace(c)) return null;
        c = c.Trim();
        if (c is "none" or "transparent" || c.StartsWith("url(")) return null;
        if (Named.TryGetValue(c, out string? hex)) c = hex;
        if (c.StartsWith('#'))
        {
            string h = c[1..];
            if (h.Length is 3 or 4) h = string.Concat(h.Take(3).Select(ch => $"{ch}{ch}"));
            if (h.Length >= 6 && int.TryParse(h[..6], NumberStyles.HexNumber, Inv, out int v))
                return (((v >> 16) & 255) / 255.0, ((v >> 8) & 255) / 255.0, (v & 255) / 255.0);
            return null;
        }
        Match m = System.Text.RegularExpressions.Regex.Match(c, @"rgba?\(([^)]*)\)");
        if (m.Success)
        {
            double[] n = m.Groups[1].Value.Split(',').Select(x => Num(x, 0)).ToArray();
            if (n.Length >= 3) return (n[0] / 255, n[1] / 255, n[2] / 255);
        }
        return (0, 0, 0);
    }
}
