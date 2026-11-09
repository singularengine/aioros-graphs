using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Shapes;

public sealed class ArcPieStackTests
{
    [Fact]
    [Trait("AC", "SHAPE-05")]
    public async Task ArcsAndCentroidsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape", "arcs"))
        {
            var arc = new ArcShape(item.Number("innerRadius"), item.Number("outerRadius"), item.Number("startAngle"), item.Number("endAngle"))
            {
                PadAngle = item.OptionalNumber("padAngle") ?? 0,
                CornerRadius = item.OptionalNumber("cornerRadius") ?? 0,
                PadRadius = item.OptionalNumber("padRadius") ?? double.NaN,
            };
            int? digits = item.OptionalNumber("digits") is { } d ? (int)d : null;

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) => Arc.Write(ref writer, arc), digits);
            var (cx, cy) = Arc.Centroid(arc);

            if (digits is null)
            {
                Assert.True(GoldenFixture.SamePath(item.Text("path"), actual, 1e-12), $"{item}: got {actual}");
                Assert.True(GoldenFixture.SamePath(string.Join(",", item.Numbers("centroid")), $"{cx:R},{cy:R}", 1e-12), $"{item} centroid: got {cx:R},{cy:R}");
            }
            else
            {
                Assert.True(item.Text("path") == actual, $"{item}: got {actual}");
                GoldenFixture.Same(item.Numbers("centroid"), [cx, cy], $"{item} centroid");
            }
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-06")]
    public async Task PiesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape", "pies"))
        {
            var settings = item.GetProperty("settings");
            var sort = settings.OptionalText("sort") switch
            {
                "none" => PieSort.None,
                "ascending" => PieSort.Ascending,
                _ => PieSort.Descending,
            };
            var options = new PieOptions
            {
                Sort = sort,
                StartAngle = settings.OptionalNumber("startAngle") ?? 0,
                EndAngle = settings.OptionalNumber("endAngle") ?? 2 * Math.PI,
                PadAngle = settings.OptionalNumber("padAngle") ?? 0,
            };

            var slices = Pie.Compute(item.Numbers("input"), options);

            var expected = item.GetProperty("slices").EnumerateArray().ToList();
            Assert.Equal(expected.Count, slices.Length);
            for (var i = 0; i < expected.Count; i++)
            {
                var because = $"{item.Text("values")} {settings} [{i}]";
                Assert.Equal((int)expected[i].Number("index"), slices[i].Index);
                GoldenFixture.Same(expected[i].Number("value"), slices[i].Value, because);
                GoldenFixture.Same(expected[i].Number("startAngle"), slices[i].StartAngle, because);
                GoldenFixture.Same(expected[i].Number("endAngle"), slices[i].EndAngle, because);
                GoldenFixture.Same(expected[i].Number("padAngle"), slices[i].PadAngle, because);
            }
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-07")]
    public void ShortSliceSpansAreRejected()
    {
        var slices = new PieSlice[1];

        Assert.Throws<ArgumentException>(() => Pie.Compute([1, 2], slices, new PieOptions()));
    }

    [Fact]
    [Trait("AC", "SHAPE-08")]
    public async Task StacksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape", "stacks"))
        {
            var series = (int)item.Number("series");
            var rows = item.GetProperty("rows").EnumerateArray().Select(r => r.Numbers()).ToList();
            var values = rows.SelectMany(r => r).ToArray();
            var points = new StackPoint[series * rows.Count];
            var order = new int[series];
            var stackOrder = Enum.Parse<StackOrder>(item.Text("order"), ignoreCase: true);
            var offset = Enum.Parse<StackOffset>(item.Text("offset"), ignoreCase: true);

            Stacks.Compute(values, series, points, order, stackOrder, offset);

            var because = $"{item.Text("values")} {item.Text("order")} {item.Text("offset")}";
            Assert.Equal(item.Numbers("stackOrder").Select(v => (int)v), order);
            var lower = item.GetProperty("lower").EnumerateArray().Select(s => s.Numbers()).ToList();
            var upper = item.GetProperty("upper").EnumerateArray().Select(s => s.Numbers()).ToList();
            for (var s = 0; s < series; s++)
            {
                GoldenFixture.Same(lower[s], [.. points.Skip(s * rows.Count).Take(rows.Count).Select(p => p.Lower)], $"{because} lower {s}");
                GoldenFixture.Same(upper[s], [.. points.Skip(s * rows.Count).Take(rows.Count).Select(p => p.Upper)], $"{because} upper {s}");
            }
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-09")]
    public void InconsistentStackSpansAreRejected()
    {
        var points = new StackPoint[4];
        var order = new int[2];

        Assert.Throws<ArgumentException>(() => Stacks.Compute([1, 2, 3], 2, points, order, StackOrder.None, StackOffset.None));
        Assert.Throws<ArgumentException>(() => Stacks.Compute([1, 2, 3, 4], 2, new StackPoint[3], order, StackOrder.None, StackOffset.None));
        Assert.Throws<ArgumentException>(() => Stacks.Compute([1, 2, 3, 4], 2, points, new int[1], StackOrder.None, StackOffset.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => Stacks.Compute([1, 2, 3, 4], 0, points, order, StackOrder.None, StackOffset.None));
    }
}
