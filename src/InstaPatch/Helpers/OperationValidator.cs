using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using InstaPatch.Caches;
using InstaPatch.Extensions;

namespace InstaPatch.Helpers;

internal static class OperationValidator<T>
{
    private static readonly Type _type = typeof(T);
    private static readonly ConcurrentDictionary<(string, OperationType), bool> _checkedPaths = new();
    private static readonly ConcurrentDictionary<(string, OperationType), bool> _checkedFroms = new();

    /// <summary>
    /// Returns true if the operation is valid.
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public static bool IsValid(PatchOperation operation)
    {
        return Validate(operation) == ValidationResult.Success;
    }

    /// <summary>
    /// Returns a ValidationResult if the operation is invalid.
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public static ValidationResult? Validate(PatchOperation operation)
    {
        var path = operation.Path ?? string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return new ValidationResult(string.Format(ErrorMessages<T>.OperationRequiresPath, operation.Op));
        }
        else
        {
            var requiresGetter = operation.Op == OperationType.Test;
            var requiresSetter = (operation.Op & (OperationType.Add | OperationType.Copy | OperationType.Move | OperationType.Remove | OperationType.Replace)) != 0;

            if (!ValidatePathProperty(operation.Op, path, requiresGetter, requiresSetter))
            {
                return new ValidationResult(string.Format(ErrorMessages<T>.OperationPathNotValid, operation.Op, path));
            }
        }

        if (OperationTypes.RequiresFrom.HasFlag(operation.Op))
        {
            var from = operation.From ?? string.Empty;
            if (string.IsNullOrWhiteSpace(from))
            {
                return new ValidationResult(string.Format(ErrorMessages<T>.OperationRequiresFrom, operation.Op));
            }
            else
            {
                var requiresGetter = (operation.Op & (OperationType.Copy | OperationType.Move)) != 0;
                var requiresSetter = (operation.Op & (OperationType.Move)) != 0;

                if (!ValidateFromProperty(operation.Op, from, requiresGetter, requiresSetter))
                {
                    return new ValidationResult(string.Format(ErrorMessages<T>.OperationFromNotValid, operation.Op, from));
                }
            }
        }

        if (OperationTypes.RequiresValue.HasFlag(operation.Op) && operation.Value == null)
        {
            return new ValidationResult(string.Format(ErrorMessages<T>.OperationRequiresValue, operation.Op));
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Returns true if the property path is valid.
    /// </summary>
    /// <param name="opType"></param>
    /// <param name="path"></param>
    /// <param name="requiresGetter"></param>
    /// <param name="requiresSetter"></param>
    /// <returns></returns>
    public static bool ValidatePathProperty(OperationType opType, string path, bool requiresGetter, bool requiresSetter)
    {
        // Normalize numeric segments in the sanitized path.
        var sanitizedPath = Regex.Replace(path, @"/\d+(?=/|$)", "/0");

        if (_checkedPaths.TryGetValue((sanitizedPath, opType), out var isValid))
        {
            return isValid;
        }
        else
        {
            var segments = sanitizedPath.TrimStart('/').Split('/').ToList();
            var result = ValidatePropertySegments(segments, requiresGetter, requiresSetter);

            _checkedPaths.TryAdd((sanitizedPath, opType), result);
            return result;
        }
    }

    /// <summary>
    /// Returns true if the property path is valid.
    /// </summary>
    /// <param name="opType"></param>
    /// <param name="path"></param>
    /// <param name="requiresGetter"></param>
    /// <param name="requiresSetter"></param>
    /// <returns></returns>
    public static bool ValidateFromProperty(OperationType opType, string path, bool requiresGetter, bool requiresSetter)
    {
        // Normalize numeric segments in the sanitized path.
        var sanitizedPath = Regex.Replace(path, @"/\d+(?=/|$)", "/0");

        if (_checkedFroms.TryGetValue((sanitizedPath, opType), out var isValid))
        {
            return isValid;
        }
        else
        {
            var segments = sanitizedPath.TrimStart('/').Split('/').ToList();
            var result = ValidatePropertySegments(segments, requiresGetter, requiresSetter);

            _checkedFroms.TryAdd((sanitizedPath, opType), result);
            return result;
        }
    }

    /// <summary>
    /// Returns true if the each segment of the property path is valid.
    /// </summary>
    /// <param name="segments"></param>
    /// <param name="requiresGetter"></param>
    /// <param name="requiresSetter"></param>
    /// <returns></returns>
    public static bool ValidatePropertySegments(List<string> segments, bool requiresGetter, bool requiresSetter)
    {
        // If there is not property name, then there is no property to validate.
        var propertyName = segments.FirstOrDefault();
        if (propertyName == null) return true;

        if (requiresGetter && !PropertyGetterCache<T>.Contains(propertyName))
        {
            return false;
        }

        if (requiresSetter && !PropertySetterCache<T>.Contains(propertyName))
        {
            return false;
        }

        if (!PropertyInfoCache<T>.Values.TryGetValue(propertyName, out var propertyInfo))
        {
            return false;
        }

        // If there is a valid property name and there are not more segments,
        // then the path is valid.
        if (segments.Count == 1) return true;

        if (propertyInfo.PropertyType.IsCollectionType())
        {
            // If the property is a collection type, then the next segment
            // should be the index of the collection.
            // If the next segment is not a number, then the path is invalid.
            if (segments.Count < 2 || !int.TryParse(segments[1], out _))
            {
                return false;
            }

            // Get the element type of the collection. Return false if the
            // element type is null (this should not happen, but just in case).
            var elementType = propertyInfo.PropertyType.GetElementType();
            if (elementType == null) return false;

            // Get a reference to OperationValidator<T>.ValidateProperty for the element type
            var validator = typeof(OperationValidator<>).MakeGenericType(elementType);
            var validate = validator.GetMethod("ValidatePropertySegments", BindingFlags.Public | BindingFlags.Static);
            if (validate == null) return false;

            // Skip the first two segments (the property name and the index) and
            // validate the rest of the segments. The next segment should be a property
            // name of the element type.
            return (bool)validate.Invoke(null, new object[] { segments.Skip(2).ToList(), requiresGetter, requiresSetter })!;
        }

        if (propertyInfo.PropertyType.CanHaveProperties())
        {
            // If the property type can have properties, then validate that the next
            // segment is a property name of the property type.
            var validator = typeof(OperationValidator<>).MakeGenericType(propertyInfo.PropertyType);
            var validate = validator.GetMethod("ValidatePropertySegments", BindingFlags.Public | BindingFlags.Static);
            if (validate == null) return false;

            // Skip the first segment (the property name) and validate the rest of the
            // segments. The next segment should be a property name of the property type.
            return (bool)validate.Invoke(null, new object[] { segments.Skip(1).ToList(), requiresGetter, requiresSetter })!;
        }

        // If there are additional segments, but the property type is not a
        // collection type or a type that can have sub-properties, then the
        // path is invalid.
        return false;
    }
}
