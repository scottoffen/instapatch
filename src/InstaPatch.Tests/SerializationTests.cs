using System.Text.Json;

namespace InstaPatch.Tests;

public class SerializationTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void PatchOperation_Serializes_OperationType_AsString()
    {
        var operation = new PatchOperation
        {
            Op = OperationType.Replace,
            Path = "/phone",
            Value = "value"
        };

        var json = JsonSerializer.Serialize(operation, _options);
        json.ShouldBe("{\"op\":\"replace\",\"path\":\"/phone\",\"value\":\"value\"}");
    }

    [Fact]
    public void PatchOperation_Deserializes_OperationType_AsString()
    {
        var json = "{\"op\":\"replace\",\"path\":\"/phone\",\"value\":\"value\"}";

        var operation = JsonSerializer.Deserialize<PatchOperation>(json, _options);
        operation.ShouldNotBeNull();

        operation.ShouldSatisfyAllConditions(
            () => operation!.Op.ShouldBe(OperationType.Replace),
            () => operation.Path.ShouldBe("/phone"),
            () => operation.Value?.ToString().ShouldBe("value")
        );
    }
}
