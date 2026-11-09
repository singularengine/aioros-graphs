using System.Text;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Demo;

internal sealed record Item(string Kind, string Class, double[] Numbers, string? Text = null, string? Extra = null);

internal sealed class Scene(double width, double height)
{
    private readonly List<Item> items = [];

    public List<string> Paths { get; } = [];

    public List<string> Texts { get; } = [];

    public void Path(string cls, string d, string? title = null, double[]? translate = null)
    {
        Paths.Add(d);
        items.Add(new Item("path", cls, translate ?? [], d, title));
    }

    public void Stroke(string cls, string d, double width, string? title = null)
    {
        Paths.Add(d);
        items.Add(new Item("path", cls, [width], d, title));
    }

    public void Line(string cls, double x1, double y1, double x2, double y2) =>
        items.Add(new Item("line", cls, [x1, y1, x2, y2]));

    public void Text(string cls, double x, double y, string text, string anchor = "start")
    {
        Texts.Add(text);
        items.Add(new Item("text", cls, [x, y], text, anchor));
    }

    public void Rect(string cls, double x, double y, double w, double h) =>
        items.Add(new Item("rect", cls, [x, y, w, h]));

    public void Cell(string cls, double x, double y, double w, double h, string fill)
    {
        Texts.Add(fill);
        items.Add(new Item("rect", cls, [x, y, w, h], null, fill));
    }

    public void Group(string cls, double x, double y, string markup, IEnumerable<string> labels)
    {
        Texts.AddRange(labels);
        items.Add(new Item("g", cls, [x, y], markup));
    }

    public void Circle(string cls, double x, double y, double r) =>
        items.Add(new Item("circle", cls, [x, y, r]));

    public void AxisBottom(double[] positions, string[] labels, double y, double x0, double x1)
    {
        Line("domain", x0, y, x1, y);
        for (var i = 0; i < positions.Length; i++)
        {
            Line("tick", positions[i], y, positions[i], y + 5);
            Text("label", positions[i], y + 17, labels[i], "middle");
        }
    }

    public void GridLeft(double[] positions, string[] labels, double x0, double x1)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            Line("grid", x0, positions[i], x1, positions[i]);
            Text("label", x0 - 8, positions[i] + 4, labels[i], "end");
        }
    }

    public void Legend(string[] keys, double x, double y)
    {
        for (var i = 0; i < keys.Length; i++)
        {
            Rect("swatch s" + (i + 1), x, y - 9, 10, 10);
            Text("legend", x + 14, y, keys[i]);
            x += 14 + keys[i].Length * 6.6 + 16;
        }
    }

    public string ToSvg(string label)
    {
        var svg = new StringBuilder();
        svg.Append("<svg class=\"chart\" viewBox=\"0 0 ").Append(N(width)).Append(' ').Append(N(height))
            .Append("\" role=\"img\" aria-label=\"").Append(Escape(label)).Append("\">");
        foreach (var item in items)
        {
            svg.Append('\n');
            Write(svg, item);
        }

        return svg.Append("\n</svg>").ToString();
    }

    public static string N(double value) => JsNumber.ToShortestString(Math.Floor(value * 100 + 0.5) / 100);

    public static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal);

    private static void Write(StringBuilder svg, Item item)
    {
        var n = item.Numbers;
        svg.Append('<').Append(item.Kind).Append(" class=\"").Append(item.Class).Append('"');
        switch (item.Kind)
        {
            case "path":
                svg.Append(" d=\"").Append(item.Text).Append('"');
                if (n.Length == 2)
                {
                    svg.Append(" transform=\"translate(").Append(N(n[0])).Append(',').Append(N(n[1])).Append(")\"");
                }
                else if (n.Length == 1)
                {
                    svg.Append(" stroke-width=\"").Append(N(n[0])).Append('"');
                }

                if (item.Extra is not null)
                {
                    svg.Append("><title>").Append(Escape(item.Extra)).Append("</title></path>");
                    return;
                }

                break;
            case "line":
                svg.Append(" x1=\"").Append(N(n[0])).Append("\" y1=\"").Append(N(n[1])).Append("\" x2=\"").Append(N(n[2])).Append("\" y2=\"").Append(N(n[3])).Append('"');
                break;
            case "text":
                svg.Append(" x=\"").Append(N(n[0])).Append("\" y=\"").Append(N(n[1])).Append('"');
                if (item.Extra != "start")
                {
                    svg.Append(" text-anchor=\"").Append(item.Extra).Append('"');
                }

                svg.Append('>').Append(Escape(item.Text!)).Append("</text>");
                return;
            case "g":
                svg.Append(" transform=\"translate(").Append(N(n[0])).Append(',').Append(N(n[1])).Append(")\">").Append(item.Text).Append("</g>");
                return;
            case "rect":
                svg.Append(" x=\"").Append(N(n[0])).Append("\" y=\"").Append(N(n[1])).Append("\" width=\"").Append(N(n[2])).Append("\" height=\"").Append(N(n[3])).Append(item.Extra is null ? "\" rx=\"2\"" : "\" fill=\"" + item.Extra + "\"");
                break;
            default:
                svg.Append(" cx=\"").Append(N(n[0])).Append("\" cy=\"").Append(N(n[1])).Append("\" r=\"").Append(N(n[2])).Append('"');
                break;
        }

        svg.Append("/>");
    }
}
