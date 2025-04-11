namespace InstaPatch.Caches;

/// <summary>
/// Caches property defaults for a given type.
/// </summary>
/// <typeparam name="T"></typeparam>
internal static class PropertyDefaultsCache<T>
{
    internal static readonly Dictionary<string, object?> PropertyDefaults = new(StringComparer.OrdinalIgnoreCase);

    static PropertyDefaultsCache()
    {
        foreach (var property in PropertyInfoCache<T>.Values.Values)
        {
            if (property.CanRead && property.CanWrite)
            {
                PropertyDefaults.Add(property.Name, GlobalDefaultCache.GetDefault(property.PropertyType));
            }
        }
    }

    /// <summary>
    /// Returns the property default for a given property name.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static bool TryGetValue(string propertyName, out object? defaultValue)
    {
        return PropertyDefaults.TryGetValue(propertyName, out defaultValue);
    }
}
