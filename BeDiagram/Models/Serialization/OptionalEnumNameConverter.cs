using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeDiagram.Models.Serialization;

/// <summary>
/// For an optional enum property (<c>Side?</c>, <c>OutcomeType?</c>): by name like <see cref="EnumNameConverter{T}"/>,
/// but a missing or unknown name reads as null (e.g. a connector side that isn't one of the four is "automatic").
/// Put it on the property: <c>[JsonConverter(typeof(OptionalEnumNameConverter&lt;Side&gt;))]</c>.
/// </summary>
public class OptionalEnumNameConverter<T> : JsonConverter<T?> where T : struct, Enum
{
    public override bool HandleNull => true;

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String ? EnumNames.Parse<T>(reader.GetString()) : null;

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value is { } v) writer.WriteStringValue(EnumNames.Of(v));
        else writer.WriteNullValue();
    }
}
