using System.Buffers;
using System.Text;
using Aioros.Graphs.Chords;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Demo;

internal static class PathText
{
    public static string Line(Curve curve, double[] x, double[] y)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        Shapes.Line.Write(ref writer, curve, x, y);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Area(Curve curve, double[] x, double[] y0, double[] y1)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        Shapes.Area.Write(ref writer, curve, x, y0, y1);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Arc(in ArcShape arc)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        Shapes.Arc.Write(ref writer, arc);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Rects(double[] x, double[] y, double width, double[] height)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        var context = new PathContext<SvgPathWriter>(ref writer);
        for (var i = 0; i < x.Length; i++)
        {
            context.Rect(x[i], y[i], width, height[i]);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Rects(double[] x, double[] y, double[] width, double[] height)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        var context = new PathContext<SvgPathWriter>(ref writer);
        for (var i = 0; i < x.Length; i++)
        {
            context.Rect(x[i], y[i], width[i], height[i]);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Symbol(SymbolType type, double size)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        Shapes.Symbol.Write(ref writer, type, size);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Rect(double x, double y, double width, double height)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        var context = new PathContext<SvgPathWriter>(ref writer);
        context.Rect(x, y, width, height);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Circle(double x, double y, double r)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        var context = new PathContext<SvgPathWriter>(ref writer);
        context.Arc(x, y, r, 0, 2 * Math.PI);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string LinkHorizontal(double sourceX, double sourceY, double targetX, double targetY)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        var context = new PathContext<SvgPathWriter>(ref writer);
        Shapes.Link.Horizontal(ref context, sourceX, sourceY, targetX, targetY);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Ribbon(in RibbonShape ribbon)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        Chords.Ribbon.Write(ref writer, ribbon);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string Segments(ReadOnlySpan<double> x, ReadOnlySpan<double> y, int[] sources, int[] targets)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        var context = new PathContext<SvgPathWriter>(ref writer);
        for (var i = 0; i < sources.Length; i++)
        {
            context.MoveTo(x[sources[i]], y[sources[i]]);
            context.LineTo(x[targets[i]], y[targets[i]]);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static string AreaRadial(Curve curve, double[] angle, double[] inner, double[] outer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);
        Shapes.AreaRadial.Write(ref writer, curve, angle, inner, outer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
