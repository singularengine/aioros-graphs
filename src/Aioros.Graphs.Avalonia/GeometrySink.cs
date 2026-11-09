using Aioros.Graphs.Paths;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;

namespace Aioros.Graphs.Avalonia;

/// <summary>
/// Draws Aioros.Graphs' paths into an Avalonia geometry context (a <see cref="StreamGeometryContext"/>, or any
/// <see cref="IGeometryContext"/>) with SVG's semantics: every
/// move begins a filled figure, a close ends it closed, and drawing after a close begins a figure at the subpath start.
/// </summary>
public struct GeometrySink : IPathSink
{
    private readonly IGeometryContext context;
    private Point start;
    private Point current;
    private bool open;

    /// <summary>Starts drawing into <paramref name="context"/>.</summary>
    /// <param name="context">The context of the geometry being built.</param>
    public GeometrySink(IGeometryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
    }

    /// <inheritdoc />
    public void MoveTo(double x, double y)
    {
        if (open)
        {
            context.EndFigure(false);
        }

        start = current = new Point(x, y);
        context.BeginFigure(start, true);
        open = true;
    }

    /// <inheritdoc />
    public void LineTo(double x, double y)
    {
        EnsureFigure();
        current = new Point(x, y);
        context.LineTo(current);
    }

    /// <inheritdoc />
    public void QuadraticCurveTo(double x1, double y1, double x, double y)
    {
        EnsureFigure();
        current = new Point(x, y);
        context.QuadraticBezierTo(new Point(x1, y1), current);
    }

    /// <inheritdoc />
    public void BezierCurveTo(double x1, double y1, double x2, double y2, double x, double y)
    {
        EnsureFigure();
        current = new Point(x, y);
        context.CubicBezierTo(new Point(x1, y1), new Point(x2, y2), current);
    }

    /// <inheritdoc />
    public void ArcTo(double radius, bool largeArc, bool sweep, double x, double y)
    {
        EnsureFigure();
        current = new Point(x, y);
        context.ArcTo(current, new Size(radius, radius), 0, largeArc, sweep ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
    }

    /// <inheritdoc />
    public void Rect(double x, double y, double width, double height)
    {
        MoveTo(x, y);
        context.LineTo(new Point(x + width, y));
        context.LineTo(new Point(x + width, y + height));
        context.LineTo(new Point(x, y + height));
        ClosePath();
    }

    /// <inheritdoc />
    public void ClosePath()
    {
        if (open)
        {
            context.EndFigure(true);
            open = false;
        }

        current = start;
    }

    /// <summary>Ends the figure left open, if any; call it once the path is written.</summary>
    public void Complete()
    {
        if (open)
        {
            context.EndFigure(false);
            open = false;
        }
    }

    private void EnsureFigure()
    {
        if (!open)
        {
            start = current;
            context.BeginFigure(current, true);
            open = true;
        }
    }
}
