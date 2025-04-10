namespace InstaPatch;

/// <summary>
/// Represents the result of a patch operation.
/// </summary>
public class PatchExecutionResult : PatchOperation
{
    public PatchExecutionResult(PatchOperation operation)
    {
        Path = operation.Path;
        Op = operation.Op;
        From = operation.From;
        Value = operation.Value;
    }

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
    public bool Success { get; } = true;

    /// <summary>
    /// The error message if the patch was not applied successfully.
    /// </summary>
    public string? ErrorMessage { get; }
}
