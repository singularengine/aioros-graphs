using System.Text.Json;
using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Tests.Arrays;

public sealed class GroupingTests
{
    [Fact]
    [Trait("AC", "ARRAY-14")]
    public async Task GroupsMatchD3Async()
    {
        var cases = await GoldenFixture.CasesAsync("array", "group");
        var strings = Grouping.Group<string?>(Strings(cases[0].GetProperty("keys")));
        var numbers = Grouping.Group<double>(cases[1].Numbers("keys"));

        AssertGroups(cases[0], strings.Select(g => ((object?)g.Key, g.Indices)).ToList(), e => e.ValueKind == JsonValueKind.Null ? null : e.GetString());
        AssertGroups(cases[1], numbers.Select(g => ((object?)g.Key, g.Indices)).ToList(), e => e.Number());
        Assert.False(double.IsNegative(numbers.Single(g => g.Key == 0).Key));
    }

    [Fact]
    [Trait("AC", "ARRAY-15")]
    public async Task RollupsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("array", "rollup"))
        {
            var keys = Strings(item.GetProperty("keys"));
            var values = item.Numbers("values");
            var groups = item.Text("reducer") switch
            {
                "sum" => Grouping.Rollup<string?, SumReducer>(keys, values, default),
                "mean" => Grouping.Rollup<string?, MeanReducer>(keys, values, default),
                "count" => Grouping.Rollup<string?, CountReducer>(keys, values, default),
                "min" => Grouping.Rollup<string?, MinReducer>(keys, values, default),
                "max" => Grouping.Rollup<string?, MaxReducer>(keys, values, default),
                _ => Grouping.Rollup<string?, MedianReducer>(keys, values, default),
            };
            var expected = item.GetProperty("groups").EnumerateArray().ToList();

            Assert.Equal(expected.Select(e => e.GetProperty("key").ValueKind == JsonValueKind.Null ? null : e.Text("key")), groups.Select(g => g.Key));
            GoldenFixture.Same(expected.Select(e => e.Number("value")).ToList(), groups.Select(g => g.Value).ToList(), item.ToString());
        }
    }

    private static string?[] Strings(JsonElement keys) =>
        [.. keys.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Null ? null : e.GetString())];

    private static void AssertGroups(JsonElement item, List<(object? Key, int[] Indices)> actual, Func<JsonElement, object?> key)
    {
        var expected = item.GetProperty("groups").EnumerateArray().ToList();
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(key(expected[i].GetProperty("key")), actual[i].Key);
            Assert.Equal(expected[i].GetProperty("members").EnumerateArray().Select(e => e.GetInt32()), actual[i].Indices);
        }
    }
}
