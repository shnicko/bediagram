using System.Text.Json.Serialization;

namespace BeDiagram.Models;

/// <summary>
/// A diagram document. Saved to disk as JSON (*.diagram.json).
/// Nodes are typed (see <see cref="NodeTypes"/>) and carry free-form properties so that a
/// future audit-rule builder can interpret the same file without a format change.
/// </summary>
public class Diagram : IJsonOnDeserialized
{
    public string Schema { get; set; } = "bediagram/1";
    public string Name { get; set; } = "Untitled";
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    /// <summary>Style given to new connectors in this diagram.</summary>
    public string DefaultConnector { get; set; } = EdgeStyles.Elbow;
    /// <summary>Look (weight, pattern, colour, arrows, label direction) given to new connectors.</summary>
    public Edge DefaultLook { get; set; } = new();
    /// <summary>Optional fixed page (paper size). Size "none" = open canvas.</summary>
    public PageSetup Page { get; set; } = new();
    public List<Group> Groups { get; set; } = new();
    public List<Node> Nodes { get; set; } = new();
    public List<Edge> Edges { get; set; } = new();
    public List<TextBox> TextBoxes { get; set; } = new();

    /// <summary>Read-only shim: files saved before text boxes had a "comments" list. They load as comment text boxes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LegacyComment>? Comments { get; set; }

    void IJsonOnDeserialized.OnDeserialized()
    {
        if (Comments is null) return;
        foreach (var c in Comments)
        {
            var t = TextBox.NewComment();
            t.Id = c.Id; t.Text = c.Text; t.X = c.X; t.Y = c.Y; t.W = c.W; t.H = c.H; t.GroupId = c.GroupId;
            t.Fill = t.BorderColor = c.Color switch { "blue" => "sky", "green" => "mint", "pink" => "rose", _ => "yellow" };
            TextBoxes.Add(t);
            foreach (var e in Edges.Where(e => e.From == c.Id || e.To == c.Id)) TextBox.ApplyPinLook(e);
        }
        Comments = null;
    }
}

public class LegacyComment
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public string Color { get; set; } = "yellow";
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 200;
    public double H { get; set; } = 110;
    public string? GroupId { get; set; }
}

public interface IBox
{
    string Id { get; }
    double X { get; set; }
    double Y { get; set; }
    double W { get; set; }
    double H { get; set; }
}

/// <summary>Items that can sit inside a group.</summary>
public interface IGroupable : IBox
{
    string? GroupId { get; set; }
}

public class Node : IGroupable
{
    public string Id { get; set; } = NewId("n");
    public string Type { get; set; } = NodeTypes.Process;
    public string Label { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 160;
    public double H { get; set; } = 64;
    public string? GroupId { get; set; }
    /// <summary>Optional Lucide icon name (see <see cref="Icons"/>). Circles show it centred; other shapes show it beside the label.</summary>
    public string? Icon { get; set; }
    /// <summary>Optional colour key from <see cref="Palette"/>; null = the type's default colours.</summary>
    public string? Color { get; set; }
    /// <summary>Solid fill with white text/icon (presentation style).</summary>
    public bool Filled { get; set; }
    /// <summary>Outcome nodes only: "pass", "fail", "rework", "conditional" or "hold" (see <see cref="Outcomes"/>).
    /// This carries the meaning; the shape and glyph are just how it is drawn.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Outcome { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    /// <summary>Name of another saved diagram (without extension) holding this step's more detailed flow. Shows a link badge.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }

    public static string NewId(string prefix) => $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";
}

public class Edge
{
    public string Id { get; set; } = Node.NewId("e");
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>"elbow" (right-angled, default) or "straight".</summary>
    public string Style { get; set; } = EdgeStyles.Elbow;
    /// <summary>"horizontal" (always level), "along" (follows the line) or "perpendicular" (90° to the line).</summary>
    public string LabelOrient { get; set; } = LabelOrients.Horizontal;
    /// <summary>Label position along the connector, 0 = start, 1 = end.</summary>
    public double LabelPos { get; set; } = 0.5;
    /// <summary>"light", "regular" or "heavy".</summary>
    public string Weight { get; set; } = LineLook.Regular;
    /// <summary>"solid", "dashed", "dotted", "dashdot" or "longdash".</summary>
    public string Pattern { get; set; } = LineLook.Solid;
    /// <summary>Palette colour key, or null for the default grey.</summary>
    public string? Color { get; set; }
    /// <summary>"end", "both" or "none".</summary>
    public string Arrows { get; set; } = LineLook.ArrowEnd;

