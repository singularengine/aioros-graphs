using System.Buffers;

namespace Aioros.Graphs.Arrays;

/// <summary>A group of <see cref="Grouping.GroupNested{TKey}"/>: a key, its items and its groups at the next level.</summary>
/// <typeparam name="TKey">The key.</typeparam>
/// <param name="Key">The key at this level.</param>
/// <param name="Indices">The indices of every item in the group, ascending.</param>
/// <param name="Children">The groups at the next level, empty at the innermost level.</param>
public readonly record struct NestedGroup<TKey>(TKey Key, int[] Indices, NestedGroup<TKey>[] Children);

/// <summary>A reduced group of <see cref="Grouping.RollupNested{TKey, TReducer}"/>.</summary>
/// <typeparam name="TKey">The key.</typeparam>
/// <param name="Key">The key at this level.</param>
/// <param name="Value">The reduced value at the innermost level, NaN above it.</param>
/// <param name="Children">The groups at the next level, empty at the innermost level.</param>
public readonly record struct NestedValue<TKey>(TKey Key, double Value, NestedValue<TKey>[] Children);

/// <summary>A row of <see cref="Grouping.FlatGroup{TKey}"/>: the keys of every level and the items.</summary>
/// <typeparam name="TKey">The key.</typeparam>
/// <param name="Keys">One key per level.</param>
/// <param name="Indices">The indices of the items with these keys, ascending.</param>
public readonly record struct FlatGroup<TKey>(TKey[] Keys, int[] Indices);

/// <summary>A row of <see cref="Grouping.FlatRollup{TKey, TReducer}"/>: the keys of every level and the reduced value.</summary>
/// <typeparam name="TKey">The key.</typeparam>
/// <param name="Keys">One key per level.</param>
/// <param name="Value">The reduced value.</param>
public readonly record struct FlatValue<TKey>(TKey[] Keys, double Value);

/// <summary>An entry of <see cref="Grouping.Index{TKey}"/>: a key and the index of its only item.</summary>
/// <typeparam name="TKey">The key.</typeparam>
/// <param name="Key">The key.</param>
/// <param name="Index">The index of the item with this key.</param>
public readonly record struct KeyIndex<TKey>(TKey Key, int Index);

public static partial class Grouping
{
    /// <summary>
    /// Groups the items by the keys of each level in turn, as d3's <c>group(data, ...keys)</c>: within each group, the
    /// groups of the next level come in the order their keys first appear.
    /// </summary>
    /// <typeparam name="TKey">The key.</typeparam>
    /// <param name="levels">The key of each item, one array per level, all of the same length.</param>
    /// <returns>The groups of the first level.</returns>
    /// <exception cref="ArgumentException">There is no level, or the levels differ in length.</exception>
    public static NestedGroup<TKey>[] GroupNested<TKey>(params ReadOnlySpan<TKey[]> levels)
    {
        var all = AllItems(levels);
        return GroupLevel(levels, 0, all);
    }

    /// <summary>Groups the items by the keys of each level and reduces each innermost group, as d3's <c>rollup(data, reduce, ...keys)</c>.</summary>
    /// <typeparam name="TKey">The key.</typeparam>
    /// <typeparam name="TReducer">The reducer, such as <see cref="SumReducer"/>.</typeparam>
    /// <param name="values">The value of each item.</param>
    /// <param name="reducer">The reducer.</param>
    /// <param name="levels">The key of each item, one array per level, as many as the values.</param>
    /// <returns>The reduced groups of the first level.</returns>
    /// <exception cref="ArgumentException">There is no level, or the levels and the values differ in length.</exception>
    public static NestedValue<TKey>[] RollupNested<TKey, TReducer>(ReadOnlySpan<double> values, TReducer reducer, params ReadOnlySpan<TKey[]> levels)
        where TReducer : struct, IGroupReducer
    {
        var all = AllItems(levels);
        if (values.Length != all.Length)
        {
            throw new ArgumentException("There must be one value per item.", nameof(values));
        }

        var buffer = ArrayPool<double>.Shared.Rent(Math.Max(1, values.Length));
        try
        {
            return RollupLevel(levels, 0, all, values, reducer, buffer);
        }
        finally
        {
            ArrayPool<double>.Shared.Return(buffer);
        }
    }

    /// <summary>Returns the innermost groups of <see cref="GroupNested{TKey}"/> as rows of keys, as d3's <c>flatGroup</c>.</summary>
    /// <typeparam name="TKey">The key.</typeparam>
    /// <param name="levels">The key of each item, one array per level, all of the same length.</param>
    /// <returns>The rows, depth first.</returns>
    /// <exception cref="ArgumentException">There is no level, or the levels differ in length.</exception>
    public static FlatGroup<TKey>[] FlatGroup<TKey>(params ReadOnlySpan<TKey[]> levels)
    {
        var rows = new List<FlatGroup<TKey>>();
        var keys = new TKey[levels.Length];
        Flatten(GroupNested(levels), keys, 0, rows);
        return [.. rows];
    }

