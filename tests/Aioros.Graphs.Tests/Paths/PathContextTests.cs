using System.Buffers;
using System.Text.Json;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Paths;

public sealed class PathContextTests
{
    [Fact]
    [Trait("AC", "PATH-01")]
    public async Task ProgramsWriteTheSamePathDataAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("path", "programs"))
        {
            var digits = item.OptionalNumber("digits") is { } d ? (int?)d : null;
            var commands = item.GetProperty("commands").EnumerateArray().ToList();

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) =>
            {
                var context = new PathContext<SvgPathWriter>(ref writer);
                foreach (var command in commands)
                {
                    Run(ref context, command);
                }
            }, digits);

            Assert.True(item.Text("path") == actual, $"{item.Text("name")} ({digits}): expected {item.Text("path")}, got {actual}");
        }
    }

    [Fact]
    [Trait("AC", "PATH-02")]
    public void NegativeRadiiAreRejected()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var context = new PathContext<SvgPathWriter>(ref writer);
            context.Arc(0, 0, -1, 0, 1);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var context = new PathContext<SvgPathWriter>(ref writer);
            context.MoveTo(0, 0);
            buffer.Clear();
            context.ArcTo(1, 1, 2, 0, -1);
        });
        Assert.Equal(0, buffer.WrittenCount);
    }

    [Fact]
    [Trait("AC", "PATH-03")]
    public void DigitsOutsideTheSupportedRangeAreRejected()
    {
        var buffer = new ArrayBufferWriter<byte>();

        Assert.Throws<ArgumentOutOfRangeException>(() => new SvgPathWriter(buffer, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SvgPathWriter(buffer, 16));
    }

    private static void Run(ref PathContext<SvgPathWriter> context, JsonElement command)
    {
        var a = command.EnumerateArray().Skip(1).Where(e => e.ValueKind is JsonValueKind.Number or JsonValueKind.String).Select(GoldenFixture.Number).ToArray();
        switch (command[0].GetString())
        {
            case "moveTo": context.MoveTo(a[0], a[1]); break;
            case "lineTo": context.LineTo(a[0], a[1]); break;
            case "quadraticCurveTo": context.QuadraticCurveTo(a[0], a[1], a[2], a[3]); break;
            case "bezierCurveTo": context.BezierCurveTo(a[0], a[1], a[2], a[3], a[4], a[5]); break;
            case "arc": context.Arc(a[0], a[1], a[2], a[3], a[4], command.GetArrayLength() > 6 && command[6].GetBoolean()); break;
            case "arcTo": context.ArcTo(a[0], a[1], a[2], a[3], a[4]); break;
            case "rect": context.Rect(a[0], a[1], a[2], a[3]); break;
            case "closePath": context.ClosePath(); break;
            default: throw new InvalidOperationException(command.ToString());
        }
    }
}
