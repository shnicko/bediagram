using System.Text.Json;

namespace BeDiagram.Models.Serialization;

/// <summary>
/// How an enum value is written in a diagram file: its member name in camelCase ("decision", "rework"), the same
/// names the file used when these were strings, so files read and write exactly as before.
/// </summary>
public static class EnumNames
{
    /// <summary>The saved name of <paramref name="value"/>, e.g. <c>NodeType.Decision</c> → "decision".</summary>
    public static string Of<T>(T value) where T : struct, Enum => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

    /// <summary>The value saved as <paramref name="name"/> (any case), or null for a missing or unknown name.</summary>
    public static T? Parse<T>(string? name) where T : struct, Enum =>
        name is not null && !int.TryParse(name, out _) && Enum.TryParse(name, true, out T value) && Enum.IsDefined(value) ? value : null;
}
