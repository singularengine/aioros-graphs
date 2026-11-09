using System.Text.Json;

namespace Aioros.Graphs.Demo;

internal static class Json
{
    public static async Task WriteAsync(string path, Action<Utf8JsonWriter> write)
    {
        await using var stream = File.Create(path);
        await using var writer = new Utf8JsonWriter(stream);
        write(writer);
    }

    public static void Data(Utf8JsonWriter json, DemoData data)
    {
        json.WriteStartObject();
        json.WriteStartObject("stock");
        Numbers(json, "dates", data.Stock.Dates);
        Numbers(json, "values", data.Stock.Values);
        json.WriteEndObject();
        json.WriteStartObject("visitors");
        Numbers(json, "dates", data.Visitors.Dates);
        Numbers(json, "values", data.Visitors.Values);
        json.WriteEndObject();
        Stacked(json, "sales", data.Sales.Dates, data.Sales.Keys, data.Sales.Rows);
        json.WriteStartObject("revenue");
        Strings(json, "labels", data.Revenue.Labels);
        Strings(json, "keys", data.Revenue.Keys);
        Rows(json, data.Revenue.Rows);
        json.WriteEndObject();
        json.WriteStartObject("browsers");
        Strings(json, "names", data.Browsers.Names);
        Numbers(json, "values", data.Browsers.Values);
        json.WriteEndObject();
        Stacked(json, "stream", data.Stream.Dates, data.Stream.Keys, data.Stream.Rows);
        json.WriteStartObject("curves");
        Numbers(json, "x", data.Curves.X);
        Numbers(json, "y", data.Curves.Y);
        json.WriteEndObject();
        Numbers(json, "heights", data.Heights);
        json.WriteStartObject("flowers");
        Strings(json, "keys", data.Flowers.Keys);
        Numbers(json, "group", [.. data.Flowers.Group.Select(g => (double)g)]);
        Numbers(json, "x", data.Flowers.X);
        Numbers(json, "y", data.Flowers.Y);
        Numbers(json, "size", data.Flowers.Size);
        json.WriteEndObject();
        json.WriteStartObject("temperatures");
        Numbers(json, "dates", data.Temperatures.Dates);
        Numbers(json, "low", data.Temperatures.Low);
        Numbers(json, "high", data.Temperatures.High);
        json.WriteEndObject();
        json.WriteStartObject("anomalies");
        Strings(json, "months", data.Anomalies.Rows);
        Numbers(json, "columns", data.Anomalies.Columns);
        Rows(json, data.Anomalies.Values);
        json.WriteEndObject();
        json.WriteStartObject("transistors");
        Numbers(json, "dates", data.Transistors.Dates);
        Numbers(json, "values", data.Transistors.Values);
        json.WriteEndObject();
        LayoutDataSet.Write(json, data.Layouts);
        json.WriteEndObject();
    }

    public static void Run(Utf8JsonWriter json, RunResult run)
    {
        json.WriteStartObject();
        json.WriteString("runtime", run.Runtime);
        json.WriteString("machine", run.Machine);
        json.WriteString("date", run.Date);
        Numbers(json, "loadAverage", run.LoadAverage);
        json.WriteStartArray("charts");
        foreach (var chart in run.Charts)
        {
            json.WriteStartObject();
            json.WriteString("id", chart.Id);
            json.WriteNumber("medianNs", chart.MedianNs);
            json.WriteNumber("iterations", chart.Iterations);
            json.WriteNumber("samples", chart.Samples);
            Strings(json, "paths", chart.Paths);
            Strings(json, "texts", chart.Texts);
            json.WriteString("svg", chart.Svg);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    public static void Results(Utf8JsonWriter json, string commit, RunResult engine, RunResult d3)
    {
        json.WriteStartObject();
        json.WriteString("kind", "demo");
        json.WriteString("commit", commit);
        json.WriteString("machine", engine.Machine);
        json.WriteString("method", "per chart: scales, ticks, tick labels and path strings (no SVG markup); 300 ms warm-up, iterations doubled until a sample takes 10 ms, median of 11 samples");
        foreach (var (name, run) in new[] { ("engine", engine), ("d3", d3) })
        {
            json.WriteStartObject(name);
            json.WriteString("runtime", run.Runtime);
            json.WriteString("date", run.Date);
            Numbers(json, "loadAverage", run.LoadAverage);
            json.WriteEndObject();
        }

        json.WriteStartArray("charts");
        foreach (var e in engine.Charts)
        {
            var d = d3.Charts.Single(c => c.Id == e.Id);
            json.WriteStartObject();
            json.WriteString("id", e.Id);
            json.WriteNumber("engineMedianNs", e.MedianNs);
            json.WriteNumber("engineIterations", e.Iterations);
            json.WriteNumber("d3MedianNs", d.MedianNs);
            json.WriteNumber("d3Iterations", d.Iterations);
            json.WriteNumber("paths", e.Paths.Length);
            json.WriteBoolean("pathsIdentical", e.Paths.SequenceEqual(d.Paths));
            json.WriteBoolean("textsIdentical", e.Texts.SequenceEqual(d.Texts));
            json.WriteBoolean("svgIdentical", e.Svg == d.Svg);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    public static async Task<RunResult> ReadRunAsync(string path)
    {
        using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(path));
        var root = document.RootElement;
        return new RunResult(
            root.GetProperty("runtime").GetString()!,
            root.GetProperty("machine").GetString()!,
            root.GetProperty("date").GetString()!,
            [.. root.GetProperty("loadAverage").EnumerateArray().Select(e => e.GetDouble())],
            [.. root.GetProperty("charts").EnumerateArray().Select(c => new ChartResult(
                c.GetProperty("id").GetString()!,
                c.GetProperty("svg").GetString()!,
                [.. c.GetProperty("paths").EnumerateArray().Select(e => e.GetString()!)],
                [.. c.GetProperty("texts").EnumerateArray().Select(e => e.GetString()!)],
                c.GetProperty("medianNs").GetDouble(),
                c.GetProperty("iterations").GetInt64(),
                c.GetProperty("samples").GetInt32()))]);
    }

    private static void Stacked(Utf8JsonWriter json, string name, double[] dates, string[] keys, double[][] rows)
    {
        json.WriteStartObject(name);
        Numbers(json, "dates", dates);
        Strings(json, "keys", keys);
        Rows(json, rows);
        json.WriteEndObject();
    }

    private static void Rows(Utf8JsonWriter json, double[][] rows)
    {
        json.WriteStartArray("rows");
        foreach (var row in rows)
        {
            json.WriteStartArray();
            foreach (var value in row)
            {
                json.WriteNumberValue(value);
            }

            json.WriteEndArray();
        }

        json.WriteEndArray();
    }

    private static void Numbers(Utf8JsonWriter json, string name, double[] values)
    {
        json.WriteStartArray(name);
        foreach (var value in values)
        {
            json.WriteNumberValue(value);
        }

        json.WriteEndArray();
    }

    private static void Strings(Utf8JsonWriter json, string name, IEnumerable<string> values)
    {
        json.WriteStartArray(name);
        foreach (var value in values)
        {
            json.WriteStringValue(value);
        }

        json.WriteEndArray();
    }
}
