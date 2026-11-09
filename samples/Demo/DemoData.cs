namespace Aioros.Graphs.Demo;

internal sealed record TimeSeries(double[] Dates, double[] Values);

internal sealed record Table(string[] Labels, string[] Keys, double[][] Rows);

internal sealed record StackedSeries(double[] Dates, string[] Keys, double[][] Rows);

internal sealed record Shares(string[] Names, double[] Values);

internal sealed record Points(double[] X, double[] Y);

internal sealed record Groups(string[] Keys, int[] Group, double[] X, double[] Y, double[] Size);

internal sealed record DailyRange(double[] Dates, double[] Low, double[] High);

internal sealed record Grid(string[] Rows, double[] Columns, double[][] Values);

internal sealed record DemoData(
    TimeSeries Stock,
    TimeSeries Visitors,
    StackedSeries Sales,
    Table Revenue,
    Shares Browsers,
    StackedSeries Stream,
    Points Curves,
    double[] Heights,
    Groups Flowers,
    DailyRange Temperatures,
    Grid Anomalies,
    TimeSeries Transistors,
    LayoutData Layouts);

internal static class DataSet
{
    public static DemoData Generate()
    {
        var random = new SeededRandom(20261012);
        return new DemoData(
            Stock(random), Visitors(random), Sales(random), Revenue(random), Browsers(), Stream(random), Curves(),
            Heights(random), Flowers(random), Temperatures(random), Anomalies(random), Transistors(random),
            LayoutDataSet.Generate());
    }

    private static TimeSeries Stock(SeededRandom random)
    {
        var dates = new List<double>();
        var closes = new List<double>();
        var price = 142.5;
        for (var day = new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero); day.Year < 2025; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            price *= Math.Exp(0.0004 + 0.016 * random.NextGaussian());
            dates.Add(day.ToUnixTimeMilliseconds());
            closes.Add(Math.Round(price * 100) / 100);
        }

