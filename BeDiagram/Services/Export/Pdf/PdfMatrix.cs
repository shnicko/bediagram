using System.Text.RegularExpressions;
using static BeDiagram.Services.Export.Pdf.SvgValues;

namespace BeDiagram.Services.Export.Pdf;

/// <summary>An affine transform (a b c d e f), as SVG and PDF both write it.</summary>
internal readonly record struct PdfMatrix(double A, double B, double C, double D, double E, double F)
{
    public static PdfMatrix Translate(double x, double y) => new(1, 0, 0, 1, x, y);
    public static PdfMatrix Scale(double x, double y) => new(x, 0, 0, y, 0, 0);
    public static PdfMatrix Rotate(double deg)
    {
        double r = deg * Math.PI / 180;
        return new(Math.Cos(r), Math.Sin(r), -Math.Sin(r), Math.Cos(r), 0, 0);
    }

    /// <summary>The <c>cm</c> operator that applies this matrix.</summary>
    public string Cm => $"{PdfSyntax.F(A)} {PdfSyntax.F(B)} {PdfSyntax.F(C)} {PdfSyntax.F(D)} {PdfSyntax.F(E)} {PdfSyntax.F(F)} cm\n";

    /// <summary>SVG transform list in the order written (each applied inside the previous).</summary>
    public static IEnumerable<PdfMatrix> Parse(string t)
    {
        foreach (Match m in Regex.Matches(t, @"(\w+)\s*\(([^)]*)\)"))
        {
            List<double> a = Nums(m.Groups[2].Value);
            switch (m.Groups[1].Value)
            {
                case "translate": yield return PdfMatrix.Translate(a.ElementAtOrDefault(0), a.ElementAtOrDefault(1)); break;
                case "scale": yield return PdfMatrix.Scale(a.ElementAtOrDefault(0), a.Count > 1 ? a[1] : a.ElementAtOrDefault(0)); break;
                case "rotate":
                    if (a.Count >= 3)
                    {
                        yield return PdfMatrix.Translate(a[1], a[2]);
                        yield return PdfMatrix.Rotate(a[0]);
                        yield return PdfMatrix.Translate(-a[1], -a[2]);
                    }
                    else yield return PdfMatrix.Rotate(a.ElementAtOrDefault(0));
                    break;
                case "matrix" when a.Count == 6: yield return new PdfMatrix(a[0], a[1], a[2], a[3], a[4], a[5]); break;
                case "skewX": yield return new PdfMatrix(1, 0, Math.Tan(a.ElementAtOrDefault(0) * Math.PI / 180), 1, 0, 0); break;
                case "skewY": yield return new PdfMatrix(1, Math.Tan(a.ElementAtOrDefault(0) * Math.PI / 180), 0, 1, 0, 0); break;
            }
        }
    }
}
