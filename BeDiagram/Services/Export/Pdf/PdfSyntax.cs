using System.Text;

namespace BeDiagram.Services.Export.Pdf;

/// <summary>Writing PDF syntax: numbers (at most 4 decimals, tiny values as 0) and string literals. Pure functions
/// shared by the content stream and the file writer, so kept static and internal.</summary>
internal static class PdfSyntax
{
    /// <summary>A number in PDF syntax.</summary>
    public static string F(double v) => Math.Abs(v) < 1e-9 ? "0" : v.ToString("0.####", SvgValues.Inv);

    /// <summary>A PDF string literal, Windows-1252 encoded, with ( ) \ escaped and other bytes as octal.</summary>
    public static string String(string s)
    {
        StringBuilder sb = new StringBuilder("(");
        foreach (byte b in Win1252Encoding.Instance.GetBytes(s ?? ""))
        {
            if (b is (byte)'(' or (byte)')' or (byte)'\\') sb.Append('\\').Append((char)b);
            else if (b < 32 || b > 126) sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
            else sb.Append((char)b);
        }
        return sb.Append(')').ToString();
    }
}
