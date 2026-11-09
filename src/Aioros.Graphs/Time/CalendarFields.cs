namespace Aioros.Graphs.Time;

internal readonly record struct CalendarFields(int Year, int Month, int Day, long Time, long DayNumber)
{
    public int Weekday => UtcCalendar.WeekdayOf(DayNumber);

    public long Hours => Time / 3_600_000;

    public long Minutes => Time / 60_000 % 60;

    public long Seconds => Time / 1000 % 60;

    public long Milliseconds => Time % 1000;
}
