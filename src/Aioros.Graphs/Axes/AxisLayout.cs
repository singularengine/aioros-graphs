using Aioros.Graphs.Formatting;
using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Axes;

/// <summary>
/// An axis laid out as d3-axis lays it out, as data: the ticks with their positions and labels, the tick lines, the
/// labels' offset and alignment, and the domain path. <see cref="Axis"/> creates it from a scale.
/// </summary>
public sealed class AxisLayout
{
    private readonly AxisTick[] ticks;

    internal AxisLayout(AxisOrientation orientation, AxisOptions options, double rangeStart, double rangeEnd, AxisTick[] ticks)
    {
        Orientation = orientation;
        Options = options;
        RangeStart = rangeStart;
        RangeEnd = rangeEnd;
        this.ticks = ticks;
    }

    /// <summary>Gets the axis's orientation.</summary>
    public AxisOrientation Orientation { get; }

    /// <summary>Gets the axis's settings.</summary>
    public AxisOptions Options { get; }

    /// <summary>Gets the start of the domain path along the axis: the range's first stop plus the offset.</summary>
    public double RangeStart { get; }

    /// <summary>Gets the end of the domain path along the axis: the range's last stop plus the offset.</summary>
    public double RangeEnd { get; }

    /// <summary>Gets the ticks in the order of the scale's tick values.</summary>
    public IReadOnlyList<AxisTick> Ticks => ticks;

    /// <summary>Gets the end of each tick line across the axis (its other end is 0): the <c>x2</c> or <c>y2</c> attribute.</summary>
    public double TickLineEnd => Direction * Options.TickSizeInner;

    /// <summary>Gets the labels' distance across the axis: the <c>x</c> or <c>y</c> attribute of d3's tick text.</summary>
    public double LabelOffset => Direction * (Math.Max(Options.TickSizeInner, 0) + Options.TickPadding);

    /// <summary>Gets the labels' baseline shift, d3's <c>dy</c>: <c>0em</c> on top, <c>0.71em</c> at the bottom, <c>0.32em</c> on the sides.</summary>
    public string LabelDy => Orientation switch
    {
        AxisOrientation.Top => "0em",
        AxisOrientation.Bottom => "0.71em",
        _ => "0.32em",
    };

    /// <summary>Gets the labels' alignment, d3's <c>text-anchor</c>: <c>middle</c>, or <c>end</c> on the left and <c>start</c> on the right.</summary>
    public string TextAnchor => Orientation switch
    {
        AxisOrientation.Right => "start",
        AxisOrientation.Left => "end",
        _ => "middle",
    };

    /// <summary>Gets a value indicating whether the axis is vertical (left or right), its ticks then varying in y.</summary>
    public bool IsVertical => Orientation is AxisOrientation.Left or AxisOrientation.Right;

    private bool HasOuter => Options.TickSizeOuter != 0 && !double.IsNaN(Options.TickSizeOuter);

    private double Direction => Orientation is AxisOrientation.Top or AxisOrientation.Left ? -1 : 1;

    /// <summary>Returns the domain path's data exactly as d3 writes it, with H and V commands.</summary>
    /// <returns>The path data.</returns>
    public string DomainPath()
    {
        var outer = Js(Direction * Options.TickSizeOuter);
        var (start, end, offset) = (Js(RangeStart), Js(RangeEnd), Js(Options.Offset));
        if (IsVertical)
        {
            return HasOuter ? $"M{outer},{start}H{offset}V{end}H{outer}" : $"M{offset},{start}V{end}";
        }

        return HasOuter ? $"M{start},{outer}V{offset}H{end}V{outer}" : $"M{start},{offset}H{end}";
    }

    /// <summary>Returns the transform of a tick's group as d3 writes it, such as <c>translate(96.5,0)</c>.</summary>
    /// <param name="index">The tick's index.</param>
    /// <returns>The transform.</returns>
    public string TickTransform(int index)
    {
        var position = Js(ticks[index].Position);
        return IsVertical ? $"translate(0,{position})" : $"translate({position},0)";
    }

    /// <summary>Writes the domain path and the tick lines to <paramref name="sink"/>, in the axis's own coordinates.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the lines go.</param>
    public void WriteLines<TSink>(ref TSink sink)
        where TSink : IPathSink
    {
        var outer = Direction * Options.TickSizeOuter;
        var offset = Options.Offset;
        if (HasOuter)
        {
            Point(ref sink, true, outer, RangeStart);
            Point(ref sink, false, offset, RangeStart);
            Point(ref sink, false, offset, RangeEnd);
            Point(ref sink, false, outer, RangeEnd);
        }
        else
        {
            Point(ref sink, true, offset, RangeStart);
            Point(ref sink, false, offset, RangeEnd);
        }

        var end = TickLineEnd;
        for (var i = 0; i < ticks.Length; i++)
        {
            Point(ref sink, true, 0, ticks[i].Position);
            Point(ref sink, false, end, ticks[i].Position);
        }
    }

    private void Point<TSink>(ref TSink sink, bool move, double across, double along)
        where TSink : IPathSink
    {
        var (x, y) = IsVertical ? (across, along) : (along, across);
        if (move)
        {
            sink.MoveTo(x, y);
        }
        else
        {
            sink.LineTo(x, y);
        }
    }

    private static string Js(double value) => JsNumber.ToShortestString(value);
}
