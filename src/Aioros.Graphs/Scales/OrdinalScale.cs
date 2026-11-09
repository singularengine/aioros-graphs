using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Scales;

/// <summary>
/// An ordinal scale, as d3-scale's <c>scaleOrdinal</c>: maps each key of its domain to the range value at the key's
/// index, cycling through the range. Keys compare as JavaScript's <c>Map</c> compares them (NaN equals NaN, -0 is 0).
/// d3 grows the domain when it maps a new key; this scale is immutable, so a new key either maps to the unknown value
/// set with <see cref="WithUnknown"/> or is added with <see cref="Extend"/>, which returns the grown scale.
/// </summary>
/// <typeparam name="TKey">The key.</typeparam>
/// <typeparam name="TValue">The range value, such as a color.</typeparam>
public sealed class OrdinalScale<TKey, TValue>
{
    private readonly TKey[] domain;
    private readonly TValue[] range;
    private readonly Dictionary<MapKey<TKey>, int> index;
    private readonly TValue unknown;

    private OrdinalScale(TKey[] domain, Dictionary<MapKey<TKey>, int> index, TValue[] range, bool hasUnknown, TValue unknown)
    {
        this.domain = domain;
        this.index = index;
        this.range = range;
        HasUnknown = hasUnknown;
        this.unknown = unknown;
    }

    /// <summary>The domain's keys, without repetitions, in the order they were first given.</summary>
    public ReadOnlySpan<TKey> Domain => domain;

    /// <summary>The range's values.</summary>
    public ReadOnlySpan<TValue> Range => range;

    /// <summary>Whether keys outside the domain map to an unknown value instead of being rejected.</summary>
    public bool HasUnknown { get; }

    /// <summary>Creates a scale from <paramref name="domain"/> to <paramref name="range"/>.</summary>
    /// <param name="domain">The keys; repetitions are ignored.</param>
    /// <param name="range">The values, cycled when there are fewer than keys.</param>
    /// <returns>The scale.</returns>
    public static OrdinalScale<TKey, TValue> Create(ReadOnlySpan<TKey> domain, ReadOnlySpan<TValue> range)
    {
        var index = new Dictionary<MapKey<TKey>, int>();
        var keys = new List<TKey>();
        Add(index, keys, domain);
        return new OrdinalScale<TKey, TValue>([.. keys], index, range.ToArray(), false, default!);
    }

    /// <summary>Returns this scale with keys outside the domain mapping to <paramref name="value"/>, as d3's <c>unknown(value)</c>.</summary>
    /// <param name="value">The value for unknown keys.</param>
    /// <returns>The new scale.</returns>
    public OrdinalScale<TKey, TValue> WithUnknown(TValue value) => new(domain, index, range, true, value);

    /// <summary>Returns this scale with the keys not yet in its domain appended in order, as d3's implicit domain grows when it maps them.</summary>
    /// <param name="keys">The keys.</param>
    /// <returns>The new scale.</returns>
    public OrdinalScale<TKey, TValue> Extend(ReadOnlySpan<TKey> keys)
    {
        var grown = new Dictionary<MapKey<TKey>, int>(index);
        var all = new List<TKey>(domain);
        Add(grown, all, keys);
        return new OrdinalScale<TKey, TValue>([.. all], grown, range, HasUnknown, unknown);
    }

    /// <summary>Maps a key to its range value.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The value; the unknown value for a key outside the domain or when the range is empty.</returns>
    /// <exception cref="KeyNotFoundException">The key is outside the domain and the scale has no unknown value; add it with <see cref="Extend"/>.</exception>
    public TValue Map(TKey key) =>
        TryMap(key, out var value) ? value : throw new KeyNotFoundException("The key is not in the domain; extend the domain or set an unknown value.");

    /// <summary>Maps a key to its range value if the key is in the domain or the scale has an unknown value.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>Whether the key could be mapped.</returns>
    public bool TryMap(TKey key, out TValue value)
    {
        if (index.TryGetValue(new MapKey<TKey>(MapKey<TKey>.Normalize(key)), out var i))
        {
            value = range.Length > 0 ? range[i % range.Length] : unknown;
            return true;
        }

        value = unknown;
        return HasUnknown;
    }

    private static void Add(Dictionary<MapKey<TKey>, int> index, List<TKey> keys, ReadOnlySpan<TKey> added)
    {
        foreach (var key in added)
        {
            var normalized = MapKey<TKey>.Normalize(key);
            if (index.TryAdd(new MapKey<TKey>(normalized), keys.Count))
            {
                keys.Add(normalized);
            }
        }
    }
}
