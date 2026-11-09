using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes;

/// <summary>
/// The symbol generator of d3-shape: a symbol of a type and an area, centered on a point. The overload over a
/// <see cref="PathContext{TSink}"/> writes symbols at any center, so a scatter plot's symbols can share one path.
/// </summary>
public static class Symbol
{
    private static readonly SymbolType[] FillTypes = [SymbolType.Circle, SymbolType.Cross, SymbolType.Diamond, SymbolType.Square, SymbolType.Star, SymbolType.Triangle, SymbolType.Wye];
    private static readonly SymbolType[] StrokeTypes = [SymbolType.Circle, SymbolType.Plus, SymbolType.Times, SymbolType.Triangle2, SymbolType.Asterisk, SymbolType.Square2, SymbolType.Diamond2];

    /// <summary>The symbols designed to be filled, as d3's <c>symbolsFill</c>.</summary>
    public static ReadOnlySpan<SymbolType> Fill => FillTypes;

    /// <summary>The symbols designed to be stroked (1.5 pixels wide, round caps), as d3's <c>symbolsStroke</c>.</summary>
    public static ReadOnlySpan<SymbolType> Stroke => StrokeTypes;

    /// <summary>Writes a symbol centered on the origin into a new path over <paramref name="sink"/>, as d3's <c>symbol(type, size)()</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="type">The symbol.</param>
    /// <param name="size">The area in square pixels; d3's default is 64.</param>
    /// <exception cref="ArgumentOutOfRangeException">The type is not defined.</exception>
    public static void Write<TSink>(ref TSink sink, SymbolType type, double size = 64)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Write(ref context, type, size, 0, 0);
    }

    /// <summary>Writes a symbol centered on (<paramref name="x"/>, <paramref name="y"/>) into <paramref name="context"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="type">The symbol.</param>
    /// <param name="size">The area in square pixels.</param>
    /// <param name="x">The center's horizontal coordinate.</param>
    /// <param name="y">The center's vertical coordinate.</param>
    /// <exception cref="ArgumentOutOfRangeException">The type is not defined.</exception>
    public static void Write<TSink>(ref PathContext<TSink> context, SymbolType type, double size, double x, double y)
        where TSink : IPathSink
    {
        switch (type)
        {
            case SymbolType.Circle:
                SymbolShapes.Circle(ref context, size, x, y);
                break;
            case SymbolType.Cross:
                SymbolShapes.Cross(ref context, size, x, y);
                break;
            case SymbolType.Diamond:
                SymbolShapes.Diamond(ref context, size, x, y);
                break;
            case SymbolType.Square:
                SymbolShapes.Square(ref context, size, x, y);
                break;
            case SymbolType.Star:
                SymbolShapes.Star(ref context, size, x, y);
                break;
            case SymbolType.Triangle:
                SymbolShapes.Triangle(ref context, size, x, y);
                break;
            case SymbolType.Wye:
                SymbolShapes.Wye(ref context, size, x, y);
                break;
            default:
                StrokeSymbolShapes.Write(ref context, type, size, x, y);
                break;
        }
    }
}
