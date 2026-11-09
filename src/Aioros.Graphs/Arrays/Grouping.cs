using System.Buffers;

namespace Aioros.Graphs.Arrays;

/// <summary>A group of <see cref="Grouping.Group{TKey}"/>: a key and the indices of the items that have it, in order.</summary>
/// <typeparam name="TKey">The key.</typeparam>
/// <param name="Key">The key.</param>
/// <param name="Indices">The indices of the items with this key, ascending.</param>
public readonly record struct KeyGroup<TKey>(TKey Key, int[] Indices);

/// <summary>A reduced group of <see cref="Grouping.Rollup{TKey, TReducer}"/>.</summary>
/// <typeparam name="TKey">The key.</typeparam>
/// <param name="Key">The key.</param>
/// <param name="Value">The group's reduced value.</param>
public readonly record struct KeyValue<TKey>(TKey Key, double Value);

/// <summary>
/// Groups items by key, as d3-array's <c>group</c>, <c>rollup</c>, <c>flatGroup</c>, <c>flatRollup</c> and
/// <c>index</c>, with one level of keys or several: the groups come in the order their keys first appear, keys are
/// compared as JavaScript's <c>Map</c> compares them (NaN equals NaN, -0 is 0), and null is a key like any other. Items
/// are given as the span of their keys; the groups hold their indices.
/// </summary>
public static partial class Grouping
{
    /// <summary>Groups the indices of <paramref name="keys"/> by key, as d3's <c>group</c>.</summary>
    /// <typeparam name="TKey">The key.</typeparam>
    /// <param name="keys">The key of each item.</param>
    /// <returns>The groups in the order of their keys' first appearance.</returns>
    public static KeyGroup<TKey>[] Group<TKey>(ReadOnlySpan<TKey> keys)
    {
        var (order, members) = Collect(keys);
        var groups = new KeyGroup<TKey>[order.Count];
        for (var i = 0; i < groups.Length; i++)
        {
            groups[i] = new KeyGroup<TKey>(order[i], [.. members[i]]);
        }

        return groups;
    }

    /// <summary>Groups <paramref name="values"/> by key and reduces each group, as d3's <c>rollup</c>.</summary>
    /// <typeparam name="TKey">The key.</typeparam>
    /// <typeparam name="TReducer">The reducer, such as <see cref="SumReducer"/>.</typeparam>
    /// <param name="keys">The key of each item.</param>
    /// <param name="values">The value of each item, as many as the keys.</param>
    /// <param name="reducer">The reducer.</param>
    /// <returns>The reduced groups in the order of their keys' first appearance.</returns>
    /// <exception cref="ArgumentException">The values and the keys differ in length.</exception>
    public static KeyValue<TKey>[] Rollup<TKey, TReducer>(ReadOnlySpan<TKey> keys, ReadOnlySpan<double> values, TReducer reducer)
        where TReducer : struct, IGroupReducer
    {
        if (values.Length != keys.Length)
        {
            throw new ArgumentException("There must be one value per key.", nameof(values));
        }

        var (order, members) = Collect(keys);
        var buffer = ArrayPool<double>.Shared.Rent(Math.Max(1, values.Length));
        try
        {
            var groups = new KeyValue<TKey>[order.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var indices = members[i];
                for (var k = 0; k < indices.Count; k++)
                {
                    buffer[k] = values[indices[k]];
                }

                groups[i] = new KeyValue<TKey>(order[i], reducer.Reduce(buffer.AsSpan(0, indices.Count)));
            }

            return groups;
        }
        finally
        {
            ArrayPool<double>.Shared.Return(buffer);
        }
    }

    private static (List<TKey> Order, List<List<int>> Members) Collect<TKey>(ReadOnlySpan<TKey> keys)
    {
        var index = new Dictionary<MapKey<TKey>, int>();
        var order = new List<TKey>();
        var members = new List<List<int>>();
        for (var i = 0; i < keys.Length; i++)
        {
            var key = MapKey<TKey>.Normalize(keys[i]);
            if (!index.TryGetValue(new MapKey<TKey>(key), out var group))
            {
                group = Add(order, members, key);
                index[new MapKey<TKey>(key)] = group;
            }

            members[group].Add(i);
        }

        return (order, members);
    }

    private static int Add<TKey>(List<TKey> order, List<List<int>> members, TKey key)
    {
        order.Add(key);
        members.Add([]);
        return order.Count - 1;
    }
}

internal readonly record struct MapKey<TKey>(TKey Value)
{
    public static TKey Normalize(TKey key) => key is double d && d == 0 ? (TKey)(object)0.0 : key;
}
