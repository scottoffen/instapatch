using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace InstaPatch;

/// <summary>
/// A static class that caches comparers for different types.
/// </summary>
internal static class GlobalComparerCache
{
    private static readonly ConcurrentDictionary<Type, Func<object, object, bool>> ComparersCache = new();

    static GlobalComparerCache()
    {
        // Preload comparers for common primitive types, DateTime, and DateOnly.
        ComparersCache.TryAdd(typeof(bool), CreateComparer<bool>());
        ComparersCache.TryAdd(typeof(byte), CreateComparer<byte>());
        ComparersCache.TryAdd(typeof(sbyte), CreateComparer<sbyte>());
        ComparersCache.TryAdd(typeof(short), CreateComparer<short>());
        ComparersCache.TryAdd(typeof(ushort), CreateComparer<ushort>());
        ComparersCache.TryAdd(typeof(int), CreateComparer<int>());
        ComparersCache.TryAdd(typeof(uint), CreateComparer<uint>());
        ComparersCache.TryAdd(typeof(long), CreateComparer<long>());
        ComparersCache.TryAdd(typeof(ulong), CreateComparer<ulong>());
        ComparersCache.TryAdd(typeof(float), CreateComparer<float>());
        ComparersCache.TryAdd(typeof(double), CreateComparer<double>());
        ComparersCache.TryAdd(typeof(decimal), CreateComparer<decimal>());
        ComparersCache.TryAdd(typeof(char), CreateComparer<char>());
        ComparersCache.TryAdd(typeof(string), CreateComparer<string>());
        ComparersCache.TryAdd(typeof(DateTime), CreateComparer<DateTime>());
        ComparersCache.TryAdd(typeof(DateOnly), CreateComparer<DateOnly>());
    }

    /// <summary>
    /// Retrieves a comparer for the specified type from the cache, or creates one if it doesn't exist.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Func<object, object, bool> GetComparer(Type type)
    {
        return ComparersCache.GetOrAdd(type, t => CreateComparerDynamic(t));
    }

    /// <summary>
    /// Dynamically creates a comparer for the specified type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static Func<object, object, bool> CreateComparerDynamic(Type type)
    {
        // Reflect and invoke the generic CreateComparer<T> method for the given type.
        var method = typeof(GlobalComparerCache).GetMethod(nameof(CreateComparer), BindingFlags.Static | BindingFlags.NonPublic)!;
        var genericMethod = method.MakeGenericMethod(type);
        return (Func<object, object, bool>)genericMethod.Invoke(null, null)!;
    }

    /// <summary>
    /// Creates a comparer for the specified type T.
    /// </summary>
    /// <remarks>
    /// This method compiles a delegate that casts two objects to T, and then uses EqualityComparer&lt;T&gt;.Default.Equals to compare them.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static Func<object, object, bool> CreateComparer<T>()
    {
        var paramA = Expression.Parameter(typeof(object), "a");
        var paramB = Expression.Parameter(typeof(object), "b");
        var convertA = Expression.Convert(paramA, typeof(T));
        var convertB = Expression.Convert(paramB, typeof(T));
        var defaultComparer = Expression.Property(null, typeof(EqualityComparer<T>), "Default");
        var equalsCall = Expression.Call(defaultComparer, nameof(EqualityComparer<T>.Equals), Type.EmptyTypes, convertA, convertB);
        var lambda = Expression.Lambda<Func<object, object, bool>>(equalsCall, paramA, paramB);
        return lambda.Compile();
    }
}
