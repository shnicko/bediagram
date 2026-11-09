using System.Text;
using static BeDiagram.Services.Export.Pdf.PdfSyntax;

namespace BeDiagram.Services.Export.Pdf;

/// <summary>
/// SVG path data turned into PDF path operators (m / l / c / h), with arcs and quadratics converted to cubics, plus
/// the start and end points and directions that markers (arrowheads) are drawn at.
/// </summary>
internal sealed class SvgPath
{
    public string Ops = "";
    public bool IsOpenLine;
    public (double X, double Y, double A)? Start, End;

    public static SvgPath Parse(string d)
    {
        SvgPath p = new SvgPath();
        StringBuilder sb = new StringBuilder();
        SvgPathTokens tok = new SvgPathTokens(d);
        double cx = 0, cy = 0, sx = 0, sy = 0;     // current point, subpath start
        double lcx = 0, lcy = 0;                   // last control point (for S/T)
        char prev = ' ';
        char cmd = ' ';
        double lastDirX = 0, lastDirY = 0;
        bool firstSeg = true;

        void Seg(double fromX, double fromY, double c1x, double c1y, double c2x, double c2y, double toX, double toY, bool curve)
        {
            // Start tangent of the very first segment and end tangent of the latest one (for markers).
            if (firstSeg)
            {
                (double dx, double dy) = curve ? Dir(fromX, fromY, c1x, c1y, c2x, c2y, toX, toY) : (toX - fromX, toY - fromY);
                if (dx != 0 || dy != 0) { p.Start = (fromX, fromY, Math.Atan2(dy, dx) * 180 / Math.PI); firstSeg = false; }
            }
            (double ex, double ey) = curve ? Dir(toX, toY, c2x, c2y, c1x, c1y, fromX, fromY) : (fromX - toX, fromY - toY);
            if (ex != 0 || ey != 0) { lastDirX = -ex; lastDirY = -ey; }
            p.End = (toX, toY, Math.Atan2(lastDirY, lastDirX) * 180 / Math.PI);
            if (curve) sb.Append($"{F(c1x)} {F(c1y)} {F(c2x)} {F(c2y)} {F(toX)} {F(toY)} c\n");
            else sb.Append($"{F(toX)} {F(toY)} l\n");
        }

        while (tok.More())
        {
            if (tok.PeekCommand() is { } c) { cmd = c; tok.Skip(); }
            else if (cmd == ' ') break;
            bool rel = char.IsLower(cmd);
            double ox = rel ? cx : 0, oy = rel ? cy : 0;
            switch (char.ToUpper(cmd))
            {
                case 'M':
                    cx = ox + tok.Num(); cy = oy + tok.Num(); sx = cx; sy = cy;
                    sb.Append($"{F(cx)} {F(cy)} m\n");
                    cmd = rel ? 'l' : 'L'; // following pairs are line-tos
                    prev = 'M';
                    lcx = cx; lcy = cy;
                    continue;
                case 'L':
                {
                    double x = ox + tok.Num(), y = oy + tok.Num();
                    Seg(cx, cy, 0, 0, 0, 0, x, y, false); cx = x; cy = y;
                    break;
                }
                case 'H':
                {
                    double x = ox + tok.Num();
                    Seg(cx, cy, 0, 0, 0, 0, x, cy, false); cx = x;
                    break;
                }
                case 'V':
                {
                    double y = oy + tok.Num();
                    Seg(cx, cy, 0, 0, 0, 0, cx, y, false); cy = y;
                    break;
                }
                case 'C':
                {
                    double x1 = ox + tok.Num(), y1 = oy + tok.Num(), x2 = ox + tok.Num(), y2 = oy + tok.Num(), x = ox + tok.Num(), y = oy + tok.Num();
                    Seg(cx, cy, x1, y1, x2, y2, x, y, true); lcx = x2; lcy = y2; cx = x; cy = y;
                    prev = 'C';
                    continue;
                }
                case 'S':
                {
                    double x1 = prev == 'C' ? 2 * cx - lcx : cx, y1 = prev == 'C' ? 2 * cy - lcy : cy;
                    double x2 = ox + tok.Num(), y2 = oy + tok.Num(), x = ox + tok.Num(), y = oy + tok.Num();
                    Seg(cx, cy, x1, y1, x2, y2, x, y, true); lcx = x2; lcy = y2; cx = x; cy = y;
                    prev = 'C';
                    continue;
                }
                case 'Q':
                {
                    double qx = ox + tok.Num(), qy = oy + tok.Num(), x = ox + tok.Num(), y = oy + tok.Num();
                    Quad(qx, qy, x, y);
                    continue;
                }
                case 'T':
                {
                    double qx = prev == 'Q' ? 2 * cx - lcx : cx, qy = prev == 'Q' ? 2 * cy - lcy : cy;
                    double x = ox + tok.Num(), y = oy + tok.Num();
                    Quad(qx, qy, x, y);
                    continue;
                }
                case 'A':
                {
                    double rx = tok.Num(), ry = tok.Num(), rot = tok.Num();
                    bool large = tok.Flag(), sweep = tok.Flag();
                    double x = ox + tok.Num(), y = oy + tok.Num();
                    foreach (double[] b in Arc(cx, cy, rx, ry, rot, large, sweep, x, y))
                        Seg(b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7], true);
                    if (rx == 0 || ry == 0 || (cx == x && cy == y)) { if (cx != x || cy != y) Seg(cx, cy, 0, 0, 0, 0, x, y, false); }
                    cx = x; cy = y;
                    break;
                }
                case 'Z':
                    if (cx != sx || cy != sy) Seg(cx, cy, 0, 0, 0, 0, sx, sy, false);
                    sb.Append("h\n");
                    cx = sx; cy = sy;
                    prev = 'Z';
                    cmd = ' ';
                    continue;
                default:
                    tok.Skip();
                    continue;
            }
            prev = char.ToUpper(cmd);
            lcx = cx; lcy = cy;

            void Quad(double qx, double qy, double x, double y)
            {
                Seg(cx, cy, cx + 2.0 / 3 * (qx - cx), cy + 2.0 / 3 * (qy - cy), x + 2.0 / 3 * (qx - x), y + 2.0 / 3 * (qy - y), x, y, true);
                lcx = qx; lcy = qy; cx = x; cy = y; prev = 'Q';
            }
        }
        p.Ops = sb.ToString();
        return p;
    }

    /// <summary>Direction leaving (x0,y0) along a cubic, skipping coincident control points.</summary>
    static (double, double) Dir(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
    {
        const double eps = 1e-9;
        if (Math.Abs(x1 - x0) > eps || Math.Abs(y1 - y0) > eps) return (x1 - x0, y1 - y0);
        if (Math.Abs(x2 - x0) > eps || Math.Abs(y2 - y0) > eps) return (x2 - x0, y2 - y0);
        return (x3 - x0, y3 - y0);
    }

    /// <summary>SVG elliptical arc → cubic Béziers (endpoint to centre parameterisation, ≤ 90° pieces).</summary>
    static IEnumerable<double[]> Arc(double x1, double y1, double rx, double ry, double phiDeg, bool large, bool sweep, double x2, double y2)
    {
        if (rx == 0 || ry == 0 || (x1 == x2 && y1 == y2)) yield break;
        rx = Math.Abs(rx); ry = Math.Abs(ry);
        double phi = phiDeg * Math.PI / 180, cos = Math.Cos(phi), sin = Math.Sin(phi);
        double dx = (x1 - x2) / 2, dy = (y1 - y2) / 2;
        double x1p = cos * dx + sin * dy, y1p = -sin * dx + cos * dy;
        double lambda = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
        if (lambda > 1) { double s = Math.Sqrt(lambda); rx *= s; ry *= s; }
        double num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
        double den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
        double coef = Math.Sqrt(Math.Max(0, num / den)) * (large == sweep ? -1 : 1);
        double cxp = coef * rx * y1p / ry, cyp = -coef * ry * x1p / rx;
        double cx = cos * cxp - sin * cyp + (x1 + x2) / 2, cy = sin * cxp + cos * cyp + (y1 + y2) / 2;
        static double Ang(double ux, double uy, double vx, double vy)
        {
            double a = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
            return a;
        }
        double t1 = Ang(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
        double dt = Ang((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
        if (!sweep && dt > 0) dt -= 2 * Math.PI;
        if (sweep && dt < 0) dt += 2 * Math.PI;
        int n = Math.Max(1, (int)Math.Ceiling(Math.Abs(dt) / (Math.PI / 2) - 1e-9));
        double step = dt / n, k = 4.0 / 3 * Math.Tan(step / 4);
        (double, double) Pt(double t) { double ex = rx * Math.Cos(t), ey = ry * Math.Sin(t); return (cos * ex - sin * ey + cx, sin * ex + cos * ey + cy); }
        (double, double) Dv(double t) { double ex = -rx * Math.Sin(t), ey = ry * Math.Cos(t); return (cos * ex - sin * ey, sin * ex + cos * ey); }
        double t0 = t1;
        (double px, double py) = (x1, y1);
        for (int i = 0; i < n; i++)
        {
            double ta = t0 + i * step, tb = ta + step;
            (double ax, double ay) = i == 0 ? (x1, y1) : Pt(ta);
            (double bx, double by) = i == n - 1 ? (x2, y2) : Pt(tb);
            (double dax, double day) = Dv(ta);
            (double dbx, double dby) = Dv(tb);
            yield return new[] { ax, ay, ax + k * dax, ay + k * day, bx - k * dbx, by - k * dby, bx, by };
        }
    }
}
