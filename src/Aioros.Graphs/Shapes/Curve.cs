namespace Aioros.Graphs.Shapes;

/// <summary>The curves d3-shape draws lines and areas with.</summary>
public enum CurveKind
{
    /// <summary>Straight lines: <c>curveLinear</c>.</summary>
    Linear,

    /// <summary>A spline monotone in y: <c>curveMonotoneX</c>.</summary>
    MonotoneX,

    /// <summary>A spline monotone in x: <c>curveMonotoneY</c>.</summary>
    MonotoneY,

    /// <summary>A natural cubic spline: <c>curveNatural</c>.</summary>
    Natural,

    /// <summary>Steps changing midway: <c>curveStep</c>.</summary>
    Step,

    /// <summary>Steps changing at the previous point: <c>curveStepBefore</c>.</summary>
    StepBefore,

    /// <summary>Steps changing at the next point: <c>curveStepAfter</c>.</summary>
    StepAfter,

    /// <summary>A cubic basis spline: <c>curveBasis</c>.</summary>
    Basis,

    /// <summary>A cardinal spline with a tension: <c>curveCardinal</c>.</summary>
    Cardinal,

    /// <summary>A Catmull–Rom spline with an alpha: <c>curveCatmullRom</c>.</summary>
    CatmullRom,

    /// <summary>A closed polygon: <c>curveLinearClosed</c>.</summary>
    LinearClosed,

    /// <summary>A closed cubic basis spline: <c>curveBasisClosed</c>.</summary>
    BasisClosed,

    /// <summary>A cubic basis spline not through the first and last points: <c>curveBasisOpen</c>.</summary>
    BasisOpen,

    /// <summary>A basis spline straightened toward the line between its ends by a beta: <c>curveBundle</c>; lines only.</summary>
    Bundle,

    /// <summary>A closed cardinal spline with a tension: <c>curveCardinalClosed</c>.</summary>
    CardinalClosed,

    /// <summary>A cardinal spline not through the first and last points: <c>curveCardinalOpen</c>.</summary>
    CardinalOpen,

    /// <summary>A closed Catmull–Rom spline with an alpha: <c>curveCatmullRomClosed</c>.</summary>
    CatmullRomClosed,

    /// <summary>A Catmull–Rom spline not through the first and last points: <c>curveCatmullRomOpen</c>.</summary>
    CatmullRomOpen,

    /// <summary>Bézier bumps with horizontal tangents: <c>curveBumpX</c>.</summary>
    BumpX,

    /// <summary>Bézier bumps with vertical tangents: <c>curveBumpY</c>.</summary>
    BumpY,
}

/// <summary>A curve and its parameter, chosen at run time; <see cref="Line"/> and <see cref="Area"/> specialize for it.</summary>
/// <param name="Kind">The curve.</param>
/// <param name="Parameter">The tension of a cardinal curve, the alpha of a Catmull–Rom curve or the beta of a bundle; ignored otherwise.</param>
public readonly record struct Curve(CurveKind Kind, double Parameter = 0)
{
    /// <summary>Straight lines.</summary>
    public static Curve Linear => new(CurveKind.Linear);

    /// <summary>A spline monotone in y, for x ascending.</summary>
    public static Curve MonotoneX => new(CurveKind.MonotoneX);

    /// <summary>A spline monotone in x, for y ascending.</summary>
    public static Curve MonotoneY => new(CurveKind.MonotoneY);

    /// <summary>A natural cubic spline.</summary>
    public static Curve Natural => new(CurveKind.Natural);

    /// <summary>Steps changing midway between points.</summary>
    public static Curve Step => new(CurveKind.Step);

    /// <summary>Steps changing at the previous point.</summary>
    public static Curve StepBefore => new(CurveKind.StepBefore);

    /// <summary>Steps changing at the next point.</summary>
    public static Curve StepAfter => new(CurveKind.StepAfter);

    /// <summary>A cubic basis spline.</summary>
    public static Curve Basis => new(CurveKind.Basis);

    /// <summary>A cardinal spline.</summary>
    /// <param name="tension">0 (d3's default) to 1 (straight lines).</param>
    /// <returns>The curve.</returns>
    public static Curve Cardinal(double tension = 0) => new(CurveKind.Cardinal, tension);

    /// <summary>A Catmull–Rom spline.</summary>
    /// <param name="alpha">0 (uniform) to 1 (chordal); d3's default is 0.5 (centripetal).</param>
    /// <returns>The curve.</returns>
    public static Curve CatmullRom(double alpha = 0.5) => new(CurveKind.CatmullRom, alpha);

    /// <summary>A closed polygon through the points.</summary>
    public static Curve LinearClosed => new(CurveKind.LinearClosed);

    /// <summary>A closed cubic basis spline.</summary>
    public static Curve BasisClosed => new(CurveKind.BasisClosed);

    /// <summary>A cubic basis spline that starts and ends inside the first and last points.</summary>
    public static Curve BasisOpen => new(CurveKind.BasisOpen);

    /// <summary>A bundle: a basis spline straightened toward the line between its ends; for lines, not areas.</summary>
    /// <param name="beta">0 (a straight line) to 1 (a basis spline); d3's default is 0.85.</param>
    /// <returns>The curve.</returns>
    public static Curve Bundle(double beta = 0.85) => new(CurveKind.Bundle, beta);

    /// <summary>A closed cardinal spline.</summary>
    /// <param name="tension">0 (d3's default) to 1.</param>
    /// <returns>The curve.</returns>
    public static Curve CardinalClosed(double tension = 0) => new(CurveKind.CardinalClosed, tension);

    /// <summary>A cardinal spline that starts and ends at the second and next-to-last points.</summary>
    /// <param name="tension">0 (d3's default) to 1.</param>
    /// <returns>The curve.</returns>
    public static Curve CardinalOpen(double tension = 0) => new(CurveKind.CardinalOpen, tension);

    /// <summary>A closed Catmull–Rom spline.</summary>
    /// <param name="alpha">0 (uniform) to 1 (chordal); d3's default is 0.5.</param>
    /// <returns>The curve.</returns>
    public static Curve CatmullRomClosed(double alpha = 0.5) => new(CurveKind.CatmullRomClosed, alpha);

    /// <summary>A Catmull–Rom spline that starts and ends at the second and next-to-last points.</summary>
    /// <param name="alpha">0 (uniform) to 1 (chordal); d3's default is 0.5.</param>
    /// <returns>The curve.</returns>
    public static Curve CatmullRomOpen(double alpha = 0.5) => new(CurveKind.CatmullRomOpen, alpha);

    /// <summary>Cubic Bézier bumps with horizontal tangents at each point.</summary>
    public static Curve BumpX => new(CurveKind.BumpX);

    /// <summary>Cubic Bézier bumps with vertical tangents at each point.</summary>
    public static Curve BumpY => new(CurveKind.BumpY);
}
