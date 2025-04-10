using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace InstaPatch;

public static class PatchDoc<T> where T : class
{
    internal static readonly Type CurrentType = typeof(T);

    internal static readonly Dictionary<string, Func<T, object>> PropertyGetters = new();

    internal static readonly Dictionary<string, Action<T, object>> PropertySetters = new();

    internal static bool IsPatchable { get; } = true;

    internal static readonly string ErrorMessageUnableToClone = $"Failed to clone {CurrentType.Name}.";
    internal static readonly string ErrorMessageTypeNotPatchable = $"Type {CurrentType.Name} cannot be patched. This is either because it has the {nameof(DenyPatchAttribute)} attribute or all of its properties are read-only or have the {nameof(DenyPatchAttribute)} attribute.";
    internal static readonly string ErrorMessageOperationRequiresPath = "{0} operation requires a path.";
    internal static readonly string ErrorMessageOperationRequiresValue = "{0} operation requires a value.";
    internal static readonly string ErrorMessageOperationRequiresFrom = "{0} operation requires a from path.";
    internal static readonly string ErrorMessagePropertyNotReadable = $"Property '{{0}}' is missing or cannot be read from type ${CurrentType.Name}.";
    internal static readonly string ErrorMessagePropertyNotWriteable = $"Property '{{0}}' is missing or does not support patching on type {CurrentType.Name}.";

    static PatchDoc()
    {
        var denyPatchAttribute = CurrentType.GetCustomAttribute<DenyPatchAttribute>();
        if (denyPatchAttribute == null)
        {
            var properties = CurrentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead);

            foreach (var property in properties)
            {
                var name = property.Name.ToLower() ??
                    throw new InvalidOperationException($"Property {property.Name} is null.");

                var getter = property.GetMethod;

                if (getter != null)
                {
                    var getterDelegate = getter.CreateDelegate(typeof(Func<T, object>));
                    PropertyGetters[name] = (Func<T, object>)getterDelegate;
                }

                if (property.CanWrite && property.GetCustomAttribute<DenyPatchAttribute>() == null)
                {
                    var setter = property.SetMethod;
                    if (setter != null)
                    {
                        var setterDelegate = setter.CreateDelegate(typeof(Action<T, object>));
                        PropertySetters[name] = (Action<T, object>)setterDelegate;
                    }
                }
            }

            IsPatchable = PropertySetters.Count > 0;
        }
        else
        {
            IsPatchable = false;
        }

