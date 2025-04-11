namespace InstaPatch.Caches;

/// <summary>
/// Caches property comparers for a given type.
/// </summary>
/// <typeparam name="T"></typeparam>
internal static class PropertyComparerCache<T>
{
    internal static readonly Dictionary<string, Func<object, object, bool>> PropertyComparers = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Func<object, object, bool> DefaultComparer = (_, _) => false;

    static PropertyComparerCache()
    {
        foreach (var property in PropertyInfoCache<T>.Values.Values)
        {
            if (property.CanRead)
            {
                PropertyComparers.Add(property.Name, GlobalComparerCache.GetComparer(property.PropertyType));
            }
        }
    }

    /// <summary>
    /// Returns the property comparer for a given property name.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="comparer"></param>
    /// <returns></returns>
    public static bool TryGetValue(string propertyName, out Func<object, object, bool> comparer)
    {
        var exists = PropertyComparers.TryGetValue(propertyName, out var foundComparer);
        comparer = exists && foundComparer != null ? foundComparer : DefaultComparer;
        return exists;
    }
}
