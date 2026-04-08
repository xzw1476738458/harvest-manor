namespace HarvestManor.Core.Time;

public sealed class DayClock
{
    private readonly int _dayStartMinute;
    private readonly int _minutesPerDay;

    public DayClock(GameDate date, int dayStartMinute, int dayEndMinute)
    {
        if (dayEndMinute <= dayStartMinute)
        {
            throw new ArgumentOutOfRangeException(nameof(dayEndMinute), dayEndMinute, "Day end minute must be greater than day start minute.");
        }

        Date = date;
        CurrentMinuteOfDay = dayStartMinute;
        _dayStartMinute = dayStartMinute;
        _minutesPerDay = dayEndMinute - dayStartMinute;
    }

    public GameDate Date { get; private set; }

    public int CurrentMinuteOfDay { get; private set; }

    public bool AdvanceMinutes(int minutes)
    {
        if (minutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "Minutes to advance must be non-negative.");
        }

        var elapsedMinutesInDay = CurrentMinuteOfDay - _dayStartMinute;
        var totalElapsedMinutes = elapsedMinutesInDay + minutes;
        var daysElapsed = totalElapsedMinutes / _minutesPerDay;
        var minuteOffsetInCurrentDay = totalElapsedMinutes % _minutesPerDay;

        CurrentMinuteOfDay = _dayStartMinute + minuteOffsetInCurrentDay;

        if (daysElapsed == 0)
        {
            return false;
        }

        for (var day = 0; day < daysElapsed; day++)
        {
            Date = Date.NextDay();
        }

        return true;
    }
}
