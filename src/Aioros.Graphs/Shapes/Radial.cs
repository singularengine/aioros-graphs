namespace Aioros.Graphs.Shapes;

/// <summary>Polar coordinates as d3-shape's radial shapes use them: angles in radians clockwise from 12 o'clock.</summary>
public static class Radial
{
    /// <summary>Returns the point at <paramref name="angle"/> and <paramref name="radius"/>, as d3's <c>pointRadial</c>.</summary>
    /// <param name="angle">The angle in radians, clockwise from 12 o'clock.</param>
    /// <param name="radius">The distance from the origin.</param>
    /// <returns>The point.</returns>
    public static (double X, double Y) Point(double angle, double radius)
    {
        angle -= Math.PI / 2;
        return (radius * Math.Cos(angle), radius * Math.Sin(angle));
    }
}
