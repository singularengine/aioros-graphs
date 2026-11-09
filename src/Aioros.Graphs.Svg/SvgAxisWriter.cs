using System.Buffers;
using System.Text;
using Aioros.Graphs.Axes;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Svg;

/// <summary>Writes an axis as the SVG markup d3-axis builds, in UTF-8.</summary>
public static class SvgAxisWriter
{
    /// <summary>
    /// Writes <paramref name="axis"/> as the <c>&lt;g&gt;</c> d3-axis fills, serialized as a browser does: the group's
    /// font and anchor attributes, the domain path, then each tick's group with its line and label.
    /// </summary>
    /// <param name="output">Where the markup goes.</param>
    /// <param name="axis">The axis.</param>
    public static void Write(IBufferWriter<byte> output, AxisLayout axis)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(axis);
        Text(output, "<g fill=\"none\" font-size=\"10\" font-family=\"sans-serif\" text-anchor=\"");
        Text(output, axis.TextAnchor);
        Text(output, "\"><path class=\"domain\" stroke=\"currentColor\" d=\"");
        Text(output, axis.DomainPath());
        Text(output, "\"></path>");
        var across = axis.IsVertical ? "x" : "y";
        for (var i = 0; i < axis.Ticks.Count; i++)
        {
            Text(output, "<g class=\"tick\" opacity=\"1\" transform=\"");
            Text(output, axis.TickTransform(i));
            Text(output, "\"><line stroke=\"currentColor\" ");
            Text(output, across);
            Text(output, "2=\"");
            Number(output, axis.TickLineEnd);
            Text(output, "\"></line><text fill=\"currentColor\" ");
            Text(output, across);
            Text(output, "=\"");
            Number(output, axis.LabelOffset);
            Text(output, "\" dy=\"");
            Text(output, axis.LabelDy);
            Text(output, "\">");
            Escaped(output, axis.Ticks[i].Label);
            Text(output, "</text></g>");
        }

        Text(output, "</g>");
    }

    private static void Number(IBufferWriter<byte> output, double value)
    {
        var span = output.GetSpan(JsNumber.MaxShortestLength);
        output.Advance(JsNumber.WriteShortest(value, span));
    }

    private static void Text(IBufferWriter<byte> output, string text)
    {
        var span = output.GetSpan(Encoding.UTF8.GetMaxByteCount(text.Length));
        output.Advance(Encoding.UTF8.GetBytes(text, span));
    }

    private static void Escaped(IBufferWriter<byte> output, string text)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var entity = text[i] switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                ' ' => "&nbsp;",
                _ => null,
            };
            if (entity is not null)
            {
                Text(output, text[start..i]);
                Text(output, entity);
                start = i + 1;
            }
        }

        Text(output, text[start..]);
    }
}
