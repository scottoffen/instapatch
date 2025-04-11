using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace InstaPatch;

/// <summary>
/// PatchDoc is a static class that provides methods for applying JSON Patch operations to a given type.
/// </summary>
/// <typeparam name="T"></typeparam>
public static class PatchDoc<T> where T : class
{
    internal static readonly Type CurrentType = typeof(T);

    internal static readonly HashSet<string> PropertyNames = new();
    internal static readonly Dictionary<string, Func<object, object, bool>> PropertyComparers = new(StringComparer.OrdinalIgnoreCase);
    internal static readonly Dictionary<string, object?> PropertyDefaults = new(StringComparer.OrdinalIgnoreCase);
    internal static readonly Dictionary<string, Func<T, object?>> PropertyGetters = new(StringComparer.OrdinalIgnoreCase);
    internal static readonly Dictionary<string, Action<T, object?>> PropertySetters = new(StringComparer.OrdinalIgnoreCase);
    internal static readonly Dictionary<string, Type> PropertyTypes = new(StringComparer.OrdinalIgnoreCase);

    internal static readonly Func<T, T>? ShallowClone = null;

    internal static bool IsPatchable { get; private set; } = true;

    internal static readonly string ErrorMessageTypeNotPatchable = $"Type {CurrentType.Name} cannot be patched. This is either because it has the {nameof(DenyPatchAttribute)} attribute or all of its properties are read-only or have the {nameof(DenyPatchAttribute)} attribute.";
    internal static readonly string ErrorMessageOperationNotSupported = "{0} operation is not supported.";
    internal static readonly string ErrorMessageOperationRequiresPath = "{0} operation requires a path.";
    internal static readonly string ErrorMessageOperationRequiresValue = "{0} operation requires a value.";
    internal static readonly string ErrorMessageOperationRequiresFrom = "{0} operation requires a from path.";
    internal static readonly string ErrorMessagePropertyNotReadable = $"Property '{{0}}' is missing or cannot be read from type {CurrentType.Name}.";
    internal static readonly string ErrorMessagePropertyNotWriteable = $"Property '{{0}}' is missing or does not support patching on type {CurrentType.Name}.";
    internal static readonly string ErrorMessageOperationTestFailed = "Expected value {0} does not equal actual value {1}.";

    static PatchDoc()
    {
        var denyPatchAttribute = CurrentType.GetCustomAttribute<DenyPatchAttribute>();
        if (denyPatchAttribute == null)
        {
            var properties = CurrentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var name = property.Name ??
                    throw new InvalidOperationException($"Property {property.Name} is null.");

                if (property.CanRead)
                {
                    var instanceParam = Expression.Parameter(typeof(T), "instance");
                    var propertyAccess = Expression.Property(instanceParam, property);
                    var convert = Expression.Convert(propertyAccess, typeof(object));
                    var getter = Expression.Lambda<Func<T, object?>>(convert, instanceParam).Compile();

                    PropertyGetters.Add(name, getter);
                    PropertyComparers.Add(name, GlobalComparerCache.GetComparer(property.PropertyType));
                }

                if (property.CanWrite && property.GetCustomAttribute<DenyPatchAttribute>() == null)
                {
                    var instanceParam = Expression.Parameter(typeof(T), "instance");
                    var valueParam = Expression.Parameter(typeof(object), "value");
                    var convert = Expression.Convert(valueParam, property.PropertyType);
                    var caller = Expression.Call(instanceParam, property.GetSetMethod()!, convert);
                    var setter = Expression.Lambda<Action<T, object?>>(caller, instanceParam, valueParam).Compile();

                    PropertySetters.Add(name, setter);
                    PropertyDefaults.Add(name, GlobalDefaultCache.GetDefault(property.PropertyType));
                    PropertyNames.Add(name);
                }
            }

            IsPatchable = PropertyNames.Count > 0;

            if (IsPatchable)
            {
                var method = CurrentType.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
                if (method != null)
                {
                    var sourceParam = Expression.Parameter(CurrentType, "source");
                    var castedSource = Expression.Convert(sourceParam, CurrentType);
                    var caller = Expression.Call(castedSource, method);
                    var convert = Expression.Convert(caller, CurrentType);
                    var lambda = Expression.Lambda<Func<T, T>>(convert, sourceParam).Compile();
                    ShallowClone = lambda;
                }
            }
        }
        else
        {
            IsPatchable = false;
        }
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

