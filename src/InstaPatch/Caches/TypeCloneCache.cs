using System.Linq.Expressions;
using System.Reflection;

namespace InstaPatch.Caches;

/// <summary>
/// Caches clone methods for a given type.
/// </summary>
/// <typeparam name="T"></typeparam>
internal static class TypeCloneCache<T>
{
    private static readonly Func<T, T>? Clone = null;

    static TypeCloneCache()
    {
        if (PropertySetterCache<T>.Any())
        {
            var type = typeof(T);

            var method = type.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null)
            {
                var sourceParam = Expression.Parameter(type, "source");
                var castedSource = Expression.Convert(sourceParam, type);
                var caller = Expression.Call(castedSource, method);
                var convert = Expression.Convert(caller, type);
                var lambda = Expression.Lambda<Func<T, T>>(convert, sourceParam).Compile();
                Clone = lambda;
            }
        }
    }

    /// <summary>
    /// Tries to create a clone of the original object.
    /// </summary>
    /// <param name="original"></param>
    /// <param name="clone"></param>
    /// <returns></returns>
    public static bool TryCreateClone(T original, out T clone)
    {
        if (Clone == null)
        {
            clone = default!;
            return false;
        }

        try
        {
            clone = Clone(original);
            return true;
        }
        catch
        {
            clone = default!;
            return false;
        }
    }
}
