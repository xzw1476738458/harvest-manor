namespace HarvestManor.Core.Time;

public readonly record struct GameDate
{
    public const int DaysPerSeason = 28;

    public Season Season { get; init; }
    public int Day { get; init; }

    public GameDate(Season season, int day)
    {
        if (day < 1 || day > DaysPerSeason)
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, $"Day must be in range [1, {DaysPerSeason}].");
        }

        Season = season;
        Day = day;
    }

    public GameDate NextDay()
    {
        if (Day < DaysPerSeason)
        {
            return this with { Day = Day + 1 };
        }

        var nextSeason = Season switch
        {
            Season.Spring => Season.Summer,
            Season.Summer => Season.Autumn,
            Season.Autumn => Season.Winter,
            _ => Season.Spring
        };

        return new GameDate(nextSeason, 1);
    }
}
