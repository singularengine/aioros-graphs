using System.Text.Json;
using Aioros.Graphs.Colors;
using Aioros.Graphs.Interpolation;

namespace Aioros.Graphs.Tests.Interpolation;

public sealed class ValueInterpolatorTests
{
    [Fact]
    [Trait("AC", "INTERP-12")]
    public async Task StringsInterpolateAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "string"))
        {
            var interpolator = new StringInterpolator(item.Text("a"), item.Text("b"));
            var ts = item.Numbers("ts");
            var outputs = item.GetProperty("outputs").EnumerateArray().Select(e => e.GetString()).ToList();
            var span = new char[interpolator.MaxLength];
            for (var i = 0; i < ts.Length; i++)
            {
                Assert.True(outputs[i] == interpolator.Interpolate(ts[i]), $"{item} at {ts[i]}: got {interpolator.Interpolate(ts[i])}");
                Assert.True(interpolator.TryInterpolate(ts[i], span, out var written));
                Assert.Equal(outputs[i], new string(span, 0, written));
            }
        }

        var path = new StringInterpolator("M0,0L10,10C1,2,3,4,5,6", "M5,5L20,30C7,8,9,10,11,12");
        Span<char> buffer = stackalloc char[path.MaxLength];
        var total = Run(path, buffer);
        var before = GC.GetAllocatedBytesForCurrentThread();
        total += Run(path, buffer);
        Assert.True(total > 0);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    [Trait("AC", "INTERP-13")]
    public async Task DatesInterpolateAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("interpolate", "date"))
        {
            var interpolator = new DateInterpolator(item.Number("a"), item.Number("b"));
            var ts = item.Numbers("ts");
            GoldenFixture.Same(item.Numbers("values"), [.. ts.Select(interpolator.Interpolate)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "INTERP-14")]
    public async Task ValuesInterpolateAsD3Async()
    {
        var cases = (await GoldenFixture.CasesAsync("interpolate", "value")).Where(c => !IsNested(c)).ToList();
        Assert.NotEmpty(cases);
        foreach (var item in cases)
        {
            CheckCase(item);
        }
    }

    [Fact]
    [Trait("AC", "INTERP-15")]
    public async Task ArraysAndObjectsInterpolateAsD3Async()
    {
        var cases = (await GoldenFixture.CasesAsync("interpolate", "value")).Where(IsNested).ToList();
        Assert.NotEmpty(cases);
        foreach (var item in cases)
        {
            CheckCase(item);
        }
    }

    private static void CheckCase(JsonElement item)
    {
        var a = Value(item.GetProperty("a"));
        var b = Value(item.GetProperty("b"));
        if (item.TryGetProperty("error", out _))
        {
            Assert.Throws<ArgumentException>(() => Interpolators.Value(a, b));
            return;
        }

        var interpolator = Interpolators.Value(a, b);
        var ts = item.Numbers("ts");
        var outputs = item.GetProperty("outputs").EnumerateArray().Select(Value).ToList();
        for (var i = 0; i < ts.Length; i++)
        {
            var actual = interpolator.Interpolate(ts[i]);
            Assert.True(outputs[i] == actual, $"{item.GetProperty("a")} → {item.GetProperty("b")} at {ts[i]}: expected {item.GetProperty("outputs")[i]}, got {actual.Kind} {actual}");
        }
    }

    private static bool IsNested(JsonElement item) => item.GetProperty("b")[0].GetString() is "a" or "o";

    private static InterpolationValue Value(JsonElement tag) => tag[0].GetString() switch
    {
        "u" => InterpolationValue.Undefined,
        "null" => InterpolationValue.Null,
        "b" => InterpolationValue.FromBoolean(tag[1].GetBoolean()),
        "n" => InterpolationValue.FromNumber(tag[1].Number()),
        "s" => InterpolationValue.FromString(tag[1].GetString()!),
        "d" => InterpolationValue.FromDate(tag[1].Number()),
        "f" => InterpolationValue.FromNumbers(tag[1].Numbers()),
        "a" => InterpolationValue.FromArray([.. tag[1].EnumerateArray().Select(Value)]),
        "c" => InterpolationValue.FromColor(new Color(tag[1].GetString() == "hsl" ? ColorSpace.Hsl : ColorSpace.Rgb, tag[2].Number(), tag[3].Number(), tag[4].Number(), tag[5].Number())),
        _ => InterpolationValue.FromObject(tag[1].EnumerateArray().Select(m => new KeyValuePair<string, InterpolationValue>(m[0].GetString()!, Value(m[1])))),
    };

    private static int Run(StringInterpolator interpolator, Span<char> buffer)
    {
        var total = 0;
        for (var i = 0; i < 1000; i++)
        {
            interpolator.TryInterpolate(i / 999.0, buffer, out var written);
            total += written;
        }

        return total;
    }
}
