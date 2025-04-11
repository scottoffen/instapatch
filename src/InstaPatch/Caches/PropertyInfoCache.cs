using System.Reflection;

namespace InstaPatch.Caches;

/// <summary>
/// Caches property info for a given type.
/// </summary>
/// <typeparam name="T"></typeparam>
internal static class PropertyInfoCache<T>
{
    /// <summary>
    /// Cached property info for all public instance properties of the type.
    /// </summary>
    public static readonly Dictionary<string, PropertyInfo> Values = new();

    static PropertyInfoCache()
    {
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Values.Add(property.Name, property);
        }
    }
}
