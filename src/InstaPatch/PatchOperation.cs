using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using InstaPatch.Converters;

namespace InstaPatch;

/// <summary>
/// Represents a single patch operation.
/// </summary>
[ExcludeFromCodeCoverage]
public class PatchOperation
{
    /// <summary>
    /// Indicates the type of operation to perform.
    /// </summary>
    [JsonConverter(typeof(OperationTypeConverter))]
    public OperationType Op { get; set; }

    /// <summary>
    /// The JSON-pointer path to the target location.
    /// </summary>
    public string Path { get; set; } = null!;

    /// <summary>
    /// The value to be used in the operation.
    /// </summary>
    /// <remarks>
    /// This property is optional and may be null for operations that do not require a value.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; set; }

    /// <summary>
    /// The JSON-pointer path to the source location.
    /// </summary>
    /// <remarks>
    /// This property is optional and may be null for operations that do not require a source.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; set; }
}
