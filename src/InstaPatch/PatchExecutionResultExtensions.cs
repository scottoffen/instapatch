namespace InstaPatch;

public static class PatchExecutionResultExtensions
{
    /// <summary>
    /// Converts a <see cref="PatchExecutionResult"/> to a <see cref="PatchOperation"/>.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static PatchOperation ToPatchOperation(this PatchExecutionResult result)
    {
        return new PatchOperation
        {
            Op = result.Op,
            Path = result.Path,
            Value = result.Value,
            From = result.From
        };
    }
}
