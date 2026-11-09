using System.Text.Json;
using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Tests.Arrays;

public sealed class NestedGroupingTests
{
    [Fact]
    [Trait("AC", "ARRAY-17")]
    public async Task NestedGroupsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "nestedGroup"))
        {
            if (IsNumeric(item))
            {
                AssertGroups(item.GetProperty("groups"), Grouping.GroupNested(NumberLevels(item)), e => e.Number(), item.ToString());
            }
            else
            {
                AssertGroups(item.GetProperty("groups"), Grouping.GroupNested(StringLevels(item)), Text, item.ToString());
            }
        }

        Assert.Throws<ArgumentException>(() => Grouping.GroupNested<string>());
        string[] two = ["a", "b"];
        string[] one = ["c"];
        Assert.Throws<ArgumentException>(() => Grouping.GroupNested(two, one));
    }

    [Fact]
    [Trait("AC", "ARRAY-18")]
    public async Task NestedRollupsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "nestedRollup"))
        {
            var values = item.Numbers("values");
            var reducer = item.Text("reducer");
            if (IsNumeric(item))
            {
                AssertValues(item.GetProperty("groups"), Rollup(values, reducer, NumberLevels(item)), e => e.Number(), item.ToString());
            }
            else
            {
                AssertValues(item.GetProperty("groups"), Rollup(values, reducer, StringLevels(item)), Text, item.ToString());
            }
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-19")]
    public async Task FlatGroupsAndRollupsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "nestedGroup"))
        {
            var expected = item.GetProperty("flat").EnumerateArray().ToList();
            var actual = IsNumeric(item)
                ? Grouping.FlatGroup(NumberLevels(item)).Select(r => (Keys: r.Keys.Select(k => (object?)k).ToList(), r.Indices)).ToList()
                : Grouping.FlatGroup(StringLevels(item)).Select(r => (Keys: r.Keys.Select(k => (object?)k).ToList(), r.Indices)).ToList();
            Assert.Equal(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                Assert.Equal(Keys(expected[i], IsNumeric(item)), actual[i].Keys);
                Assert.Equal(expected[i].GetProperty("members").EnumerateArray().Select(e => e.GetInt32()), actual[i].Indices);
            }
        }

        foreach (var item in await GoldenFixture.CasesAsync("array", "nestedRollup"))
        {
            var values = item.Numbers("values");
            var expected = item.GetProperty("flat").EnumerateArray().ToList();
            var actual = IsNumeric(item)
                ? FlatRollup(values, item.Text("reducer"), NumberLevels(item)).Select(r => (Keys: r.Keys.Select(k => (object?)k).ToList(), r.Value)).ToList()
                : FlatRollup(values, item.Text("reducer"), StringLevels(item)).Select(r => (Keys: r.Keys.Select(k => (object?)k).ToList(), r.Value)).ToList();
            Assert.Equal(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                Assert.Equal(Keys(expected[i], IsNumeric(item)), actual[i].Keys);
                GoldenFixture.Same(expected[i].Number("value"), actual[i].Value, item.ToString());
            }
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-20")]
    public async Task IndexMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "index"))
        {
            var keys = item.GetProperty("keys").EnumerateArray().ToList();
            var numeric = keys.Any(k => k.ValueKind == JsonValueKind.Number);
            if (item.OptionalText("error") is { } error)
            {
                var thrown = numeric
                    ? Assert.Throws<InvalidOperationException>(() => Grouping.Index<double>([.. keys.Select(k => k.Number())]))
                    : Assert.Throws<InvalidOperationException>(() => Grouping.Index<string?>([.. keys.Select(Text)]));
                Assert.Equal(error, thrown.Message);
                continue;
            }

            var expected = item.GetProperty("index").EnumerateArray().ToList();
            var actual = numeric
                ? Grouping.Index<double>([.. keys.Select(k => k.Number())]).Select(e => ((object?)e.Key, e.Index)).ToList()
                : Grouping.Index<string?>([.. keys.Select(Text)]).Select(e => ((object?)e.Key, e.Index)).ToList();
            Assert.Equal(expected.Select(e => (numeric ? (object?)e.GetProperty("key").Number() : Text(e.GetProperty("key")), e.GetProperty("index").GetInt32())), actual);
        }
    }

    [Fact]
    [Trait("AC", "ARRAY-21")]
    public async Task FullPrecisionRunningSumsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "fcumsum"))
        {
            var values = item.Numbers("values");
            var sums = new double[values.Length];
            Statistics.FCumulativeSum(values, sums);
            GoldenFixture.Same(item.Numbers("fcumsum"), sums, item.ToString());

            var adder = new Adder();
            var running = new double[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                adder.Add(double.IsNaN(values[i]) ? 0 : values[i]);
                running[i] = adder.Value;
            }

            GoldenFixture.Same(item.Numbers("adder"), running, item.ToString());
        }

        Assert.Throws<ArgumentException>(() => Statistics.FCumulativeSum([1, 2], new double[1]));
    }

    private static NestedValue<TKey>[] Rollup<TKey>(double[] values, string reducer, TKey[][] levels) => reducer switch
    {
        "sum" => Grouping.RollupNested<TKey, SumReducer>(values, default, levels),
        "count" => Grouping.RollupNested<TKey, CountReducer>(values, default, levels),
        _ => Grouping.RollupNested<TKey, MedianReducer>(values, default, levels),
    };

    private static FlatValue<TKey>[] FlatRollup<TKey>(double[] values, string reducer, TKey[][] levels) => reducer switch
    {
        "sum" => Grouping.FlatRollup<TKey, SumReducer>(values, default, levels),
        "count" => Grouping.FlatRollup<TKey, CountReducer>(values, default, levels),
        _ => Grouping.FlatRollup<TKey, MedianReducer>(values, default, levels),
    };

    private static bool IsNumeric(JsonElement item) =>
        item.GetProperty("levels").EnumerateArray().Any(l => l.EnumerateArray().Any(k => k.ValueKind == JsonValueKind.Number));

    private static double[][] NumberLevels(JsonElement item) => [.. item.GetProperty("levels").EnumerateArray().Select(l => l.Numbers())];

    private static string?[][] StringLevels(JsonElement item) => [.. item.GetProperty("levels").EnumerateArray().Select(l => l.EnumerateArray().Select(Text).ToArray())];

    private static string? Text(JsonElement e) => e.ValueKind == JsonValueKind.Null ? null : e.GetString();

    private static List<object?> Keys(JsonElement row, bool numeric) =>
        [.. row.GetProperty("keys").EnumerateArray().Select(k => numeric ? (object?)k.Number() : Text(k))];

    private static void AssertGroups<TKey>(JsonElement expected, NestedGroup<TKey>[] actual, Func<JsonElement, object?> key, string because)
    {
        var groups = expected.EnumerateArray().ToList();
        Assert.True(groups.Count == actual.Length, because);
        for (var i = 0; i < groups.Count; i++)
        {
            Assert.Equal(key(groups[i].GetProperty("key")), actual[i].Key);
            if (groups[i].TryGetProperty("children", out var children))
            {
                AssertGroups(children, actual[i].Children, key, because);
                Assert.Equal(actual[i].Children.SelectMany(c => c.Indices).Order(), actual[i].Indices);
            }
            else
            {
                Assert.Empty(actual[i].Children);
                Assert.Equal(groups[i].GetProperty("members").EnumerateArray().Select(e => e.GetInt32()), actual[i].Indices);
            }
        }
    }

    private static void AssertValues<TKey>(JsonElement expected, NestedValue<TKey>[] actual, Func<JsonElement, object?> key, string because)
    {
        var groups = expected.EnumerateArray().ToList();
        Assert.True(groups.Count == actual.Length, because);
        for (var i = 0; i < groups.Count; i++)
        {
            Assert.Equal(key(groups[i].GetProperty("key")), actual[i].Key);
            if (groups[i].TryGetProperty("children", out var children))
            {
                AssertValues(children, actual[i].Children, key, because);
            }
            else
            {
                GoldenFixture.Same(groups[i].Number("value"), actual[i].Value, because);
            }
        }
    }
}