        ThrowExceptionIfNotPatchable();
    }

    /// <summary>
    /// Attempts to apply the patch operations to the object. Returns true if all operations were successful.
    /// </summary>
    /// <remarks>
    /// Review the PatchExecutionResult for each operation to determine which operations failed.
    /// </remarks>
    /// <param name="target"></param>
    /// <param name="operations"></param>
    /// <param name="executionResults"></param>
    /// <returns></returns>
    public static bool TryApply(T target, IEnumerable<PatchOperation> operations, out PatchExecutionResult[] executionResults)
    {
        ThrowExceptionIfNotPatchable();

        var clone = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(target))
            ?? throw new InvalidOperationException(ErrorMessageUnableToClone);

        var results = new List<PatchExecutionResult>();

        foreach (var operation in operations)
        {
            results.Add(TryApply(clone, operation));
        }

        executionResults = results.ToArray();

        if (results.All(r => r.Success))
        {
            foreach (var kvp in PropertySetters)
            {
                var getter = PropertyGetters[kvp.Key];
                var value = getter(clone);
                kvp.Value(target, value);
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true if the given operations are valid against the properties of the type.
    /// </summary>
    /// <param name="operations"></param>
    /// <returns></returns>
    public static bool IsValid(IEnumerable<PatchOperation> operations)
    {
        ThrowExceptionIfNotPatchable();
        return !Validate(operations).Any();
    }

    /// <summary>
    /// Validates the given operations against the properties of the type.
    /// </summary>
    /// <param name="operations"></param>
    /// <returns></returns>
    public static IEnumerable<ValidationResult> Validate(IEnumerable<PatchOperation> operations)
    {
        ThrowExceptionIfNotPatchable();

        foreach (var operation in operations)
        {
            var path = operation.Path.TrimStart('/').ToLower();
            if (string.IsNullOrWhiteSpace(path))
            {
                yield return new ValidationResult(string.Format(ErrorMessageOperationRequiresPath, operation.Op));
            }
            else
            {
                if (OperationTypes.RequiresGetter.HasFlag(operation.Op))
                {
                    if (!PropertyGetters.ContainsKey(path))
                    {
                        yield return new ValidationResult(string.Format(ErrorMessagePropertyNotReadable, operation.Path));
                    }
                }

                if (OperationTypes.RequiresSetter.HasFlag(operation.Op))
                {
                    if (!PropertySetters.ContainsKey(path))
                    {
                        yield return new ValidationResult(string.Format(ErrorMessagePropertyNotWriteable, operation.Path));
                    }
                }
            }

            if (OperationTypes.RequiresValue.HasFlag(operation.Op))
            {
                if (string.IsNullOrEmpty(operation.Value))
                {
                    yield return new ValidationResult(string.Format(ErrorMessageOperationRequiresValue, operation.Op));
                }
            }

            if (OperationTypes.RequiresFrom.HasFlag(operation.Op))
            {
                var fromPath = operation.From?.TrimStart('/').ToLower() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(fromPath))
                {
                    yield return new ValidationResult(string.Format(ErrorMessageOperationRequiresFrom, operation.Op));
                }
                else if (!PropertyGetters.ContainsKey(fromPath))
                {
                    yield return new ValidationResult(string.Format(ErrorMessagePropertyNotWriteable, operation.From));
                }
            }
        }
    }

    /// <summary>
    /// Attempts to apply the patch operation to the object in order.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal static PatchExecutionResult TryApply(T target, PatchOperation operation)
    {
        var path = operation.Path.TrimStart('/').ToLower();
        if (string.IsNullOrWhiteSpace(path))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessageOperationRequiresPath, operation.Op));
        }

        switch (operation.Op)
        {
            case OperationType.Add:
                return TryApplyAdd(target, path, operation);
            case OperationType.Copy:
                return TryApplyCopy(target, path, operation);
            case OperationType.Move:
                return TryApplyMove(target, path, operation);
            case OperationType.Remove:
                return TryApplyRemove(target, path, operation);
            case OperationType.Replace:
                return TryApplyReplace(target, path, operation);
            case OperationType.Test:
                return TryApplyTest(target, path, operation);
        }

        throw new NotImplementedException($"Operation {operation.Op} is not implemented.");
    }

    internal static PatchExecutionResult TryApplyAdd(T target, string path, PatchOperation operation)
    {
        // TODO: Implement Add operation
        throw new NotImplementedException();
    }

    internal static PatchExecutionResult TryApplyCopy(T target, string path, PatchOperation operation)
    {
        // TODO: Implement Copy operation
        throw new NotImplementedException();
    }

    internal static PatchExecutionResult TryApplyMove(T target, string path, PatchOperation operation)
    {
        // TODO: Implement Move operation
        throw new NotImplementedException();
    }

    internal static PatchExecutionResult TryApplyRemove(T target, string path, PatchOperation operation)
    {
        // TODO: Implement Remove operation
        throw new NotImplementedException();
    }

    internal static PatchExecutionResult TryApplyReplace(T target, string path, PatchOperation operation)
    {
        // TODO: Implement Replace operation
        throw new NotImplementedException();
    }

    internal static PatchExecutionResult TryApplyTest(T target, string path, PatchOperation operation)
    {
        // TODO: Implement Test operation
        throw new NotImplementedException();
    }

    private static void ThrowExceptionIfNotPatchable()
    {
        if (!IsPatchable)
        {
            throw new InvalidOperationException(ErrorMessageTypeNotPatchable);
        }
    }
}
