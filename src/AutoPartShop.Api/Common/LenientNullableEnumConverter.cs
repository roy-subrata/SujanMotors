using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoPartShop.Api.Common;

/// <summary>
/// List/filter DTOs expose nullable enum fields (e.g. Status) whose "no filter" sentinel on the
/// frontend is an empty string. The default JsonStringEnumConverter throws on "" for a nullable
/// enum, turning every "All Statuses" request into a 400. Reads "" (and null) as no value instead
/// of throwing; writes fall back to the default enum converter.
/// </summary>
public class LenientNullableEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert);
        return underlying is not null && underlying.IsEnum;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var enumType = Nullable.GetUnderlyingType(typeToConvert)!;
        var converterType = typeof(LenientNullableEnumConverter<>).MakeGenericType(enumType);
        return (JsonConverter?)Activator.CreateInstance(converterType, options);
    }

    private class LenientNullableEnumConverter<TEnum>(JsonSerializerOptions options) : JsonConverter<TEnum?>
        where TEnum : struct, Enum
    {
        private readonly JsonConverter<TEnum> _innerConverter =
            (JsonConverter<TEnum>)new JsonStringEnumConverter().CreateConverter(typeof(TEnum), options)!;

        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String && string.IsNullOrWhiteSpace(reader.GetString()))
                return null;

            return _innerConverter.Read(ref reader, typeof(TEnum), options);
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                _innerConverter.Write(writer, value.Value, options);
        }
    }
}
