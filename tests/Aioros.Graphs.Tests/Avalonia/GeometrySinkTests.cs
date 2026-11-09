using Aioros.Graphs.Avalonia;
using Aioros.Graphs.Paths;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;

namespace Aioros.Graphs.Tests.Avalonia;

public sealed class GeometrySinkTests
{
    [Fact]
    [Trait("AC", "PATH-04")]
    public void PathsBecomeFiguresWithSvgSemantics()
    {
        var recorder = new RecordingContext();
        var sink = new GeometrySink(recorder);
        var path = new PathContext<GeometrySink>(ref sink);
        path.MoveTo(1, 2);
        path.LineTo(3, 4);
        path.QuadraticCurveTo(5, 6, 7, 8);
        path.BezierCurveTo(9, 10, 11, 12, 13, 14);
        path.Arc(0, 0, 10, 0, Math.PI / 2);
        path.Arc(0, 0, 10, Math.PI / 2, 0, true);
        path.ClosePath();
        path.LineTo(20, 20);
        path.Rect(30, 40, 5, 6);
        path.MoveTo(50, 50);
        path.LineTo(60, 60);
        sink.Complete();

        Assert.Equal(
        [
            "begin 1,2 filled", "line 3,4", "quad 5,6 7,8", "cubic 9,10 11,12 13,14",
            "line 10,0", "arc 6.123233995736766E-16,10 r10 small Clockwise",
            "arc 10,0 r10 small CounterClockwise", "end closed",
            "begin 1,2 filled", "line 20,20", "end open",
            "begin 30,40 filled", "line 35,40", "line 35,46", "line 30,46", "end closed",
            "begin 50,50 filled", "line 60,60", "end open",
        ], recorder.Calls);
    }

    private sealed class RecordingContext : IGeometryContext
    {
        public List<string> Calls { get; } = [];

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true) =>
            Calls.Add($"arc {P(point)} r{size.Width} {(isLargeArc ? "large" : "small")} {sweepDirection}");

        public void BeginFigure(Point startPoint, bool isFilled = true) => Calls.Add($"begin {P(startPoint)} {(isFilled ? "filled" : "hollow")}");

        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true) =>
            Calls.Add($"cubic {P(controlPoint1)} {P(controlPoint2)} {P(endPoint)}");

        public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true) => Calls.Add($"quad {P(controlPoint)} {P(endPoint)}");

        public void LineTo(Point point, bool isStroked = true) => Calls.Add($"line {P(point)}");

        public void EndFigure(bool isClosed) => Calls.Add(isClosed ? "end closed" : "end open");

        public void SetFillRule(FillRule fillRule) => Calls.Add($"fill {fillRule}");

        public void Dispose() => Calls.Add("disposed");

        private static string P(Point point) => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{point.X:R},{point.Y:R}");
    }
}
