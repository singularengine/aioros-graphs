using System.Globalization;
using Aioros.Graphs.Shapes;

namespace Aioros.Graphs.Tests.Shapes;

internal static class CurveNames
{
    public static Curve Parse(string name)
    {
        var parts = name.Split(':');
        double? parameter = parts.Length > 1 ? double.Parse(parts[1], CultureInfo.InvariantCulture) : null;
        return parts[0] switch
        {
            "linear" => Curve.Linear,
            "monotoneX" => Curve.MonotoneX,
            "monotoneY" => Curve.MonotoneY,
            "natural" => Curve.Natural,
            "step" => Curve.Step,
            "stepBefore" => Curve.StepBefore,
            "stepAfter" => Curve.StepAfter,
            "basis" => Curve.Basis,
            "cardinal" => Curve.Cardinal(parameter ?? 0),
            "catmullRom" => Curve.CatmullRom(parameter ?? 0.5),
            "linearClosed" => Curve.LinearClosed,
            "basisClosed" => Curve.BasisClosed,
            "basisOpen" => Curve.BasisOpen,
            "bundle" => Curve.Bundle(parameter ?? 0.85),
            "cardinalClosed" => Curve.CardinalClosed(parameter ?? 0),
            "cardinalOpen" => Curve.CardinalOpen(parameter ?? 0),
            "catmullRomClosed" => Curve.CatmullRomClosed(parameter ?? 0.5),
            "catmullRomOpen" => Curve.CatmullRomOpen(parameter ?? 0.5),
            "bumpX" => Curve.BumpX,
            "bumpY" => Curve.BumpY,
            _ => throw new InvalidOperationException(name),
        };
    }
}
