using System.Collections.Immutable;

namespace Aioros.Graphs.Formatting;

/// <summary>
/// A d3-format locale definition (<c>formatLocale</c>): the decimal mark, the thousands separator and its grouping, the
/// currency, the numerals, and the percent, minus and NaN texts. <see cref="Get"/> returns the definitions d3 ships.
/// </summary>
public sealed class NumberLocale
{
    private static ImmutableDictionary<string, NumberLocale> loaded = ImmutableDictionary.Create<string, NumberLocale>(StringComparer.Ordinal);

    private readonly int[] grouping;
    private readonly string[]? numerals;

    /// <summary>Creates a locale from d3's definition.</summary>
    /// <param name="decimalMark">The decimal mark.</param>
    /// <param name="thousands">The thousands separator, or null for no grouping.</param>
    /// <param name="grouping">The group sizes from the right; the last one repeats. Empty for no grouping.</param>
    /// <param name="currencyPrefix">The text before a currency amount.</param>
    /// <param name="currencySuffix">The text after a currency amount.</param>
    /// <param name="numerals">The ten digits, or null for 0 to 9.</param>
    /// <param name="percent">The percent sign.</param>
    /// <param name="minus">The minus sign.</param>
    /// <param name="nan">The text of NaN.</param>
    /// <exception cref="ArgumentException">The numerals are not ten.</exception>
    public NumberLocale(string decimalMark, string? thousands, int[] grouping, string currencyPrefix, string currencySuffix, string[]? numerals = null, string percent = "%", string minus = "−", string nan = "NaN")
    {
        ArgumentNullException.ThrowIfNull(decimalMark);
        ArgumentNullException.ThrowIfNull(grouping);
        ArgumentNullException.ThrowIfNull(currencyPrefix);
        ArgumentNullException.ThrowIfNull(currencySuffix);
        ArgumentNullException.ThrowIfNull(percent);
        ArgumentNullException.ThrowIfNull(minus);
        ArgumentNullException.ThrowIfNull(nan);
        if (numerals is { Length: not 10 })
        {
            throw new ArgumentException("Expected ten numerals.", nameof(numerals));
        }

        DecimalMark = decimalMark;
        Thousands = thousands;
        this.grouping = [.. grouping];
        CurrencyPrefix = currencyPrefix;
        CurrencySuffix = currencySuffix;
        this.numerals = numerals is null ? null : [.. numerals];
        Percent = percent;
        Minus = minus;
        Nan = nan;
        Groups = thousands is not null && grouping.Length > 0;
    }

    /// <summary>Gets d3's default locale, en-US: <c>.</c>, <c>,</c> by three, <c>$</c>, U+2212 minus.</summary>
    public static NumberLocale EnUs { get; } = new(".", ",", [3], "$", "");

    /// <summary>Gets the names of the locales d3 ships, such as <c>es-MX</c>.</summary>
    public static IReadOnlyList<string> Names => NumberLocales.Names;

    /// <summary>Gets the decimal mark.</summary>
    public string DecimalMark { get; }

    /// <summary>Gets the thousands separator, or null for no grouping.</summary>
    public string? Thousands { get; }

    /// <summary>Gets the group sizes from the right; the last one repeats.</summary>
    public IReadOnlyList<int> Grouping => grouping;

    /// <summary>Gets the text before a currency amount.</summary>
    public string CurrencyPrefix { get; }

    /// <summary>Gets the text after a currency amount.</summary>
    public string CurrencySuffix { get; }

    /// <summary>Gets the ten digits, or null for 0 to 9.</summary>
    public IReadOnlyList<string>? Numerals => numerals;

    /// <summary>Gets the percent sign.</summary>
    public string Percent { get; }

    /// <summary>Gets the minus sign.</summary>
    public string Minus { get; }

    /// <summary>Gets the text of NaN.</summary>
    public string Nan { get; }

    internal bool Groups { get; }

    internal int[] GroupSizes => grouping;

    internal string[]? Digits => numerals;

    /// <summary>Returns the locale d3 ships under <paramref name="name"/>.</summary>
    /// <param name="name">The name, such as <c>fr-FR</c>.</param>
    /// <returns>The locale.</returns>
    /// <exception cref="ArgumentException">d3 ships no locale of that name.</exception>
    public static NumberLocale Get(string name) =>
        TryGet(name, out var locale) ? locale : throw new ArgumentException($"Unknown locale: {name}", nameof(name));

    /// <summary>Looks up the locale d3 ships under <paramref name="name"/>.</summary>
    /// <param name="name">The name, such as <c>fr-FR</c>.</param>
    /// <param name="locale">The locale, when found.</param>
    /// <returns>Whether d3 ships it.</returns>
    public static bool TryGet(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out NumberLocale? locale)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (loaded.TryGetValue(name, out locale))
        {
            return true;
        }

        locale = NumberLocales.Create(name);
        if (locale is null)
        {
            return false;
        }

        locale = ImmutableInterlocked.GetOrAdd(ref loaded, name, locale);
        return true;
    }
}
