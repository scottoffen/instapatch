using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace InstaPatch;

public static class PatchDoc<T> where T : class
{
    internal static readonly Type CurrentType = typeof(T);

    internal static readonly HashSet<string> PropertyNames = new();
    internal static readonly HashSet<string> HasGetter = new();
    internal static readonly HashSet<string> HasSetter = new();

    internal static bool IsPatchable { get; private set; } = true;

    internal static readonly string ErrorMessageTypeNotPatchable = $"Type {CurrentType.Name} cannot be patched. This is either because it has the {nameof(DenyPatchAttribute)} attribute or all of its properties are read-only or have the {nameof(DenyPatchAttribute)} attribute.";
    internal static readonly string ErrorMessageOperationRequiresPath = "{0} operation requires a path.";
    internal static readonly string ErrorMessageOperationRequiresValue = "{0} operation requires a value.";
    internal static readonly string ErrorMessageOperationRequiresFrom = "{0} operation requires a from path.";
    internal static readonly string ErrorMessagePropertyNotReadable = $"Property '{{0}}' is missing or cannot be read from type {CurrentType.Name}.";
    internal static readonly string ErrorMessagePropertyNotWriteable = $"Property '{{0}}' is missing or does not support patching on type {CurrentType.Name}.";

    static PatchDoc()
    {
        var denyPatchAttribute = CurrentType.GetCustomAttribute<DenyPatchAttribute>();
        if (denyPatchAttribute == null)
        {
            var properties = CurrentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var name = property.Name.ToLower() ??
                    throw new InvalidOperationException($"Property {property.Name} is null.");

                if (property.CanRead)
                    HasGetter.Add(name);

                if (!property.CanWrite || property.GetCustomAttribute<DenyPatchAttribute>() != null)
                {
                    continue;
                }

                PropertyNames.Add(name);
                HasSetter.Add(name);
            }

            IsPatchable = PropertyNames.Count > 0;
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
                var path = operation.Path.TrimStart('/').ToLower();
                if (string.IsNullOrWhiteSpace(path))
                {
                    yield return new ValidationResult(string.Format(ErrorMessageOperationRequiresPath, operation.Op));
                }
                else
                {
                    if (OperationTypes.RequiresGetter.HasFlag(operation.Op))
                    {
                        if (!HasGetter.Contains(path))
                        {
                            yield return new ValidationResult(string.Format(ErrorMessagePropertyNotReadable, operation.Path));
                        }
                    }

                    if (OperationTypes.RequiresSetter.HasFlag(operation.Op))
                    {
                        if (!HasSetter.Contains(path))
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
                    else if (!HasGetter.Contains(fromPath))
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
}
