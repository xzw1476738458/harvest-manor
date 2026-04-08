namespace HarvestManor.Core.Time;

public sealed class DayClock
{
    private readonly int _dayStartMinute;
    private readonly int _dayEndMinute;

    public DayClock(GameDate date, int dayStartMinute, int dayEndMinute)
    {
        Date = date;
        CurrentMinuteOfDay = dayStartMinute;
        _dayStartMinute = dayStartMinute;
        _dayEndMinute = dayEndMinute;
    }

    public GameDate Date { get; private set; }

    public int CurrentMinuteOfDay { get; private set; }

    public bool AdvanceMinutes(int minutes)
    {
        CurrentMinuteOfDay += minutes;

        if (CurrentMinuteOfDay < _dayEndMinute)
        {
            return false;
        }

        Date = Date.NextDay();
        CurrentMinuteOfDay = _dayStartMinute;
        return true;
    }
}
