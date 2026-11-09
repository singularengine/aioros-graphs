using System.Buffers;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Svg;

/// <summary>
/// Writes a path as SVG path data in UTF-8, byte for byte as d3-path writes it: absolute commands, numbers separated
/// by commas, and coordinates rounded to <c>digits</c> fraction digits (d3-shape's default is 3).
/// </summary>
public readonly struct SvgPathWriter : IPathSink
{
    private const int MaxCommandLength = 1 + 7 * (JsNumber.MaxShortestLength + 1);

    private readonly IBufferWriter<byte> output;
    private readonly int digits;

    /// <summary>Starts writing to <paramref name="output"/>.</summary>
    /// <param name="output">Where the path data goes.</param>
    /// <param name="digits">The fraction digits coordinates are rounded to, or null to write them in full as d3.path() does.</param>
    public SvgPathWriter(IBufferWriter<byte> output, int? digits = 3)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (digits is < 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(digits), digits, "Digits must be between 0 and 15.");
        }

        this.output = output;
        this.digits = digits ?? -1;
    }

    /// <inheritdoc />
    public void MoveTo(double x, double y) => Write('M', x, y);

    /// <inheritdoc />
    public void LineTo(double x, double y) => Write('L', x, y);

    /// <inheritdoc />
    public void QuadraticCurveTo(double x1, double y1, double x, double y)
    {
        var span = output.GetSpan(MaxCommandLength);
        span[0] = (byte)'Q';
        var written = 1 + Number(x1, span[1..]);
        written += Next(y1, span[written..]);
        written += Next(x, span[written..]);
        written += Next(y, span[written..]);
        output.Advance(written);
    }

    /// <inheritdoc />
    public void BezierCurveTo(double x1, double y1, double x2, double y2, double x, double y)
    {
        var span = output.GetSpan(MaxCommandLength);
        span[0] = (byte)'C';
        var written = 1 + Number(x1, span[1..]);
        written += Next(y1, span[written..]);
        written += Next(x2, span[written..]);
        written += Next(y2, span[written..]);
        written += Next(x, span[written..]);
        written += Next(y, span[written..]);
        output.Advance(written);
    }

    /// <inheritdoc />
    public void ArcTo(double radius, bool largeArc, bool sweep, double x, double y)
    {
        var span = output.GetSpan(MaxCommandLength);
        span[0] = (byte)'A';
        var written = 1 + Number(radius, span[1..]);
        written += Next(radius, span[written..]);
        span[written++] = (byte)',';
        span[written++] = (byte)'0';
        span[written++] = (byte)',';
        span[written++] = largeArc ? (byte)'1' : (byte)'0';
        span[written++] = (byte)',';
        span[written++] = sweep ? (byte)'1' : (byte)'0';
        written += Next(x, span[written..]);
        written += Next(y, span[written..]);
        output.Advance(written);
    }

    /// <inheritdoc />
    public void Rect(double x, double y, double width, double height)
    {
        var span = output.GetSpan(MaxCommandLength);
        span[0] = (byte)'M';
        var written = 1 + Number(x, span[1..]);
        written += Next(y, span[written..]);
        span[written++] = (byte)'h';
        written += Number(width, span[written..]);
        span[written++] = (byte)'v';
        written += Number(height, span[written..]);
        span[written++] = (byte)'h';
        written += Number(-width, span[written..]);
        span[written++] = (byte)'Z';
        output.Advance(written);
    }

    /// <inheritdoc />
    public void ClosePath()
    {
        output.GetSpan(1)[0] = (byte)'Z';
        output.Advance(1);
    }

    private void Write(char command, double x, double y)
    {
        var span = output.GetSpan(MaxCommandLength);
        span[0] = (byte)command;
        var written = 1 + Number(x, span[1..]);
        written += Next(y, span[written..]);
        output.Advance(written);
    }

    private int Next(double value, Span<byte> span)
    {
        span[0] = (byte)',';
        return 1 + Number(value, span[1..]);
    }

    private int Number(double value, Span<byte> span) =>
        digits < 0 ? JsNumber.WriteShortest(value, span) : JsNumber.WriteRounded(value, digits, span);
}
