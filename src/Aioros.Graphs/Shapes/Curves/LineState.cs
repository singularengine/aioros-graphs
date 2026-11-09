using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

internal enum LineState
{
    Line,
    Topline,
    Baseline,
}

internal static class LineStates
{
    public static LineState Next(this LineState state) => state switch
    {
        LineState.Topline => LineState.Baseline,
        LineState.Baseline => LineState.Topline,
        _ => LineState.Line,
    };

    public static void Close<TSink>(this LineState state, ref PathContext<TSink> context, bool singlePoint)
        where TSink : IPathSink
    {
        if (state == LineState.Baseline || (state == LineState.Line && singlePoint))
        {
            context.ClosePath();
        }
    }

    public static void Start<TSink>(this LineState state, ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (state == LineState.Baseline)
        {
            context.LineTo(x, y);
        }
        else
        {
            context.MoveTo(x, y);
        }
    }
}
