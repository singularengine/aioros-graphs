using System.Text.Json;
using Aioros.Graphs.Chords;

namespace Aioros.Graphs.Tests.Chords;

public sealed class ChordTests
{
    [Fact]
    [Trait("AC", "CHORD-01")]
    public async Task ChordsMatchD3Async() => await CheckAsync("chords", directed: false, transpose: false);

    [Fact]
    [Trait("AC", "CHORD-02")]
    public async Task DirectedChordsMatchD3Async() => await CheckAsync("directed", directed: true, transpose: false);

    [Fact]
    [Trait("AC", "CHORD-03")]
    public async Task TransposedChordsMatchD3Async() => await CheckAsync("transpose", directed: false, transpose: true);

    [Fact]
    [Trait("AC", "CHORD-04")]
    public void MatricesOfTheWrongSizeAreRejected()
    {
        var options = new ChordOptions();

        Assert.Throws<ArgumentException>(() => ChordLayout.Compute([1, 2, 3], 2, options));
        Assert.Throws<ArgumentException>(() => ChordLayout.Compute([1, 2, 3, 4, 5], 2, options));
        Assert.Throws<ArgumentException>(() => ChordLayout.Compute([], -1, options));
    }

    private static async Task CheckAsync(string group, bool directed, bool transpose)
    {
        foreach (var item in await GoldenFixture.CasesAsync("chord", group))
        {
            var options = new ChordOptions
            {
                PadAngle = item.Number("padAngle"),
                SortGroups = Sort(item, "sortGroups"),
                SortSubgroups = Sort(item, "sortSubgroups"),
                SortChords = Sort(item, "sortChords"),
                Directed = directed,
                Transpose = transpose,
            };

            var layout = ChordLayout.Compute(item.Numbers("matrix"), (int)item.Number("n"), options);

            var groups = item.GetProperty("groups").EnumerateArray().ToList();
            var chords = item.GetProperty("chords").EnumerateArray().ToList();
            var because = $"{group} {item.Text("name")} {options}";
            Assert.True(groups.Count == layout.Groups.Length, $"{because}: {layout.Groups.Length} groups");
            Assert.True(chords.Count == layout.Chords.Length, $"{because}: {layout.Chords.Length} chords");
            for (var i = 0; i < groups.Count; i++)
            {
                Same(groups[i].Numbers(), 0, layout.Groups[i], $"{because} group {i}");
            }

            for (var i = 0; i < chords.Count; i++)
            {
                var expected = chords[i].Numbers();
                Same(expected, 0, layout.Chords[i].Source, $"{because} chord {i} source");
                Same(expected, 4, layout.Chords[i].Target, $"{because} chord {i} target");
            }
        }
    }

    private static void Same(double[] expected, int offset, ChordGroup actual, string because) =>
        GoldenFixture.Same(expected[offset..(offset + 4)], [actual.Index, actual.StartAngle, actual.EndAngle, actual.Value], because);

    private static ChordSort Sort(JsonElement item, string property) => item.Text(property) switch
    {
        "ascending" => ChordSort.Ascending,
        "descending" => ChordSort.Descending,
        _ => ChordSort.None,
    };
}
