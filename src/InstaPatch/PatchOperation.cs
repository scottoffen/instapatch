using System.Text.Json.Serialization;

namespace InstaPatch;

/// <summary>
/// Represents a single patch operation.
/// </summary>
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
    public string? Value { get; set; }

    /// <summary>
    /// The JSON-pointer path to the source location.
    /// </summary>
    /// <remarks>
    /// This property is optional and may be null for operations that do not require a source.
    /// </remarks>
    public string? From { get; set; }
}
