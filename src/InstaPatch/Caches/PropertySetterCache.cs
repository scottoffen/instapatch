using System.Linq.Expressions;
using System.Reflection;

namespace InstaPatch.Caches;

/// <summary>
/// Caches property setters for a given type.
/// </summary>
/// <typeparam name="T"></typeparam>
internal static class PropertySetterCache<T>
{
    /// <summary>
    /// Collection of property setters keyed by property name.
    /// </summary>
    /// <remarks>
    /// Not every property will necessarily  have a setter.
    /// </remarks>
    public static readonly Dictionary<string, Action<T, object?>> Values = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Action<T, object?> DefaultSetter = (_, _) => { };

    static PropertySetterCache()
    {
        foreach (var property in PropertyInfoCache<T>.Values.Values)
        {
            if (property.CanWrite && property.GetCustomAttribute<DenyPatchAttribute>() == null)
            {
                var instanceParam = Expression.Parameter(typeof(T), "instance");
                var valueParam = Expression.Parameter(typeof(object), "value");
                var convert = Expression.Convert(valueParam, property.PropertyType);
                var caller = Expression.Call(instanceParam, property.GetSetMethod()!, convert);
                var setter = Expression.Lambda<Action<T, object?>>(caller, instanceParam, valueParam).Compile();

                Values.Add(property.Name, setter);
            }
        }
    }

    /// <summary>
    /// Returns true if the property setter was found.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="setter"></param>
    /// <returns></returns>
    public static bool TryGetValue(string propertyName, out Action<T, object?> setter)
    {
        var exists = Values.TryGetValue(propertyName, out var foundSetter);
        setter = exists && foundSetter != null ? foundSetter : DefaultSetter;
        return exists;
    }

    /// <summary>
    /// Returns true if a property setter was found.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static bool Contains(string propertyName)
    {
        return Values.ContainsKey(propertyName);
    }

    /// <summary>
    /// Returns true if any property setters are available.
    /// </summary>
    /// <returns></returns>
    public static bool Any() => Values.Count > 0;
}
