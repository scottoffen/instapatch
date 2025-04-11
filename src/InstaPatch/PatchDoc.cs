using System.ComponentModel.DataAnnotations;
using System.Reflection;
using InstaPatch.Caches;
using InstaPatch.Helpers;

namespace InstaPatch;

/// <summary>
/// PatchDoc is a static class that provides methods for applying JSON Patch operations to a given type.
/// </summary>
/// <typeparam name="T"></typeparam>
public static class PatchDoc<T> where T : class
{
    private static bool IsPatchable { get; }

    static PatchDoc()
    {
        var denyPatchAttribute = typeof(T).GetCustomAttribute<DenyPatchAttribute>();
        IsPatchable = denyPatchAttribute == null && PropertySetterCache<T>.Any();
    }

    /// <summary>
    /// Returns true if the given operations are valid against the properties of the type.
    /// </summary>
    /// <param name="operations"></param>
    /// <returns></returns>
    public static bool IsValid(IEnumerable<PatchOperation> operations)
    {
        return !Validate(operations).Any();
    }

    /// <summary>
    /// Applies the given operations to the instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="operations"></param>
    /// <param name="results"></param>
    /// <returns>True if all operations were applied successfully.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static bool TryApplyPatch(T instance, IEnumerable<PatchOperation> operations, out IEnumerable<PatchExecutionResult> results)
    {
        if (!IsPatchable) throw new InvalidOperationException(ErrorMessages<T>.TypeNotPatchable);
        if (!TypeCloneCache<T>.TryCreateClone(instance, out var clone))
        {
            throw new InvalidOperationException(ErrorMessages<T>.PropertyNotWriteable);
        }

        var executions = new List<PatchExecutionResult>();

        foreach (var operation in operations)
        {
            executions.Add(TryApplyPatch(clone, operation));
        }

        results = executions;

        if (executions.All(x => x.Success))
        {
            foreach (var setter in PropertySetterCache<T>.Values)
            {
                var value = PropertyGetterCache<T>.Values[setter.Key](clone);
                setter.Value(instance, value);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Validates the given operations against the properties of the type.
    /// </summary>
    /// <param name="operations"></param>
    /// <returns></returns>
    public static IEnumerable<ValidationResult> Validate(IEnumerable<PatchOperation> operations)
    {
        if (IsPatchable)
        {
            foreach (var operation in operations)
            {
                var path = GetPropertyName(operation.Path) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path))
                {
                    yield return new ValidationResult(string.Format(ErrorMessages<T>.OperationRequiresPath, operation.Op));
                }
                else
                {
                    if (OperationTypes.RequiresGetter.HasFlag(operation.Op))
                    {
                        if (!PropertyGetterCache<T>.Contains(path))
                        {
                            yield return new ValidationResult(string.Format(ErrorMessages<T>.PropertyNotReadable, operation.Path));
                        }
                    }

                    if (OperationTypes.RequiresSetter.HasFlag(operation.Op))
                    {
                        if (!PropertySetterCache<T>.Contains(path))
                        {
                            yield return new ValidationResult(string.Format(ErrorMessages<T>.PropertyNotWriteable, operation.Path));
                        }
                    }
                }

                if (OperationTypes.RequiresValue.HasFlag(operation.Op))
                {
                    if (operation.Value == null)
                    {
                        yield return new ValidationResult(string.Format(ErrorMessages<T>.OperationRequiresValue, operation.Op));
                    }
                }

                if (OperationTypes.RequiresFrom.HasFlag(operation.Op))
                {
                    var fromPath = GetPropertyName(operation.From) ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(fromPath))
                    {
                        yield return new ValidationResult(string.Format(ErrorMessages<T>.OperationRequiresFrom, operation.Op));
                    }
                    else if (!PropertyGetterCache<T>.Contains(fromPath))
                    {
                        yield return new ValidationResult(string.Format(ErrorMessages<T>.PropertyNotReadable, operation.From));
                    }
                }
            }
        }
        else
        {
            yield return new ValidationResult(ErrorMessages<T>.TypeNotPatchable);
        }
    }

