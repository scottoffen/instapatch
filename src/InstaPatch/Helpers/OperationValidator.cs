using System.ComponentModel.DataAnnotations;

namespace InstaPatch.Helpers;

internal static class OperationValidator<T>
{
    public static ValidationResult? Validate(PatchOperation operation)
    {
        // TODO: Implement validation logic
        return ValidationResult.Success;
    }
}