    /// <summary>Side of the source shape the connector leaves from ("top", "right", "bottom", "left"); null = automatic.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FromSide { get; set; }
    /// <summary>Side of the target shape the connector arrives at; null = automatic.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToSide { get; set; }
    /// <summary>Elbow connectors only: how far (px) the middle segment has been dragged from its automatic position.
    /// Horizontal segments move in y, vertical ones in x. 0 = automatic.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double Bend { get; set; }

    public bool IsManual => FromSide is not null || ToSide is not null || Bend != 0;

    /// <summary>Back to fully automatic sides and bend.</summary>
    public void ResetRoute() { FromSide = null; ToSide = null; Bend = 0; }

    public void CopyRouteFrom(Edge o) { FromSide = o.FromSide; ToSide = o.ToSide; Bend = o.Bend; }

    public void CopyLookFrom(Edge o) { Style = o.Style; Weight = o.Weight; Pattern = o.Pattern; Color = o.Color; Arrows = o.Arrows; LabelOrient = o.LabelOrient; }
}

/// <summary>Line weight, dash pattern, colour and arrowheads for connectors.</summary>
public static class LineLook
{
    public const string Light = "light", Regular = "regular", Heavy = "heavy";
    public const string Solid = "solid", Dashed = "dashed", Dotted = "dotted", DashDot = "dashdot", LongDash = "longdash";
    public const string ArrowEnd = "end", ArrowBoth = "both", ArrowNone = "none";
    public const string DefaultColor = "#55606e";

    public static readonly (string Key, string Name)[] Weights = { (Light, "Light"), (Regular, "Regular"), (Heavy, "Heavy") };
    public static readonly (string Key, string Name)[] Patterns =
        { (Solid, "Solid"), (Dashed, "Dashed"), (Dotted, "Dotted"), (DashDot, "Dash-dot"), (LongDash, "Long dash") };
    public static readonly (string Key, string Name)[] ArrowModes = { (ArrowEnd, "End"), (ArrowBoth, "Both"), (ArrowNone, "None") };

    public static double Width(string weight) => weight switch { Light => 1, Heavy => 3, _ => 1.6 };

    public static string? Dash(string pattern, double w) => pattern switch
    {
        Dashed => $"{N(5 + 2 * w)} {N(3 + 1.5 * w)}",
        Dotted => $"0 {N(2 + 2 * w)}",
        DashDot => $"{N(7 + 2 * w)} {N(3 + 1.5 * w)} 0 {N(3 + 1.5 * w)}",
        LongDash => $"{N(12 + 3 * w)} {N(4 + 2 * w)}",
        _ => null,
    };

    /// <summary>Round caps turn zero-length dashes into dots.</summary>
    public static string Cap(string pattern) => pattern is Dotted or DashDot ? "round" : "butt";

    public static string Colour(string? key) =>
        key is not null && Palette.All.TryGetValue(key, out var sw) ? sw.Stroke : TextPalette.StrokeOf(key, DefaultColor);

    /// <summary>Marker id for an arrowhead in a given colour and weight (one marker per combination).</summary>
    public static string MarkerId(string colour, string weight) => $"arr-{colour.TrimStart('#')}-{weight}";

    /// <summary>Arrowhead marker sized in user units so heavy lines don't get giant arrows.</summary>
    public static string MarkerDef(string colour, string weight)
    {
        var size = weight switch { Light => 8, Heavy => 13, _ => 10 };
        return $"<marker id=\"{MarkerId(colour, weight)}\" viewBox=\"0 0 10 10\" refX=\"9\" refY=\"5\" markerUnits=\"userSpaceOnUse\" markerWidth=\"{size}\" markerHeight=\"{size}\" orient=\"auto-start-reverse\"><path d=\"M0,0 L10,5 L0,10 z\" fill=\"{colour}\"/></marker>";
    }

    static string N(double v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}

public static class EdgeStyles
{
    public const string Elbow = "elbow";
    public const string Straight = "straight";
}

/// <summary>Connection points on a shape: the middle of each side.</summary>
public static class Sides
{
    public const string Top = "top", Right = "right", Bottom = "bottom", Left = "left";
    public static readonly string[] All = { Top, Right, Bottom, Left };

    /// <summary>Outward direction of a side.</summary>
    public static (double X, double Y) Dir(string side) => side switch
    {
        Top => (0, -1), Right => (1, 0), Bottom => (0, 1), _ => (-1, 0),
    };

    public static string Opposite(string side) => side switch
    {
        Top => Bottom, Right => Left, Bottom => Top, _ => Right,
    };

