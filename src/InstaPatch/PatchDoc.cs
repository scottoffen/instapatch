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
        throw new NotImplementedException();
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
                var result = OperationValidator<T>.Validate(operation);
                if (result != ValidationResult.Success && result != null)
                {
                    yield return result;
                }
            }
        }
        else
        {
            yield return new ValidationResult(ErrorMessages<T>.TypeNotPatchable);
        }
    }
}
