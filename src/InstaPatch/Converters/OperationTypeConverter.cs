using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstaPatch.Converters;

internal class OperationTypeConverter : JsonConverter<OperationType>
{
    public override OperationType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (Enum.TryParse<OperationType>(value, true, out var operationType))
        {
            return operationType;
        }

        throw new JsonException($"Invalid operation type: {value}");
    }

    public override void Write(Utf8JsonWriter writer, OperationType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}