    public static bool IsValid(string? side) => side is Top or Right or Bottom or Left;
}

public static class LabelOrients
{
    public const string Horizontal = "horizontal";
    public const string Along = "along";
    public const string Perpendicular = "perpendicular";
}

/// <summary>
/// Free text: multi-line, with optional background, border and font settings. Text boxes can be
/// connected like shapes. <see cref="IsComment"/> flags annotations (sticky-note look, dashed pin lines)
/// so code reading the file can tell notes apart from text that is part of the diagram.
/// </summary>
public class TextBox : IGroupable
{
    public string Id { get; set; } = Node.NewId("t");
    public string Text { get; set; } = "";
    /// <summary>An annotation rather than diagram content.</summary>
    public bool IsComment { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 200;
    public double H { get; set; } = 60;
    public string? GroupId { get; set; }

    /// <summary>Background colour key from <see cref="TextPalette"/>; null = transparent.</summary>
    public string? Fill { get; set; }
    /// <summary>"none" or a line pattern from <see cref="LineLook"/> (solid, dashed, dotted, dashdot, longdash).</summary>
    public string Border { get; set; } = TextLook.NoBorder;
    /// <summary>"light", "regular" or "heavy".</summary>
    public string BorderWeight { get; set; } = LineLook.Regular;
    /// <summary>Border colour key from <see cref="TextPalette"/>; null = default grey.</summary>
    public string? BorderColor { get; set; }
    public bool Rounded { get; set; } = true;

    /// <summary>"sans", "serif" or "mono".</summary>
    public string Font { get; set; } = TextLook.Sans;
    public double FontSize { get; set; } = 14;
    public bool Bold { get; set; }
    /// <summary>Text colour key from <see cref="TextPalette"/>; null = default dark.</summary>
    public string? TextColor { get; set; }
    /// <summary>"left", "center" or "right".</summary>
    public string Align { get; set; } = TextLook.Left;
    /// <summary>"top", "middle" or "bottom".</summary>
    public string VAlign { get; set; } = TextLook.Top;

    public TextBox Copy() { var c = (TextBox)MemberwiseClone(); c.Id = Node.NewId("t"); return c; }

    /// <summary>The standard comment look: yellow sticky note, thin border, Consolas, folded corner.</summary>
    public void ApplyCommentStyle()
    {
        IsComment = true;
        Fill = "yellow"; Border = LineLook.Solid; BorderWeight = LineLook.Light; BorderColor = "yellow"; Rounded = true;
        Font = TextLook.Mono; FontSize = 12; Bold = false; TextColor = "ink";
        Align = TextLook.Left; VAlign = TextLook.Top;
    }

    public static TextBox NewComment()
    {
        var t = new TextBox { W = 200, H = 110 };
        t.ApplyCommentStyle();
        return t;
    }

    /// <summary>Connector look for lines pinned to a comment: light, dashed, no arrows, straight.</summary>
    public static void ApplyPinLook(Edge e)
    {
        e.Style = EdgeStyles.Straight; e.Weight = LineLook.Light; e.Pattern = LineLook.Dashed;
        e.Color = "grey"; e.Arrows = LineLook.ArrowNone;
    }
}

public static class TextLook
{
    public const string NoBorder = "none";
    public const string Sans = "sans", Serif = "serif", Mono = "mono";
    public const string Left = "left", Center = "center", Right = "right";
    public const string Top = "top", Middle = "middle", Bottom = "bottom";

    public static readonly (string Key, string Name)[] Fonts = { (Sans, "Sans"), (Serif, "Serif"), (Mono, "Mono") };
    public static readonly double[] Sizes = { 10, 11, 12, 13, 14, 16, 18, 20, 24, 28, 32, 40 };

    public static string Family(string font) => font switch
    {
        Mono => "Consolas, 'Cascadia Mono', 'Courier New', monospace",
        Serif => "Georgia, 'Times New Roman', serif",
        _ => "'Segoe UI', Arial, sans-serif",
    };

    /// <summary>Average character width as a fraction of the font size (used for wrapping).</summary>
    public static double CharWidth(string font) => font switch { Mono => 0.56, Serif => 0.52, _ => 0.54 };

    public const double LineSpacing = 1.35;
    public const double PadX = 10, PadY = 8;
}

/// <summary>
/// Colours for text boxes. Fill = background, Stroke = border and text. Includes the sticky-note
/// colours, white, greys and the shared shape <see cref="Palette"/>.
/// </summary>
public static class TextPalette
{
    public static readonly IReadOnlyDictionary<string, (string Fill, string Stroke)> All = BuildAll();

