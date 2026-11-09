using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes.Curves;

namespace Aioros.Graphs.Shapes;

internal interface ICurveDrawing
{
    void Draw<TCurve, TSink>(ref PathContext<TSink> context, ref TCurve curve)
        where TCurve : struct, ICurve
        where TSink : IPathSink;
}

internal static class CurveDispatch
{
    public static void Run<TDrawing, TSink>(Curve curve, ref PathContext<TSink> context, ref TDrawing drawing)
        where TDrawing : ICurveDrawing, allows ref struct
        where TSink : IPathSink
    {
        if (curve.Kind >= CurveKind.LinearClosed)
        {
            RunVariant(curve, ref context, ref drawing);
            return;
        }

        switch (curve.Kind)
        {
            case CurveKind.MonotoneX:
            case CurveKind.MonotoneY:
                var monotone = new MonotoneCurve(curve.Kind == CurveKind.MonotoneY);
                drawing.Draw(ref context, ref monotone);
                break;
            case CurveKind.Natural:
                var natural = default(NaturalCurve);
                drawing.Draw(ref context, ref natural);
                break;
            case CurveKind.Step:
            case CurveKind.StepBefore:
            case CurveKind.StepAfter:
                var step = new StepCurve(curve.Kind == CurveKind.Step ? 0.5 : curve.Kind == CurveKind.StepBefore ? 0 : 1);
                drawing.Draw(ref context, ref step);
                break;
            case CurveKind.Basis:
                var basis = default(BasisCurve);
                drawing.Draw(ref context, ref basis);
                break;
            case CurveKind.Cardinal:
                var cardinal = new CardinalCurve(curve.Parameter);
                drawing.Draw(ref context, ref cardinal);
                break;
            case CurveKind.CatmullRom when IsTruthy(curve.Parameter):
                var catmullRom = new CatmullRomCurve(curve.Parameter);
                drawing.Draw(ref context, ref catmullRom);
                break;
            case CurveKind.CatmullRom:
                var uniform = new CardinalCurve(0);
                drawing.Draw(ref context, ref uniform);
                break;
            default:
                var linear = default(LinearCurve);
                drawing.Draw(ref context, ref linear);
                break;
        }
    }

    private static void RunVariant<TDrawing, TSink>(Curve curve, ref PathContext<TSink> context, ref TDrawing drawing)
        where TDrawing : ICurveDrawing, allows ref struct
        where TSink : IPathSink
    {
        switch (curve.Kind)
        {
            case CurveKind.LinearClosed:
                var linearClosed = default(LinearClosedCurve);
                drawing.Draw(ref context, ref linearClosed);
                break;
            case CurveKind.BasisClosed:
                var basisClosed = default(BasisClosedCurve);
                drawing.Draw(ref context, ref basisClosed);
                break;
            case CurveKind.BasisOpen:
                var basisOpen = default(BasisOpenCurve);
                drawing.Draw(ref context, ref basisOpen);
                break;
            case CurveKind.Bundle when curve.Parameter == 1:
                var basis = default(BasisCurve);
                drawing.Draw(ref context, ref basis);
                break;
            case CurveKind.Bundle:
                var bundle = new BundleCurve(curve.Parameter);
                drawing.Draw(ref context, ref bundle);
                break;
            case CurveKind.CardinalClosed:
            case CurveKind.CatmullRomClosed when !IsTruthy(curve.Parameter):
                var cardinalClosed = new CardinalClosedCurve(curve.Kind == CurveKind.CardinalClosed ? curve.Parameter : 0);
                drawing.Draw(ref context, ref cardinalClosed);
                break;
            case CurveKind.CardinalOpen:
            case CurveKind.CatmullRomOpen when !IsTruthy(curve.Parameter):
                var cardinalOpen = new CardinalOpenCurve(curve.Kind == CurveKind.CardinalOpen ? curve.Parameter : 0);
                drawing.Draw(ref context, ref cardinalOpen);
                break;
            case CurveKind.CatmullRomClosed:
                var catmullRomClosed = new CatmullRomClosedCurve(curve.Parameter);
                drawing.Draw(ref context, ref catmullRomClosed);
                break;
            case CurveKind.CatmullRomOpen:
                var catmullRomOpen = new CatmullRomOpenCurve(curve.Parameter);
                drawing.Draw(ref context, ref catmullRomOpen);
                break;
            default:
                var bump = new BumpCurve(curve.Kind == CurveKind.BumpX);
                drawing.Draw(ref context, ref bump);
                break;
        }
    }

    private static bool IsTruthy(double value) => value != 0 && !double.IsNaN(value);
}
