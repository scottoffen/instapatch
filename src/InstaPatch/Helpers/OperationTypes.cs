using System.Diagnostics.CodeAnalysis;

namespace InstaPatch.Helpers;

[ExcludeFromCodeCoverage]
internal static class OperationTypes
{
    public static readonly OperationType RequiresGetter = OperationType.Test;

    public static readonly OperationType RequiresSetter = OperationType.Add | OperationType.Copy | OperationType.Remove | OperationType.Replace;

    public static readonly OperationType RequiresValue = OperationType.Add | OperationType.Replace;

    public static readonly OperationType RequiresFrom = OperationType.Copy | OperationType.Move;
}