    static Dictionary<string, (string, string)> BuildAll()
    {
        var d = new Dictionary<string, (string, string)>
        {
            ["white"] = ("#ffffff", "#cfd4da"),
            ["grey"] = ("#f1f3f5", "#9aa3ad"),
            ["ink"] = ("#3d3a2a", "#3d3a2a"),
            ["yellow"] = ("#fff7c2", "#d9c255"),
            ["sky"] = ("#dcecff", "#7ea6d8"),
            ["mint"] = ("#dcf5df", "#7fbf88"),
            ["rose"] = ("#fde0ea", "#d88aa6"),
        };
        foreach (var (k, s) in Palette.All) d[k] = (s.Light, s.Stroke);
        return d;
    }

    /// <summary>Swatches offered for backgrounds (pale colours).</summary>
    public static readonly string[] Fills = { "white", "grey", "yellow", "sky", "mint", "rose", "blue", "teal", "green", "amber", "red", "pink", "purple", "slate" };
    /// <summary>Swatches offered for borders and text (strong colours).</summary>
    public static readonly string[] Strokes = { "grey", "yellow", "sky", "mint", "rose", "blue", "teal", "green", "amber", "red", "pink", "purple", "slate", "ink" };

    public static string? FillOf(string? key) => key is not null && All.TryGetValue(key, out var c) ? c.Fill : null;
    public static string StrokeOf(string? key, string fallback) => key is not null && All.TryGetValue(key, out var c) ? c.Stroke : fallback;
}

/// <summary>Resolved colours and lines for drawing a text box (canvas and export).</summary>
public record TextStyle(string? Fill, string? Stroke, double StrokeWidth, string? Dash, string Cap, double Radius,
                        string Family, string TextColour, string FoldColour)
{
    public const string DefaultBorder = "#9aa3ad";
    public const string DefaultText = "#1f2933";

    public static TextStyle For(TextBox t)
    {
        var hasBorder = t.Border != TextLook.NoBorder;
        var w = LineLook.Width(t.BorderWeight);
        var stroke = TextPalette.StrokeOf(t.BorderColor, DefaultBorder);
        // Folded corner follows the border colour, or the background's own edge colour when there is no border.
        var fold = hasBorder ? stroke : TextPalette.StrokeOf(t.Fill, DefaultBorder);
        return new TextStyle(
            TextPalette.FillOf(t.Fill),
            hasBorder ? stroke : null,
            w,
            hasBorder ? LineLook.Dash(t.Border, w) : null,
            LineLook.Cap(t.Border),
            t.Rounded ? 4 : 0,
            TextLook.Family(t.Font),
            TextPalette.StrokeOf(t.TextColor, DefaultText),
            fold);
    }
}

/// <summary>A coloured container. Members are the nodes/comments whose GroupId points here.</summary>
public class Group : IBox
{
    public const double Header = 32;
    public const double Pad = 20;

    public string Id { get; set; } = Node.NewId("g");
    public string Title { get; set; } = "Group";
    public string Color { get; set; } = "blue";
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 360;
    public double H { get; set; } = 220;
    /// <summary>Name of another saved diagram (without extension) holding this group's more detailed flow. Shows a link badge.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }
}

public record NodeTypeInfo(string Key, string Display, string Shape, string Fill, string Stroke, double W, double H);

public static class NodeTypes
{
    public const string Start = "start";
    public const string End = "end";
    public const string Process = "process";
    public const string Decision = "decision";
    public const string Check = "check";
    public const string Action = "action";
    public const string Data = "data";
    public const string Status = "status";
    public const string Circle = "circle";
    public const string Outcome = "outcome";

    public static readonly IReadOnlyList<NodeTypeInfo> All = new List<NodeTypeInfo>
    {
        new(Start,    "Start",         "pill",          "#e7f5ec", "#2f8f5b", 140, 52),
        new(Process,  "Process",       "rect",          "#eef3fb", "#3b6fb6", 160, 64),
        new(Decision, "Decision",      "diamond",       "#fff6e5", "#c58a12", 170, 96),
        new(Check,    "Rule check",    "check",         "#f1ecfb", "#6a4bb5", 170, 64),
        new(Action,   "Action",        "rect-rounded",  "#e8f6f8", "#1f8796", 160, 64),
        new(Data,     "Data / IFC",    "parallelogram", "#f4f4f2", "#6b6b66", 170, 64),
        new(Status,   "Status",        "hexagon",       "#fdeef0", "#b8475a", 150, 56),
        new(End,      "End",           "pill",          "#fbe9e7", "#b4472f", 140, 52),
        new(Circle,   "Circle",        "circle",        "#eef3fb", "#3b6fb6", 84, 84),
        new(Outcome,  "Outcome",       "outcome",       "#e7f5ec", "#2f8f5b", 50, 50),
    };

