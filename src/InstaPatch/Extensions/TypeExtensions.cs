using System.Collections;

namespace InstaPatch.Extensions;

internal static class TypeExtensions
{
    /// <summary>
    /// Returns true if the property can have sub-properties.
    /// </summary>
    /// <remarks>
    /// This method will return true on all types that are not:
    /// <list type="bullet">
    /// <item>Primitive types (int, bool, double, etc.)</item>
    /// <item>Enums</item>
    /// <item>Strings</item>
    /// <item>Decimals</item>
    /// <item>DateTimes</item>
    /// <item>DateTimeOffsets</item>
    /// <item>TimeSpans</item>
    /// <item>Guids</item>
    /// </list>
    /// Also, it will return true on collection types. If you first want to check if a type is a collection type, use <see cref="IsCollectionType(Type)"/>.
    /// </remarks>
    /// <param name="type"></param>
    public static bool CanHaveProperties(this Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        // Get the property's type and check if it's nullable.
        type = Nullable.GetUnderlyingType(type) ?? type;

        // If the type is one of the simple types, it cannot have sub-properties.
        if (type.IsPrimitive ||        // covers int, bool, double, etc.
            type.IsEnum ||             // covers enumerations
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) || // optional: time spans are simple too
            type == typeof(Guid))       // optional: GUIDs are simple types
        {
            return false;
        }

        // Otherwise, assume the type can have sub-properties.
        return true;
    }

    /// <summary>
    /// Returns true if the type is a collection type (but not a string).
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsCollectionType(this Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        if (type == typeof(string))
        {
            return false;
        }

        return type.IsArray || typeof(IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// Returns the type of the elements in a collection type.
    /// </summary>
    /// <param name="collectionType"></param>
    /// <returns></returns>
    public static Type? GetElementType(this Type collectionType)
    {
        if (collectionType == null) throw new ArgumentNullException(nameof(collectionType));

        if (collectionType.IsArray)
        {
            return collectionType.GetElementType()!;
        }

        // Look for IEnumerable<T> on the type or its interfaces.
        var enumerableInterface = collectionType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableInterface != null)
        {
            return enumerableInterface.GetGenericArguments()[0];
        }

        return null;
    }
}
