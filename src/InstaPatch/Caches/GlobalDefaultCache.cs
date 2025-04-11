using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace InstaPatch.Caches;

/// <summary>
/// A static class that caches default values for different types.
/// </summary>
internal static class GlobalDefaultCache
{
    private static readonly ConcurrentDictionary<Type, object?> DefaultsCache = new();

    static GlobalDefaultCache()
    {
        // Preload defaults for common primitive types.
        DefaultsCache.TryAdd(typeof(bool), default(bool));
        DefaultsCache.TryAdd(typeof(byte), default(byte));
        DefaultsCache.TryAdd(typeof(sbyte), default(sbyte));
        DefaultsCache.TryAdd(typeof(short), default(short));
        DefaultsCache.TryAdd(typeof(ushort), default(ushort));
        DefaultsCache.TryAdd(typeof(int), default(int));
        DefaultsCache.TryAdd(typeof(uint), default(uint));
        DefaultsCache.TryAdd(typeof(long), default(long));
        DefaultsCache.TryAdd(typeof(ulong), default(ulong));
        DefaultsCache.TryAdd(typeof(float), default(float));
        DefaultsCache.TryAdd(typeof(double), default(double));
        DefaultsCache.TryAdd(typeof(decimal), default(decimal));
        DefaultsCache.TryAdd(typeof(char), default(char));
        DefaultsCache.TryAdd(typeof(string), null); // Reference type: default is null.
        DefaultsCache.TryAdd(typeof(DateTime), default(DateTime));
        // DefaultsCache.TryAdd(typeof(DateOnly), default(DateOnly));
    }

    /// <summary>
    /// Retrieves the default value for a given type, caching it for future calls.
    /// </summary>
    /// <param name="type">The type for which to get the default value.</param>
    /// <returns>
    /// The default value (null for reference types, default(T) for value types).
    /// </returns>
    public static object? GetDefault(Type type)
    {
        return DefaultsCache.GetOrAdd(type, t => CreateDefault(t));
    }

    private static object? CreateDefault(Type type)
    {
        // For reference types, default is always null.
        if (!type.IsValueType)
        {
            return null;
        }

        // For value types, compile an expression that returns the default.
        var defaultExpression = Expression.Lambda(Expression.Default(type));
        return defaultExpression.Compile().DynamicInvoke();
    }
}
