using System.Globalization;
using static BeDiagram.Services.Export.Pdf.SvgValues;

namespace BeDiagram.Services.Export.Pdf;

/// <summary>Reads SVG path data and number lists: commands, numbers (with exponents) and arc flags.</summary>
internal sealed class SvgPathTokens
{
    readonly string s;
    int i;
    public SvgPathTokens(string s) { this.s = s; }
    void Ws() { while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++; }
    public bool More() { Ws(); return i < s.Length; }
    public char? PeekCommand() { Ws(); return i < s.Length && char.IsLetter(s[i]) && s[i] != 'e' && s[i] != 'E' ? s[i] : null; }
    public void Skip() => i++;
    public bool Flag() { Ws(); if (i < s.Length && (s[i] == '0' || s[i] == '1')) return s[i++] == '1'; return Num() != 0; }
    public double Num()
    {
        Ws();
        int start = i;
        if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
        bool dot = false;
        while (i < s.Length && (char.IsDigit(s[i]) || (s[i] == '.' && !dot)))
        {
            if (s[i] == '.') dot = true;
            i++;
        }
        if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
        {
            int save = i++;
            if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
            if (i < s.Length && char.IsDigit(s[i])) while (i < s.Length && char.IsDigit(s[i])) i++;
            else i = save;
        }
        if (i == start) { i++; return 0; } // junk: skip a character rather than loop forever
        return double.TryParse(s.AsSpan(start, i - start), NumberStyles.Float, Inv, out double v) ? v : 0;
    }
}
