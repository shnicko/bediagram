namespace BeDiagram.Models;

/// <summary>Fixed page for diagrams that go into documents. The page sits at (0,0) on the canvas.</summary>
public class PageSetup
{
    public const string None = "none";
    public const string Custom = "custom";

    public string Size { get; set; } = None;
    public string Orientation { get; set; } = "landscape";
    /// <summary>Custom size in millimetres (portrait values; orientation swaps them).</summary>
    public double CustomW { get; set; } = 210;
    public double CustomH { get; set; } = 297;
    public double MarginMm { get; set; } = 10;
    /// <summary>On export, scale the drawing down if it doesn't fit inside the margins.</summary>
    public bool FitOnExport { get; set; } = true;

    public bool Enabled => Size != None;
    public bool Landscape => Orientation == "landscape";

    public const double PxPerMm = 96 / 25.4;

    /// <summary>Common sizes, portrait width × height in mm.</summary>
    public static readonly IReadOnlyList<(string Key, string Name, double W, double H)> Sizes = new List<(string, string, double, double)>
    {
        ("A5", "A5", 148, 210),
        ("A4", "A4", 210, 297),
        ("A3", "A3", 297, 420),
        ("A2", "A2", 420, 594),
        ("A1", "A1", 594, 841),
        ("A0", "A0", 841, 1189),
        ("Letter", "US Letter", 215.9, 279.4),
        ("Legal", "US Legal", 215.9, 355.6),
        ("Tabloid", "Tabloid / Ledger", 279.4, 431.8),
        ("Slide169", "Slide 16:9", 190.5, 338.67),
        ("Slide43", "Slide 4:3", 190.5, 254),
    };

    /// <summary>Page size in mm after orientation.</summary>
    public (double W, double H) Mm
    {
        get
        {
            double w, h;
            if (Size == Custom) { w = Math.Max(20, CustomW); h = Math.Max(20, CustomH); }
            else
            {
                (string Key, string Name, double W, double H) s = Sizes.FirstOrDefault(x => x.Key == Size);
                (w, h) = s.Key is null ? (210, 297) : (s.W, s.H);
                // Stored portrait; landscape swaps. (For custom sizes the user's numbers are used as typed.)
                (w, h) = (Math.Min(w, h), Math.Max(w, h));
                if (Landscape) (w, h) = (h, w);
            }
            return (w, h);
        }
    }

    public double WidthPx => Mm.W * PxPerMm;
    public double HeightPx => Mm.H * PxPerMm;
    public double MarginPx => Math.Max(0, MarginMm) * PxPerMm;

    public string Describe()
    {
        if (!Enabled) return "Open canvas";
        (double w, double h) = Mm;
        string name = Size == Custom ? "Custom" : Sizes.First(x => x.Key == Size).Name;
        string orient = Size == Custom ? "" : (Landscape ? " · Landscape" : " · Portrait");
        return $"{name}{orient} · {w:0.#} × {h:0.#} mm";
    }
}
