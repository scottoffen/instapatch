using System.Linq.Expressions;

namespace InstaPatch.Caches;

/// <summary>
/// Caches property getters for a given type.
/// </summary>
/// <typeparam name="T"></typeparam>
internal static class PropertyGetterCache<T>
{
    /// <summary>
    /// Collection of property getters keyed by property name.
    /// </summary>
    /// <remarks>
    /// Not every property will necessarily  have a getter.
    /// </remarks>
    public static readonly Dictionary<string, Func<T, object?>> Values = new(StringComparer.OrdinalIgnoreCase);

    public static readonly Func<T, object?> DefaultGetter = _ => null;

    static PropertyGetterCache()
    {
        foreach (var property in PropertyInfoCache<T>.Values.Values)
        {
            if (property.CanRead)
            {
                var instanceParam = Expression.Parameter(typeof(T), "instance");
                var propertyAccess = Expression.Property(instanceParam, property);
                var convert = Expression.Convert(propertyAccess, typeof(object));
                var getter = Expression.Lambda<Func<T, object?>>(convert, instanceParam).Compile();

                Values.Add(property.Name, getter);
            }
        }
    }

    /// <summary>
    /// Returns true if the property getter was found.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="getter"></param>
    /// <returns></returns>
    public static bool TryGetValue(string propertyName, out Func<T, object?> getter)
    {
        var exists = Values.TryGetValue(propertyName, out var foundGetter);
        getter = exists && foundGetter != null ? foundGetter : DefaultGetter;
        return exists;
    }

    /// <summary>
    /// Returns true if a property getter was found.
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