    public static NodeTypeInfo Get(string key) => All.FirstOrDefault(t => t.Key == key) ?? All[1];
}

/// <summary>
/// One result of a review or check. The outline differs per outcome (not just the colour), so a sheet printed
/// in greyscale or read by a colour-blind reviewer still tells them apart.
/// </summary>
public record OutcomeInfo(string Key, string Display, string Shape, string Glyph, string Color, string Hint);

public static class Outcomes
{
    public const string Pass = "pass", Fail = "fail", Rework = "rework", Conditional = "conditional", Hold = "hold";

    public static readonly IReadOnlyList<OutcomeInfo> All = new List<OutcomeInfo>
    {
        new(Pass,        "Pass",        "seal",           "outcome-tick",    "green", "Accepted / approved"),
        new(Fail,        "Fail",        "octagon",        "outcome-cross",   "red",   "Rejected — stops here"),
        new(Rework,      "Rework",      "circle",         "outcome-rework",  "pink",  "Returned to the author for rework"),
        new(Conditional, "Conditional", "triangle",       "outcome-exclaim", "amber", "Pass with comments / approved subject to changes"),
        new(Hold,        "Hold",        "square-rounded", "outcome-clock",   "slate", "On hold / awaiting review"),
    };

    public static OutcomeInfo Get(string? key) => All.FirstOrDefault(o => o.Key == key) ?? All[0];
}

/// <summary>Shared colour set for shapes and groups. Light = pale fill, Stroke = outline/icon, Solid = filled style.</summary>
public static class Palette
{
    public record Swatch(string Light, string Stroke, string Solid);

    public static readonly IReadOnlyDictionary<string, Swatch> All = new Dictionary<string, Swatch>
    {
        ["blue"] = new("#eef3fb", "#3b6fb6", "#3b6fb6"),
        ["teal"] = new("#e6f5f5", "#1f8796", "#1f8796"),
        ["green"] = new("#e7f5ec", "#2f8f5b", "#2f8f5b"),
        ["amber"] = new("#fff6e5", "#c58a12", "#d9930d"),
        ["red"] = new("#fbe9e7", "#b4472f", "#c4513a"),
        ["pink"] = new("#fdeef4", "#b8477a", "#c45086"),
        ["purple"] = new("#f1ecfb", "#6a4bb5", "#6a4bb5"),
        ["slate"] = new("#f1f3f5", "#55606e", "#4a5563"),
    };

    public static Swatch Get(string? key) => key is not null && All.TryGetValue(key, out var s) ? s : All["blue"];
}

/// <summary>Resolved colours for drawing a node.</summary>
public record NodeStyle(string Fill, string Stroke, string Text, string IconColor)
{
    public static NodeStyle For(Node n)
    {
        var t = NodeTypes.Get(n.Type);
        string fill = t.Fill, stroke = t.Stroke;
        if (n.Type == NodeTypes.Outcome)
        {
            // Outcomes default to their own colour (green pass, red fail, ...).
            var os = Palette.Get(Outcomes.Get(n.Outcome).Color);
            fill = os.Light; stroke = os.Stroke;
            // Conditional uses a dark mark on amber, like a warning sign: white on amber is too faint in greyscale.
            if (n.Color is null && n.Filled)
                return new NodeStyle(os.Solid, os.Solid, "#ffffff", n.Outcome == Outcomes.Conditional ? "#1f2933" : "#ffffff");
        }
        if (n.Color is not null && Palette.All.TryGetValue(n.Color, out var sw))
        {
            fill = sw.Light; stroke = sw.Stroke;
            if (n.Filled) return new NodeStyle(sw.Solid, sw.Solid, "#ffffff", "#ffffff");
        }
        else if (n.Filled)
        {
            return new NodeStyle(stroke, stroke, "#ffffff", "#ffffff");
        }
        return new NodeStyle(fill, stroke, "#1f2933", stroke);
    }
}

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
                var s = Sizes.FirstOrDefault(x => x.Key == Size);
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
        var (w, h) = Mm;
        var name = Size == Custom ? "Custom" : Sizes.First(x => x.Key == Size).Name;
        var orient = Size == Custom ? "" : (Landscape ? " · Landscape" : " · Portrait");
        return $"{name}{orient} · {w:0.#} × {h:0.#} mm";
    }
}
