namespace Aioros.Graphs.Arrays;

/// <summary>Reads the numeric key of an item, for the searches of <see cref="Bisector"/>.</summary>
/// <typeparam name="T">The item.</typeparam>
public interface IKeySelector<in T>
{
    /// <summary>Returns the key of <paramref name="item"/>.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The key.</returns>
    double Key(T item);
}

/// <summary>Binary search in items ascending by a key, as d3-array's <c>bisector(accessor)</c>; the selector is a struct, so each search is specialized for it.</summary>
public static class Bisector
{
    /// <summary>Returns the first index at which an item with key <paramref name="x"/> could be inserted, before equal keys.</summary>
    /// <typeparam name="T">The item.</typeparam>
    /// <typeparam name="TSelector">The key selector.</typeparam>
    /// <param name="items">The items, ascending by key.</param>
    /// <param name="x">The key to place.</param>
    /// <param name="selector">The key selector.</param>
    /// <returns>The index, or the length when <paramref name="x"/> is NaN.</returns>
    public static int Left<T, TSelector>(ReadOnlySpan<T> items, double x, TSelector selector)
        where TSelector : struct, IKeySelector<T> => Left(items, x, selector, 0, items.Length);

    /// <summary>Returns the last index at which an item with key <paramref name="x"/> could be inserted, after equal keys.</summary>
    /// <typeparam name="T">The item.</typeparam>
    /// <typeparam name="TSelector">The key selector.</typeparam>
    /// <param name="items">The items, ascending by key.</param>
    /// <param name="x">The key to place.</param>
    /// <param name="selector">The key selector.</param>
    /// <returns>The index, or the length when <paramref name="x"/> is NaN.</returns>
    public static int Right<T, TSelector>(ReadOnlySpan<T> items, double x, TSelector selector)
        where TSelector : struct, IKeySelector<T>
    {
        var lo = 0;
        var hi = items.Length;
        if (lo < hi && double.IsNaN(x))
        {
            return hi;
        }

        while (lo < hi)
        {
            var mid = (int)((uint)(lo + hi) >> 1);
            if (selector.Key(items[mid]) <= x)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    /// <summary>Returns the index of the item whose key is closest to <paramref name="x"/>, the lower one on ties.</summary>
    /// <typeparam name="T">The item.</typeparam>
    /// <typeparam name="TSelector">The key selector.</typeparam>
    /// <param name="items">The items, ascending by key.</param>
    /// <param name="x">The key to look for.</param>
    /// <param name="selector">The key selector.</param>
    /// <returns>The index.</returns>
    public static int Center<T, TSelector>(ReadOnlySpan<T> items, double x, TSelector selector)
        where TSelector : struct, IKeySelector<T>
    {
        var i = Left(items, x, selector, 0, items.Length - 1);
        return i > 0 && selector.Key(items[i - 1]) - x > -(selector.Key(items[i]) - x) ? i - 1 : i;
    }

    private static int Left<T, TSelector>(ReadOnlySpan<T> items, double x, TSelector selector, int lo, int hi)
        where TSelector : struct, IKeySelector<T>
    {
        if (lo < hi && double.IsNaN(x))
        {
            return hi;
        }

        while (lo < hi)
        {
            var mid = (int)((uint)(lo + hi) >> 1);
            if (selector.Key(items[mid]) < x)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }
}