        return new TimeSeries([.. dates], [.. closes]);
    }

    private static TimeSeries Visitors(SeededRandom random)
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var dates = new double[52];
        var values = new double[52];
        for (var k = 0; k < 52; k++)
        {
            dates[k] = start.AddDays(7 * k).ToUnixTimeMilliseconds();
            values[k] = Math.Round(12000 + 3000 * Math.Sin(2 * Math.PI * (k - 10) / 52) + 60 * k + 700 * random.NextGaussian());
        }

        return new TimeSeries(dates, values);
    }

    private static StackedSeries Sales(SeededRandom random)
    {
        string[] keys = ["Electronics", "Apparel", "Home & garden", "Grocery", "Toys"];
        double[] bases = [42, 31, 24, 36, 12];
        double[] holiday = [0.45, 0.25, 0.1, 0.08, 1.1];
        var dates = new double[36];
        var rows = new double[36][];
        for (var month = 0; month < 36; month++)
        {
            dates[month] = new DateTimeOffset(2022 + month / 12, month % 12 + 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
            rows[month] = new double[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                var season = month % 12 == 11 ? 1 + holiday[i] : month % 12 == 10 ? 1 + holiday[i] / 3 : 1;
                var value = bases[i] * (1 + 0.012 * month) * season * (1 + 0.06 * random.NextGaussian());
                rows[month][i] = Math.Round(value * 10) / 10;
            }
        }

        return new StackedSeries(dates, keys, rows);
    }

    private static Table Revenue(SeededRandom random)
    {
        string[] keys = ["North America", "Europe", "Asia Pacific", "Latin America"];
        double[] bases = [1.9e6, 1.3e6, 0.95e6, 0.35e6];
        var labels = new string[8];
        var rows = new double[8][];
        for (var q = 0; q < 8; q++)
        {
            labels[q] = $"Q{q % 4 + 1} {2023 + q / 4}";
            rows[q] = new double[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                var growth = 1 + (0.03 + 0.02 * i) * q;
                rows[q][i] = Math.Round(bases[i] * growth * (1 + 0.08 * random.NextGaussian()) / 1000) * 1000;
            }
        }

        return new Table(labels, keys, rows);
    }

    private static Shares Browsers() =>
        new(["Chrome", "Safari", "Edge", "Firefox", "Samsung Internet", "Opera", "Other"], [64.7, 18.6, 5.2, 2.8, 2.4, 2.2, 4.1]);

    private static StackedSeries Stream(SeededRandom random)
    {
        const int n = 100;
        const int m = 8;
        var series = new double[m][];
        for (var i = 0; i < m; i++)
        {
            series[i] = new double[n];
            for (var b = 0; b < 5; b++)
            {
                Bump(series[i], random);
            }
        }

        var rows = new double[n][];
        for (var j = 0; j < n; j++)
        {
            rows[j] = new double[m];
            for (var i = 0; i < m; i++)
            {
                rows[j][i] = Math.Round(series[i][j] * 1000) / 1000;
            }
        }

        return new StackedSeries([.. Enumerable.Range(0, n).Select(j => (double)j)], [.. Enumerable.Range(1, m).Select(i => $"Series {i}")], rows);
    }

    private static void Bump(double[] values, SeededRandom random)
    {
        var x = 1 / (0.1 + random.NextDouble());
        var y = 2 * random.NextDouble() - 0.5;
        var z = 10 / (0.1 + random.NextDouble());
        for (var i = 0; i < values.Length; i++)
        {
            var w = ((double)i / values.Length - y) * z;
            values[i] += x * Math.Exp(-w * w);
        }
    }

    private static double[] Heights(SeededRandom random)
    {
        var values = new double[1000];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Math.Round((i % 2 == 0 ? 176 : 163) + (i % 2 == 0 ? 7.2 : 6.4) * random.NextGaussian(), 1);
        }

        return values;
    }

    private static Groups Flowers(SeededRandom random)
    {
        string[] keys = ["Setosa", "Versicolor", "Virginica"];
        double[] cx = [5.0, 5.9, 6.6];
        double[] cy = [3.4, 2.8, 3.0];
        var group = new int[120];
        var x = new double[120];
        var y = new double[120];
        var size = new double[120];
        for (var i = 0; i < group.Length; i++)
        {
            var k = i % 3;
            group[i] = k;
            x[i] = Math.Round((cx[k] + 0.4 * random.NextGaussian()) * 10) / 10;
            y[i] = Math.Round((cy[k] + 0.3 * random.NextGaussian()) * 10) / 10;
            size[i] = Math.Round((0.4 + 1.6 * k + 0.8 * random.NextDouble()) * 10) / 10;
        }

        return new Groups(keys, group, x, y, size);
    }

    private static DailyRange Temperatures(SeededRandom random)
    {
        var dates = new double[365];
        var low = new double[365];
        var high = new double[365];
        var start = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var d = 0; d < 365; d++)
        {
            dates[d] = start.AddDays(d).ToUnixTimeMilliseconds();
            var season = -Math.Cos(2 * Math.PI * (d - 15) / 365);
            var mean = 12 + 11 * season + 2.5 * random.NextGaussian();
            var spread = 7 + 2 * season + 1.5 * random.NextDouble();
            low[d] = Math.Round((mean - spread / 2) * 10) / 10;
            high[d] = Math.Round((mean + spread / 2) * 10) / 10;
        }

        return new DailyRange(dates, low, high);
    }

    private static Grid Anomalies(SeededRandom random)
    {
        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        var years = new double[30];
        var values = new double[12][];
        for (var m = 0; m < 12; m++)
        {
            values[m] = new double[years.Length];
            for (var j = 0; j < years.Length; j++)
            {
                years[j] = 1994 + j;
                values[m][j] = Math.Round((-0.6 + 0.045 * j + 0.35 * Math.Sin(m / 2.0) * 0.5 + 0.4 * random.NextGaussian()) * 100) / 100;
            }
        }

        return new Grid(months, years, values);
    }

    private static TimeSeries Transistors(SeededRandom random)
    {
        var years = new double[51];
        var counts = new double[51];
        for (var i = 0; i < years.Length; i++)
        {
            years[i] = 1971 + i;
            counts[i] = Math.Round(2300 * Math.Pow(2, i / 2.1) * Math.Exp(0.35 * random.NextGaussian()));
        }

        return new TimeSeries(years, counts);
    }

    private static Points Curves() => new([0, 1, 2, 3, 4, 5, 6, 7, 8], [2.0, 6.5, 4.2, 8.8, 7.9, 3.1, 4.6, 9.2, 6.0]);
}

internal sealed class SeededRandom(uint seed)
{
    private uint state = seed;

    public double NextDouble()
    {
        state += 0x6D2B79F5;
        var t = state;
        t = (t ^ (t >> 15)) * (t | 1);
        t ^= t + (t ^ (t >> 7)) * (t | 61);
        return (t ^ (t >> 14)) / 4294967296.0;
    }

    public double NextGaussian() =>
        Math.Sqrt(-2 * Math.Log(1 - NextDouble())) * Math.Cos(2 * Math.PI * NextDouble());
}