    public static bool TryApplyPatch(T instance, IEnumerable<PatchOperation> operations, out IEnumerable<PatchExecutionResult> results)
    {
        if (!IsPatchable) throw new InvalidOperationException(ErrorMessageTypeNotPatchable);

        var executions = new List<PatchExecutionResult>();
        var clone = ShallowClone!(instance);

        foreach (var operation in operations)
        {
            executions.Add(TryApplyPatch(clone, operation));
        }

        results = executions;

        if (executions.All(x => x.Success))
        {
            foreach (var setter in PropertySetters)
            {
                var value = PropertyGetters[setter.Key](clone);
                setter.Value(instance, value);
            }

            return true;
        }
        else
        {
            return false;
        }
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
                var path = GetPropertyName(operation.Path);
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
                    if (operation.Value == null)
                    {
                        yield return new ValidationResult(string.Format(ErrorMessageOperationRequiresValue, operation.Op));
                    }
                }

                if (OperationTypes.RequiresFrom.HasFlag(operation.Op))
                {
                    var fromPath = GetPropertyName(operation.From);

                    if (string.IsNullOrWhiteSpace(fromPath))
                    {
                        yield return new ValidationResult(string.Format(ErrorMessageOperationRequiresFrom, operation.Op));
                    }
                    else if (!PropertyGetters.ContainsKey(fromPath))
                    {
                        yield return new ValidationResult(string.Format(ErrorMessagePropertyNotReadable, operation.From));
                    }
                }
            }
        }
        else
        {
            yield return new ValidationResult(ErrorMessageTypeNotPatchable);
        }
    }

    internal static PatchExecutionResult TryApplyPatch(T instance, PatchOperation operation)
    {
        var path = GetPropertyName(operation.Path);
        if (path == null)
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessageOperationRequiresPath, operation.Op));
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
                return new PatchExecutionResult(operation, string.Format(ErrorMessageOperationNotSupported, operation.Op));
        }
    }

    internal static PatchExecutionResult TryApplyAdd(T instance, string path, PatchOperation operation)
    {
        return new PatchExecutionResult(operation, string.Format(ErrorMessageOperationNotSupported, operation.Op));
    }

    internal static PatchExecutionResult TryApplyCopy(T instance, string path, PatchOperation operation)
    {
        var from = GetPropertyName(operation.From);
        if (string.IsNullOrWhiteSpace(from))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessageOperationRequiresFrom, operation.Op));
        }

        if (!PropertyGetters.TryGetValue(from, out var getter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotReadable, operation.From));
        }

        if (!PropertySetters.TryGetValue(path, out var setter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotWriteable, operation.Path));
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
        var from = GetPropertyName(operation.From);
        if (string.IsNullOrWhiteSpace(from))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessageOperationRequiresFrom, operation.Op));
        }

        if (!PropertyGetters.TryGetValue(from, out var sourceGetter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotReadable, operation.From));
        }

        if (!PropertySetters.TryGetValue(path, out var destinationSetter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotWriteable, operation.Path));
        }

        if (!PropertySetters.TryGetValue(from, out var sourceSetter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotWriteable, operation.From));
        }

        if (!PropertyDefaults.TryGetValue(from, out var defaultValue))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotWriteable, operation.From));
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
        if (!PropertySetters.TryGetValue(path, out var setter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotWriteable, operation.Path));
        }

        if (!PropertyDefaults.TryGetValue(path, out var defaultValue))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotWriteable, operation.Path));
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
        if (!PropertySetters.TryGetValue(path, out var setter))
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotWriteable, operation.Path));
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
        var getter = PropertyGetters[path];
        var comparer = PropertyComparers[path];

        if (getter == null || comparer == null)
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessagePropertyNotReadable, operation.Path));
        }

        var value = getter(instance);
        if (value == null && operation.Value == null)
        {
            return new PatchExecutionResult(operation);
        }
        else if (value == null || operation.Value == null)
        {
            return new PatchExecutionResult(operation, string.Format(ErrorMessageOperationTestFailed, operation.Value, value));
        }

        try
        {
            return (comparer(value, operation.Value))
                ? new PatchExecutionResult(operation)
                : new PatchExecutionResult(operation, string.Format(ErrorMessageOperationTestFailed, operation.Value, value));
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
