using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeDiagram.Models.Serialization;

/// <summary>
/// Reads and writes an enum by name (see <see cref="EnumNames"/>). An unknown name doesn't stop the file opening: it
/// reads as the enum's first member (its default), which each enum lists as its fallback.
/// </summary>
public class EnumNameConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String ? EnumNames.Parse<T>(reader.GetString()) ?? default : default;

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(EnumNames.Of(value));
}
