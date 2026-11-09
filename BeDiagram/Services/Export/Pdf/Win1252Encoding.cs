using System.Text;

namespace BeDiagram.Services.Export.Pdf;

/// <summary>Windows-1252 encoder that doesn't need the CodePages provider; unknown characters become '?'.</summary>
internal sealed class Win1252Encoding : Encoding
{
    public static readonly Win1252Encoding Instance = new();
    static readonly Dictionary<char, byte> High = new()
    {
        ['€'] = 0x80, ['‚'] = 0x82, ['ƒ'] = 0x83, ['„'] = 0x84, ['…'] = 0x85, ['†'] = 0x86, ['‡'] = 0x87, ['ˆ'] = 0x88,
        ['‰'] = 0x89, ['Š'] = 0x8A, ['‹'] = 0x8B, ['Œ'] = 0x8C, ['Ž'] = 0x8E, ['‘'] = 0x91, ['’'] = 0x92, ['“'] = 0x93,
        ['”'] = 0x94, ['•'] = 0x95, ['–'] = 0x96, ['—'] = 0x97, ['˜'] = 0x98, ['™'] = 0x99, ['š'] = 0x9A, ['›'] = 0x9B,
        ['œ'] = 0x9C, ['ž'] = 0x9E, ['Ÿ'] = 0x9F,
        ['→'] = (byte)'>', ['←'] = (byte)'<', ['✓'] = (byte)'v', ['✕'] = (byte)'x', ['×'] = 0xD7,
    };
    public override int GetByteCount(char[] chars, int index, int count) => count;
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        for (int k = 0; k < charCount; k++)
        {
            char ch = chars[charIndex + k];
            bytes[byteIndex + k] = ch < 0x80 || (ch >= 0xA0 && ch <= 0xFF) ? (byte)ch
                : High.TryGetValue(ch, out byte b) ? b : (byte)'?';
        }
        return charCount;
    }
    public override int GetCharCount(byte[] bytes, int index, int count) => count;
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        for (int k = 0; k < byteCount; k++) chars[charIndex + k] = (char)bytes[byteIndex + k];
        return byteCount;
    }
    public override int GetMaxByteCount(int charCount) => charCount;
    public override int GetMaxCharCount(int byteCount) => byteCount;
}
