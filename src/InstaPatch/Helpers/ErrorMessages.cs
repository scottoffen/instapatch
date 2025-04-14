namespace InstaPatch.Helpers;

internal static class ErrorMessages<T>
{
    private static readonly string _typeName = typeof(T).Name;

    /// <summary>
    /// The error message for when a type is not patchable.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// none
    /// </remarks>
    public static readonly string TypeNotPatchable = $"Type {_typeName} cannot be patched. This is either because it has the {nameof(DenyPatchAttribute)} attribute or all of its properties are read-only or have the {nameof(DenyPatchAttribute)} attribute.";

    /// <summary>
    /// The error message for when an operation from is not valid.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// 0 - operation name
    /// 1 - operation from
    /// </remarks>
    public static readonly string OperationFromNotValid = $"{{0}} operation from '{{1}}' is not valid for type {_typeName}.";

    /// <summary>
    /// The error message for when an operation path is not valid.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// 0 - operation name
    /// 1 - operation path
    /// </remarks>
    public static readonly string OperationPathNotValid = $"{{0}} operation path '{{1}}' is not valid for type {_typeName}.";

    /// <summary>
    /// The error message for when an operation is not supported.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// 0 - operation name
    /// </remarks>
    public static readonly string OperationNotSupported = "{0} operation is not supported.";

    /// <summary>
    /// The error message for when an operation requires a path.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// 0 - operation name
    /// </remarks>
    public static readonly string OperationRequiresPath = "{0} operation requires a path.";

    /// <summary>
    /// The error message for when an operation requires a value.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// 0 - operation name
    /// </remarks>
    public static readonly string OperationRequiresValue = "{0} operation requires a value.";

    /// <summary>
    /// The error message for when an operation requires a from path.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// 0 - operation name
    /// </remarks>
    public static readonly string OperationRequiresFrom = "{0} operation requires a from path.";

    /// <summary>
    /// The error message for when a property is not readable.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// 0 - property name
    /// </remarks>
    public static readonly string PropertyNotReadable = $"Property '{{0}}' is missing or cannot be read from type {_typeName}.";

    /// <summary>
    /// The error message for when a property is not writeable.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// 0 - property name
    /// </remarks>
    public static readonly string PropertyNotWriteable = $"Property '{{0}}' is missing or does not support patching on type {_typeName}.";

    /// <summary>
    /// The error message for when an operation fails.
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// 0 - expected value
    /// 1 - actual value
    /// </remarks>
    public static readonly string OperationTestFailed = "Expected value {0} does not equal actual value {1}.";
}
