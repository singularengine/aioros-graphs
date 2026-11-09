using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes.Curves;

namespace Aioros.Graphs.Shapes;

internal readonly ref struct RadialDrawing<TDrawing>(TDrawing drawing) : ICurveDrawing
    where TDrawing : ICurveDrawing, allows ref struct
{
    private readonly TDrawing drawing = drawing;

    public void Draw<TCurve, TSink>(ref PathContext<TSink> context, ref TCurve curve)
        where TCurve : struct, ICurve
        where TSink : IPathSink
    {
        var radial = new RadialCurve<TCurve>(curve);
        drawing.Draw(ref context, ref radial);
    }
}
