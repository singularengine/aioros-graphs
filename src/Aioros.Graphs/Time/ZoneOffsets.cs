using System.Collections.Immutable;

namespace Aioros.Graphs.Time;

internal sealed class ZoneOffsets
{
    private const double Reach = 16 * UtcCalendar.MillisecondsPerDay;
    private const int Capacity = 32;
    private const double MinDateTime = -62_135_596_800_000;
    private const double MaxDateTime = 253_402_300_799_999;
    private const double Cycle = 146_097 * UtcCalendar.MillisecondsPerDay;

    private static ImmutableDictionary<TimeZoneInfo, ZoneOffsets> zones = ImmutableDictionary.Create<TimeZoneInfo, ZoneOffsets>(ReferenceEqualityComparer.Instance);
    private static ZoneOffsets? last;

    private readonly TimeZoneInfo zone;
    private Window[] windows = [];

    private ZoneOffsets(TimeZoneInfo zone)
    {
        this.zone = zone;
    }

    public static ZoneOffsets For(TimeZoneInfo zone)
    {
        var recent = Volatile.Read(ref last);
        if (recent is not null && ReferenceEquals(recent.zone, zone))
        {
            return recent;
        }

        var offsets = ImmutableInterlocked.GetOrAdd(ref zones, zone, static z => new ZoneOffsets(z));
        Volatile.Write(ref last, offsets);
        return offsets;
    }

    public double At(double t)
    {
        var known = Volatile.Read(ref windows);
        foreach (var window in known)
        {
            if (window.Start <= t && t <= window.End)
            {
                return window.Offset;
            }
        }

        var offset = Raw(t);
        var start = Raw(t - Reach) == offset ? t - Reach : Edge(t - Reach, t, offset);
        var end = Raw(t + Reach) == offset ? t + Reach : Edge(t + Reach, t, offset);
        var updated = new Window[Math.Min(known.Length + 1, Capacity)];
        updated[0] = new Window(start, end, offset);
        Array.Copy(known, 0, updated, 1, updated.Length - 1);
        Volatile.Write(ref windows, updated);
        return offset;
    }

    private double Edge(double outside, double inside, double offset)
    {
        while (Math.Abs(outside - inside) > 1)
        {
            var middle = Math.Floor((outside + inside) / 2);
            if (Raw(middle) == offset)
            {
                inside = middle;
            }
            else
            {
                outside = middle;
            }
        }

        return inside;
    }

    private double Raw(double t)
    {
        var mapped = t > MaxDateTime ? t - Math.Ceiling((t - MaxDateTime) / Cycle) * Cycle : Math.Max(t, MinDateTime);
        var instant = new DateTime((long)(mapped - MinDateTime) * TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        return zone.GetUtcOffset(instant).TotalMilliseconds;
    }

    private readonly record struct Window(double Start, double End, double Offset);
}
