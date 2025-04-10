using System.Diagnostics.CodeAnalysis;

namespace InstaPatch;

/// <summary>
/// Represents the result of a patch operation.
/// </summary>
[ExcludeFromCodeCoverage]
public class PatchExecutionResult : PatchOperation
{
    /// <summary>
    /// Creates a new instance of <see cref="PatchExecutionResult"/> with the given operation.
    /// </summary>
    /// <remarks>
    /// This constructor is used when the patch operation is successful.
    /// </remarks>
    /// <param name="operation"></param>
    public PatchExecutionResult(PatchOperation operation)
    {
        Path = operation.Path;
        Op = operation.Op;
        From = operation.From;
        Value = operation.Value;
        Success = true;
    }

    /// <summary>
    /// Creates a new instance of <see cref="PatchExecutionResult"/> with the given operation and error message.
    /// </summary>
    /// <remarks>
    /// This constructor is used when the patch operation fails.
    /// </remarks>
    /// <param name="operation"></param>
    /// <param name="errorMessage"></param>
    public PatchExecutionResult(PatchOperation operation, string errorMessage)
    {
        Path = operation.Path;
        Op = operation.Op;
        From = operation.From;
        Value = operation.Value;
        Success = false;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Indicates whether the patch was applied successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// The error message if the patch was not applied successfully.
    /// </summary>
    public string? ErrorMessage { get; }
}