    /// <summary>Returns the innermost groups of <see cref="RollupNested{TKey, TReducer}"/> as rows of keys, as d3's <c>flatRollup</c>.</summary>
    /// <typeparam name="TKey">The key.</typeparam>
    /// <typeparam name="TReducer">The reducer.</typeparam>
    /// <param name="values">The value of each item.</param>
    /// <param name="reducer">The reducer.</param>
    /// <param name="levels">The key of each item, one array per level, as many as the values.</param>
    /// <returns>The rows, depth first.</returns>
    /// <exception cref="ArgumentException">There is no level, or the levels and the values differ in length.</exception>
    public static FlatValue<TKey>[] FlatRollup<TKey, TReducer>(ReadOnlySpan<double> values, TReducer reducer, params ReadOnlySpan<TKey[]> levels)
        where TReducer : struct, IGroupReducer
    {
        var rows = new List<FlatValue<TKey>>();
        var keys = new TKey[levels.Length];
        Flatten(RollupNested(values, reducer, levels), keys, 0, rows);
        return [.. rows];
    }

    /// <summary>Maps each key to its only item, as d3's <c>index</c>, in the order of the keys.</summary>
    /// <typeparam name="TKey">The key.</typeparam>
    /// <param name="keys">The key of each item.</param>
    /// <returns>The entries.</returns>
    /// <exception cref="InvalidOperationException">A key appears more than once ("duplicate key", as d3 throws).</exception>
    public static KeyIndex<TKey>[] Index<TKey>(ReadOnlySpan<TKey> keys)
    {
        var (order, members) = Collect(keys);
        var entries = new KeyIndex<TKey>[order.Count];
        for (var i = 0; i < entries.Length; i++)
        {
            if (members[i].Count != 1)
            {
                throw new InvalidOperationException("duplicate key");
            }

            entries[i] = new KeyIndex<TKey>(order[i], members[i][0]);
        }

        return entries;
    }

    private static int[] AllItems<TKey>(ReadOnlySpan<TKey[]> levels)
    {
        if (levels.IsEmpty)
        {
            throw new ArgumentException("There must be at least one level of keys.", nameof(levels));
        }

        var n = levels[0]?.Length ?? throw new ArgumentException("A level is null.", nameof(levels));
        foreach (var level in levels)
        {
            if (level is null || level.Length != n)
            {
                throw new ArgumentException("Every level must have one key per item.", nameof(levels));
            }
        }

        var all = new int[n];
        for (var i = 0; i < n; i++)
        {
            all[i] = i;
        }

        return all;
    }

    private static (List<TKey> Order, List<List<int>> Members) Split<TKey>(TKey[] keys, int[] items)
    {
        var index = new Dictionary<MapKey<TKey>, int>();
        var order = new List<TKey>();
        var members = new List<List<int>>();
        foreach (var item in items)
        {
            var key = MapKey<TKey>.Normalize(keys[item]);
            if (!index.TryGetValue(new MapKey<TKey>(key), out var group))
            {
                group = Add(order, members, key);
                index[new MapKey<TKey>(key)] = group;
            }

            members[group].Add(item);
        }

        return (order, members);
    }

    private static NestedGroup<TKey>[] GroupLevel<TKey>(ReadOnlySpan<TKey[]> levels, int level, int[] items)
    {
        var (order, members) = Split(levels[level], items);
        var groups = new NestedGroup<TKey>[order.Count];
        for (var i = 0; i < groups.Length; i++)
        {
            int[] indices = [.. members[i]];
            groups[i] = new NestedGroup<TKey>(order[i], indices, level + 1 < levels.Length ? GroupLevel(levels, level + 1, indices) : []);
        }

        return groups;
    }

    private static NestedValue<TKey>[] RollupLevel<TKey, TReducer>(ReadOnlySpan<TKey[]> levels, int level, int[] items, ReadOnlySpan<double> values, TReducer reducer, double[] buffer)
        where TReducer : struct, IGroupReducer
    {
        var (order, members) = Split(levels[level], items);
        var groups = new NestedValue<TKey>[order.Count];
        for (var i = 0; i < groups.Length; i++)
        {
            var indices = members[i];
            if (level + 1 < levels.Length)
            {
                groups[i] = new NestedValue<TKey>(order[i], double.NaN, RollupLevel(levels, level + 1, [.. indices], values, reducer, buffer));
                continue;
            }

            for (var k = 0; k < indices.Count; k++)
            {
                buffer[k] = values[indices[k]];
            }

            groups[i] = new NestedValue<TKey>(order[i], reducer.Reduce(buffer.AsSpan(0, indices.Count)), []);
        }

        return groups;
    }

    private static void Flatten<TKey>(NestedGroup<TKey>[] groups, TKey[] keys, int level, List<FlatGroup<TKey>> rows)
    {
        foreach (var group in groups)
        {
            keys[level] = group.Key;
            if (level + 1 < keys.Length)
            {
                Flatten(group.Children, keys, level + 1, rows);
            }
            else
            {
                rows.Add(new FlatGroup<TKey>([.. keys], group.Indices));
            }
        }
    }

    private static void Flatten<TKey>(NestedValue<TKey>[] groups, TKey[] keys, int level, List<FlatValue<TKey>> rows)
    {
        foreach (var group in groups)
        {
            keys[level] = group.Key;
            if (level + 1 < keys.Length)
            {
                Flatten(group.Children, keys, level + 1, rows);
            }
            else
            {
                rows.Add(new FlatValue<TKey>([.. keys], group.Value));
            }
        }
    }
}
