using Aioros.Graphs.Scales;

namespace Aioros.Graphs.Tests.Scales;

public sealed class JsTrigTests
{
    [Fact]
    [Trait("AC", "NUMBER-07")]
    public async Task SineAndCosineMatchV8BitForBitAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("trig", "trig"))
        {
            var x = item.Number("x");
            GoldenFixture.Same(item.Number("sin"), JsTrig.Sin(x), $"sin({x:R})");
            GoldenFixture.Same(item.Number("cos"), JsTrig.Cos(x), $"cos({x:R})");
        }
    }
}
