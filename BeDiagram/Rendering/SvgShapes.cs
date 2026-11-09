using System.Globalization;
using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;

namespace BeDiagram.Rendering;

/// <summary>SVG markup shared by the canvas and the SVG export: shape paths, the halo ring, the shadow, step and link badges, rounded rectangles. Keeping one copy keeps the two renderers in step.</summary>
public static class SvgShapes
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Soft shadow under each shape: the same outline moved down, in the shape's own colour, faint.
    /// Drawn as a plain shape (no SVG blur filter), so the PDF export and greyscale prints show it too.</summary>
    public const double ShadowDy = 4;
    /// <summary>Font size of the number in the step badge.</summary>
    public const double StepFontSize = 11.5;
    /// <summary>Radius of the round "has a more detailed diagram" badge.</summary>
    public const double BadgeR = 9;


    /// <summary>
    /// The halo ring around a major step, or null for other steps: the shape's own outline drawn <see cref="Boxes.HaloGap"/>
    /// further out, as a thin line. Connectors stop on the outside of the ring (<see cref="HaloOut"/>), so their arrowheads stay visible.
    /// </summary>
    public static string? HaloSvg(Node n, string colour)
    {
        if (!Emphases.Of(n).HasHalo) return null;
        AreaBox ring = new AreaBox(n.X - Boxes.HaloGap, n.Y - Boxes.HaloGap, n.W + 2 * Boxes.HaloGap, n.H + 2 * Boxes.HaloGap);
        return $"<path d=\"{n.Kind.OutlineOf(n).Path(ring)}\" fill=\"none\" stroke=\"{colour}\" stroke-width=\"{F(Boxes.HaloRing)}\"/>";
    }

    /// <summary>The SVG transform that moves a shape's outline to where its shadow is drawn.</summary>
    public static string ShadowTransform => $"translate(0,{F(ShadowDy)})";

    /// <summary>Radius of the step-number badge (a little wider for two-digit numbers).</summary>
    static double StepBadgeR(int step) => step >= 10 ? 11.5 : 10;

    /// <summary>The step number drawn on <paramref name="n"/>, or null when numbers are hidden or it has none (outcomes).</summary>
    public static int? ShownStep(Diagram d, Node n) => d.ShowSteps && n.Kind.IsStep ? n.Step : null;

    /// <summary>SVG markup for the step badge: a disc in the shape's colour with a white ring and a white number, at
    /// the top-left of the drawn outline (see <see cref="IShapeOutline.StepBadgeCentre"/>). Used by the canvas and the export.</summary>
    public static string StepBadgeSvg(Node n, int step, string colour)
    {
        (double cx, double cy) = n.Kind.OutlineOf(n).StepBadgeCentre(n);
        return $"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(StepBadgeR(step))}\" fill=\"{colour}\" stroke=\"#ffffff\" stroke-width=\"2\"/>"
             + $"<text x=\"{F(cx)}\" y=\"{F(cy + StepFontSize * 0.36)}\" font-size=\"{F(StepFontSize)}\" font-weight=\"700\" text-anchor=\"middle\" fill=\"#ffffff\">{step}</text>";
    }

    /// <summary>True when the item links to another diagram.</summary>
    public static bool HasLink(IBox b) => !string.IsNullOrWhiteSpace(b switch { Node n => n.Link, Group g => g.Link, _ => null });

    /// <summary>
    /// SVG markup for the link badge (white disc, coloured ring, Lucide "link" glyph). Used by canvas and export.
    /// It sits at the bottom-right of the drawn outline (see <see cref="IShapeOutline.LinkBadgeCentre"/>: on the curve
    /// for circles, seals and octagons, on the lower-right edge for diamonds), or inside the bottom-right corner of a group.
    /// </summary>
    public static string LinkBadgeSvg(IBox b, string colour)
    {
        (double cx, double cy) = b is Node n ? n.Kind.OutlineOf(n).LinkBadgeCentre(n) : (b.X + b.W - 18, b.Y + b.H - 18);
        const double glyph = 12;
        return $"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(BadgeR)}\" fill=\"#ffffff\" stroke=\"{colour}\" stroke-width=\"1.4\"/>"
             + $"<g transform=\"translate({F(cx - glyph / 2)},{F(cy - glyph / 2)}) scale({F(glyph / 24)})\" fill=\"none\" stroke=\"{colour}\" stroke-width=\"2.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get("link")}</g>";
    }

    public static string ShapePath(Node n) => n.Kind.OutlineOf(n).Path(n);

    /// <summary>Outline of a text box (square or slightly rounded rectangle).</summary>
    public static string TextBoxPath(TextBox t, double radius) =>
        radius > 0 ? Paths.RoundRect(t.X, t.Y, t.W, t.H, radius) : $"M{F(t.X)},{F(t.Y)} H{F(t.X + t.W)} V{F(t.Y + t.H)} H{F(t.X)} Z";

    /// <summary>The small diagonal "folded corner" drawn on comments.</summary>
    public static string FoldPath(TextBox t) => $"M{F(t.X + t.W - 14)},{F(t.Y + t.H)} L{F(t.X + t.W)},{F(t.Y + t.H - 14)}";
}