    internal static PatchExecutionResult TryApplyPatch(T instance, PatchOperation operation)
    {
        var path = GetPropertyName(operation.Path);
        if (path == null)
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.OperationRequiresPath, operation.Op));
        }

        switch (operation.Op)
        {
            case OperationType.Add:
                return TryApplyAdd(instance, path, operation);
            case OperationType.Copy:
                return TryApplyCopy(instance, path, operation);
            case OperationType.Move:
                return TryApplyMove(instance, path, operation);
            case OperationType.Remove:
                return TryApplyRemove(instance, path, operation);
            case OperationType.Replace:
                return TryApplyReplace(instance, path, operation);
            case OperationType.Test:
                return TryApplyTest(instance, path, operation);
            default:
                return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.OperationNotSupported, operation.Op));
        }
    }

    internal static PatchExecutionResult TryApplyAdd(T instance, string path, PatchOperation operation)
    {
        return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.OperationNotSupported, operation.Op));
    }

    internal static PatchExecutionResult TryApplyCopy(T instance, string path, PatchOperation operation)
    {
        var from = GetPropertyName(operation.From) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(from))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.OperationRequiresFrom, operation.Op));
        }

        if (!PropertyGetterCache<T>.TryGetValue(from, out var getter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotReadable, operation.From));
        }

        if (!PropertySetterCache<T>.TryGetValue(path, out var setter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotWriteable, operation.Path));
        }

        try
        {
            var value = getter(instance);
            setter(instance, value);
            return new PatchExecutionResult(operation);
        }
        catch (Exception ex)
        {
            return new PatchExecutionResult(operation, ex.Message);
        }
    }

    internal static PatchExecutionResult TryApplyMove(T instance, string path, PatchOperation operation)
    {
        var from = GetPropertyName(operation.From) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(from))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.OperationRequiresFrom, operation.Op));
        }

        if (!PropertyGetterCache<T>.TryGetValue(from, out var sourceGetter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotReadable, operation.From));
        }

        if (!PropertySetterCache<T>.TryGetValue(path, out var destinationSetter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotWriteable, operation.Path));
        }

        if (!PropertySetterCache<T>.TryGetValue(from, out var sourceSetter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotWriteable, operation.From));
        }

        if (!PropertyDefaultsCache<T>.TryGetValue(from, out var defaultValue))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotWriteable, operation.From));
        }

        try
        {
            var value = sourceGetter(instance);
            destinationSetter(instance, value);
            sourceSetter(instance, defaultValue);
            return new PatchExecutionResult(operation);
        }
        catch (Exception ex)
        {
            return new PatchExecutionResult(operation, ex.Message);
        }
    }

    internal static PatchExecutionResult TryApplyRemove(T instance, string path, PatchOperation operation)
    {
        if (!PropertySetterCache<T>.TryGetValue(path, out var setter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotWriteable, operation.Path));
        }

        if (!PropertyDefaultsCache<T>.TryGetValue(path, out var defaultValue))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotWriteable, operation.Path));
        }

        try
        {
            setter(instance, defaultValue);
            return new PatchExecutionResult(operation);
        }
        catch (Exception ex)
        {
            return new PatchExecutionResult(operation, ex.Message);
        }
    }

    internal static PatchExecutionResult TryApplyReplace(T instance, string path, PatchOperation operation)
    {
        if (!PropertySetterCache<T>.TryGetValue(path, out var setter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotWriteable, operation.Path));
        }

        try
        {
            setter(instance, operation.Value);
            return new PatchExecutionResult(operation);
        }
        catch (Exception ex)
        {
            return new PatchExecutionResult(operation, ex.Message);
        }
    }

    internal static PatchExecutionResult TryApplyTest(T instance, string path, PatchOperation operation)
    {
        if (!PropertyGetterCache<T>.TryGetValue(path, out var getter) || !PropertyComparerCache<T>.TryGetValue(path, out var comparer))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.PropertyNotReadable, operation.Path));
        }

        var value = getter(instance);
        if (value == null && operation.Value == null)
        {
            return new PatchExecutionResult(operation);
        }
        else if (value == null || operation.Value == null)
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.OperationTestFailed, operation.Value, value));
        }

        try
        {
            return (comparer(value, operation.Value))
                ? new PatchExecutionResult(operation)
                : new PatchExecutionResult(operation, string.Format(ErrorMessages<T>.OperationTestFailed, operation.Value, value));
        }
        catch (Exception ex)
        {
            return new PatchExecutionResult(operation, ex.Message);
        }
    }

    private static string? GetPropertyName(string? path)
    {
        return path?.TrimStart('/').Split('/')[0] ?? null;
    }
}
