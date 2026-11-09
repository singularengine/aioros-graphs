namespace Aioros.Graphs.Colors;

internal static class ColorMath
{
    public const double Darker = 0.7;
    public const double Brighter = 1 / Darker;
    public const double Radians = Math.PI / 180;
    public const double Degrees = 180 / Math.PI;

    public static double ClampOpacity(double opacity) => double.IsNaN(opacity) ? 1 : Math.Max(0, Math.Min(1, opacity));

    public static double ClampChannel(double value)
    {
        var floor = Math.Floor(value);
        var rounded = value - floor >= 0.5 ? floor + 1 : floor;
        if (!(rounded > 0))
        {
            return 0;
        }

        return rounded > 255 ? 255 : rounded;
    }

    public static double ClampHue(double value)
    {
        value = (double.IsNaN(value) || value == 0 ? 0 : value) % 360;
        return value < 0 ? value + 360 : value;
    }

    public static double ClampUnit(double value) => Math.Max(0, Math.Min(1, double.IsNaN(value) || value == 0 ? 0 : value));

    public static double IsTrue(bool value) => value ? 1 : 0;
}
