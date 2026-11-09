using System.Globalization;

namespace BeDiagram.Models;

/// <summary>
/// How numbers are written into SVG markup and path data: at most two decimals, invariant culture ("12.5", not
/// "12,50"). One copy for the canvas, the exports, the outlines and the layouts; import it with
/// <c>using static BeDiagram.Models.SvgNumbers;</c> and write <c>F(x)</c>. (The PDF writer keeps its own, finer
/// <c>PdfSyntax.F</c> on purpose.)
/// </summary>
public static class SvgNumbers
{
    public static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
